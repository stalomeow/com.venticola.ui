using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using static VentiColaEditor.UI.CodeInjection.MetaDataUtility;

namespace VentiColaEditor.UI.CodeInjection.AssemblyInjectors
{
    internal class PageInjector : IAssemblyInjector
    {
        public string DisplayTitle => "UI Page Injector";

        public AssemblyDefinition Assembly { get; set; }

        public MethodReferenceCache Methods { get; set; }

        public InjectionTasks Tasks { get; set; }

        public LogLevel LogLevel { get; set; }

        public IProgress<float> Progress { get; set; }

        public bool InjectAssembly()
        {
            List<TypeDefinition> pageTypes = FindAllTypes(Assembly, type =>
            {
                return IsSubclassOf<UIPage>(type);
            });

            bool injectedAnyCode = false;

            for (int i = 0; i < pageTypes.Count; i++)
            {
                injectedAnyCode |= InjectType(pageTypes[i]);
                Progress.Report((float)(i + 1) / pageTypes.Count);
            }

            return injectedAnyCode;
        }

        private bool InjectType(TypeDefinition type)
        {
            bool injectAtLeastOne = false;

            if (Tasks.HasFlag(InjectionTasks.UIPage_Callback))
            {
                injectAtLeastOne |= InjectCallbacks(type);
            }

            if (Tasks.HasFlag(InjectionTasks.UIPage_EventHandler))
            {
                injectAtLeastOne |= InjectEventHandlers(type);
            }

            if (injectAtLeastOne && LogLevel.HasFlag(LogLevel.Type))
            {
                Debug.Log($"Injected UI codes for type <b>'{type.FullName}'</b>.");
            }

            return injectAtLeastOne;
        }

        private static readonly StringSwitchCaseEmitter<MethodReference> s_CallbackSwithEmitter = new()
        {
            EmitCaseHandlerDelegate = static (il, methods, switchKey, caseString, method, userData) =>
            {
                il.Emit_Ldarg(0); // this
                il.Emit_Ldarg(2); // firstArg

                var firstArgType = method.Parameters[0].ParameterType;
                var genericParameter = (GenericParameter)userData;

                if (firstArgType.IsValueType)
                {
                    il.Emit(OpCodes.Call, methods.MakeCastAnyMethod(genericParameter, firstArgType));
                }
                else
                {
                    il.Emit(OpCodes.Box, genericParameter);
                    il.Emit(OpCodes.Castclass, firstArgType);
                }

                for (int i = 1; i < method.Parameters.Count; i++)
                {
                    var argType = method.Parameters[i].ParameterType;

                    il.Emit_Ldarg(3); // restArgs
                    il.Emit_Ldc_I4(i - 1); // index
                    il.Emit(OpCodes.Ldelem_Ref); // restArgs[index]
                    il.Emit(OpCodes.Callvirt, methods.MakeDynamicArgumentGetValueMethod(argType));
                }

                il.Emit(OpCodes.Call, method);
                return true;
            },
            EmitFallThroughDelegate = static (il, methods, switchKey, userData) =>
            {
                il.Emit_Ldloc_Or_Ldarg(switchKey);
                il.Emit(OpCodes.Newobj, methods.MissingPublicMethodExceptionConstructor);
                il.Emit(OpCodes.Throw);
                return false; // 都 throw 了，后面没必要 ret
            }
        };

        private static readonly StringSwitchCaseEmitter<MethodReference> s_EventHandlerSwithEmitter = new()
        {
            EmitCaseHandlerDelegate = static (il, methods, switchKey, caseString, method, userData) =>
            {
                il.Emit_Ldarg(0); // this

                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var argType = method.Parameters[i].ParameterType;

                    il.Emit_Ldarg(2); // args
                    il.Emit_Ldc_I4(i); // index
                    il.Emit(OpCodes.Ldelem_Ref); // args[index]
                    il.Emit(OpCodes.Callvirt, methods.MakeDynamicArgumentGetValueMethod(argType));
                }

                il.Emit(OpCodes.Call, method);

                var genericParameter = (GenericParameter)userData;

                if (method.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Call, methods.MakeCastAnyMethod(method.ReturnType, genericParameter));
                }
                else
                {
                    il.Emit(OpCodes.Unbox_Any, genericParameter);
                }

                return true;
            },
            EmitFallThroughDelegate = static (il, methods, switchKey, userData) =>
            {
                il.Emit_Ldloc_Or_Ldarg(switchKey);
                il.Emit(OpCodes.Newobj, methods.MissingPublicMethodExceptionConstructor);
                il.Emit(OpCodes.Throw);
                return false; // 都 throw 了，后面没必要 ret
            }
        };

        private bool InjectCallbacks(TypeDefinition type)
        {
            static bool IsValidInvokeMethod(MethodDefinition method)
            {
                return method.Name == nameof(UIPage.InvokeMethod)
                    && !method.IsStatic
                    && method.IsVirtual
                    && method.ReturnType.FullName == typeof(void).FullName
                    && method.GenericParameters.Count == 1
                    && method.Parameters.Count == 3
                    && method.Parameters[0].ParameterType.FullName == typeof(string).FullName
                    && method.Parameters[1].ParameterType == method.GenericParameters[0]
                    && method.Parameters[2].ParameterType.FullName == typeof(DynamicArgument[]).FullName;
            }

            // 检查是否已经重载过（只检查自己身上，不检查基类的）
            if (type.Methods.Any(IsValidInvokeMethod))
            {
                return false;
            }

            // 筛选合适的方法（除了自己的还要收集基类的）
            var callbacks = new Dictionary<string, MethodReference>();
            var excludedMethods = new HashSet<string>();

            WalkUpTypesUntil<UIPage>(type, t =>
            {
                foreach (var method in t.Methods)
                {
                    if (method is { IsPublic: true, IsStatic: false, HasGenericParameters: false, Parameters: { Count: > 0 } }
                        && !method.IsSpecialName // 属性访问器等
                        && !method.IsRuntimeSpecialName // 构造函数等
                        && method.Parameters.All(p => !p.ParameterType.IsByReference && !p.ParameterType.IsPointer)
                        && method.ReturnType.FullName == typeof(void).FullName)
                    {
                        // 不能有重载！
                        if (callbacks.ContainsKey(method.Name))
                        {
                            callbacks.Remove(method.Name);
                            excludedMethods.Add(method.Name);
                        }
                        else if (!excludedMethods.Contains(method.Name))
                        {
                            callbacks.Add(method.Name, type.Module.ImportReference(method));
                        }
                    }
                }
            });

            if (callbacks.Count == 0)
            {
                return false;
            }

            // 定义重载方法
            var overrideMethod = new MethodDefinition(nameof(UIPage.InvokeMethod),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                type.Module.TypeSystem.Void);
            var genericParam = new GenericParameter("T", overrideMethod);

            overrideMethod.GenericParameters.Add(genericParam);
            overrideMethod.Parameters.Add(new("name", ParameterAttributes.None, type.Module.TypeSystem.String));
            overrideMethod.Parameters.Add(new("firstArg", ParameterAttributes.None, genericParam));
            overrideMethod.Parameters.Add(new("restArgs", ParameterAttributes.None, type.Module.ImportReference(typeof(DynamicArgument[]))));
            type.Methods.Add(overrideMethod);

            // 写入 IL 代码
            var body = overrideMethod.Body;
            body.InitLocals = true;

            s_CallbackSwithEmitter.Emit(body.GetILProcessor(), type.Module.TypeSystem, Methods,
                LocalOrParameter.Parameter(1), callbacks, genericParam);

            if (LogLevel.HasFlag(LogLevel.Method))
            {
                Debug.Log($"Injected UI codes for method <b>'{overrideMethod.FullName}'</b>.");
            }

            return true;
        }

        private bool InjectEventHandlers(TypeDefinition type)
        {
            static bool IsValidInvokeMethod(MethodDefinition method)
            {
                return method.Name == nameof(UIPage.InvokeMethod)
                    && !method.IsStatic
                    && method.IsVirtual
                    && method.GenericParameters.Count == 1
                    && method.GenericParameters[0] == method.ReturnType
                    && method.Parameters.Count == 2
                    && method.Parameters[0].ParameterType.FullName == typeof(string).FullName
                    && method.Parameters[1].ParameterType.FullName == typeof(DynamicArgument[]).FullName;
            }

            // 检查是否已经重载过（只检查自己身上，不检查基类的）
            if (type.Methods.Any(IsValidInvokeMethod))
            {
                return false;
            }

            // 筛选合适的方法（除了自己的还要收集基类的）
            var eventHandlers = new Dictionary<string, MethodReference>();
            var excludedMethods = new HashSet<string>();

            WalkUpTypesUntil<UIPage>(type, t =>
            {
                foreach (var method in t.Methods)
                {
                    if (method is { IsPublic: true, IsStatic: false, HasGenericParameters: false }
                        && !method.IsSpecialName // 属性访问器等
                        && !method.IsRuntimeSpecialName // 构造函数等
                        && method.Parameters.All(p => !p.ParameterType.IsByReference && !p.ParameterType.IsPointer)
                        && method.ReturnType.FullName != typeof(void).FullName
                        && !method.ReturnType.IsByReference
                        && !method.ReturnType.IsPointer)
                    {
                        // 不能有重载！
                        if (eventHandlers.ContainsKey(method.Name))
                        {
                            eventHandlers.Remove(method.Name);
                            excludedMethods.Add(method.Name);
                        }
                        else if (!excludedMethods.Contains(method.Name))
                        {
                            eventHandlers.Add(method.Name, type.Module.ImportReference(method));
                        }
                    }
                }
            });

            if (eventHandlers.Count == 0)
            {
                return false;
            }

            // 定义重载方法
            var overrideMethod = new MethodDefinition(nameof(UIPage.InvokeMethod),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                type.Module.TypeSystem.Void);
            var genericParam = new GenericParameter("T", overrideMethod);

            overrideMethod.GenericParameters.Add(genericParam);
            overrideMethod.ReturnType = genericParam;
            overrideMethod.Parameters.Add(new("name", ParameterAttributes.None, type.Module.TypeSystem.String));
            overrideMethod.Parameters.Add(new("args", ParameterAttributes.None, type.Module.ImportReference(typeof(DynamicArgument[]))));
            type.Methods.Add(overrideMethod);

            // 写入 IL 代码
            var body = overrideMethod.Body;
            body.InitLocals = true;

            s_EventHandlerSwithEmitter.Emit(body.GetILProcessor(), type.Module.TypeSystem, Methods,
                LocalOrParameter.Parameter(1), eventHandlers, genericParam);

            if (LogLevel.HasFlag(LogLevel.Method))
            {
                Debug.Log($"Injected UI codes for method <b>'{overrideMethod.FullName}'</b>.");
            }

            return true;
        }
    }
}