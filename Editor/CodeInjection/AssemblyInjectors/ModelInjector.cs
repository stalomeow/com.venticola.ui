using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using VentiCola.UI;
using static VentiColaEditor.UI.CodeInjection.MetaDataUtility;

namespace VentiColaEditor.UI.CodeInjection.AssemblyInjectors
{
    internal class ModelInjector : IAssemblyInjector
    {
        public string DisplayTitle => "Reactive Model Injector";

        public AssemblyDefinition Assembly { get; set; }

        public MethodReferenceCache Methods { get; set; }

        public InjectionTasks Tasks { get; set; }

        public LogLevel LogLevel { get; set; }

        public IProgress<float> Progress { get; set; }

        public bool InjectAssembly()
        {
            List<TypeDefinition> modelTypes = FindAllTypes(Assembly, type =>
            {
                return IsSubclassOf<ReactiveModel>(type);
            });

            bool injectedAnyCode = false;

            for (int i = 0; i < modelTypes.Count; i++)
            {
                injectedAnyCode |= InjectType(modelTypes[i]);
                Progress.Report((float)(i + 1) / modelTypes.Count);
            }

            return injectedAnyCode;
        }

        private bool InjectType(TypeDefinition type)
        {
            bool injectAtLeastOne = false;

            foreach (PropertyDefinition prop in type.Properties)
            {
                injectAtLeastOne |= InjectProperty(prop);
            }

            if (Tasks.HasFlag(InjectionTasks.Reactive_ModelGet))
            {
                injectAtLeastOne |= InjectModelGetMethodOverride(type);
            }

            if (Tasks.HasFlag(InjectionTasks.Reactive_ModelSet))
            {
                injectAtLeastOne |= InjectModelSetMethodOverride(type);
            }

            if (injectAtLeastOne && LogLevel.HasFlag(LogLevel.Type))
            {
                Debug.Log($"Injected UI codes for type <b>'{type.FullName}'</b>.");
            }

            return injectAtLeastOne;
        }

        private bool InjectProperty(PropertyDefinition prop)
        {
            if (!IsValidProperty(prop, out bool isComputed, out bool computedWithoutBranches))
            {
                return false;
            }

            if (!isComputed && Tasks.HasFlag(InjectionTasks.Reactive_AutoProperty))
            {
                InjectAutoPropertyGetter(prop);
                InjectAutoPropertySetter(prop);

                if (LogLevel.HasFlag(LogLevel.Property))
                {
                    Debug.Log($"Injected UI codes for auto-property <b>'{prop.FullName}'</b>.");
                }

                return true;
            }

            if (isComputed && Tasks.HasFlag(InjectionTasks.Reactive_LazyComputed))
            {
                InjectComputedPropertyBackingField(prop, out FieldDefinition field);
                InjectComputedPropertyAnonymousMethod(prop, out MethodDefinition method);
                InjectComputedPropertyGetter(prop, field);
                InjectComputedPropertyInitialization(prop, field, method, computedWithoutBranches);

                if (LogLevel.HasFlag(LogLevel.Property))
                {
                    Debug.Log($"Injected UI codes for lazy-computed-property <b>'{prop.FullName}'</b>.");
                }

                return true;
            }

            return false;
        }

        private static bool IsValidProperty(PropertyDefinition prop, out bool isComputed, out bool computedWithoutBranches)
        {
            computedWithoutBranches = default;

            if (!prop.HasThis || prop.GetMethod is not { IsPublic: true })
            {
                isComputed = false;
                return false;
            }

            // 不再支持 discard 了
            //if (HasCustomAttribute<ReactiveDiscardAttribute>(prop.CustomAttributes))
            //{
            //    isComputed = false;
            //    return false;
            //}

            // 现在，Computed 属性可以有 Set 方法了，且 Set 方法不会被修改。
            isComputed = !IsCompilerGeneratedMethod(prop.GetMethod)
                && IsComputedProperty(prop, out computedWithoutBranches);
            // && (prop.SetMethod is null);

            return isComputed || (
                IsCompilerGeneratedMethod(prop.GetMethod)
                && prop.SetMethod is { IsPublic: true }
                && IsCompilerGeneratedMethod(prop.SetMethod)
            );
        }

        private static bool IsComputedProperty(PropertyDefinition prop, out bool computedWithoutBranches)
        {
            computedWithoutBranches = default;

            if (HasCustomAttribute<LazyComputedAttribute>(prop.CustomAttributes, out CustomAttribute attr))
            {
                foreach (var attrNamedProp in attr.Properties)
                {
                    switch (attrNamedProp.Name)
                    {
                        case nameof(LazyComputedAttribute.NoBranches):
                            computedWithoutBranches = (bool)attrNamedProp.Argument.Value;
                            break;
                    }
                }

                return true;
            }

            return false;
        }

        private void InjectAutoPropertyGetter(PropertyDefinition prop)
        {
            MethodDefinition getter = prop.GetMethod;
            Collection<Instruction> instructions = getter.Body.Instructions;
            FieldReference backingField = FindBackingFieldOfAutoProperty(instructions);

            if (backingField is null)
            {
                throw new MissingFieldException($"Can not find the backing-field of auto-property '{prop.FullName}'!");
            }

            RemoveCustomAttributes(getter.CustomAttributes, typeof(CompilerGeneratedAttribute));
            instructions.Clear();

            ILProcessor il = getter.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, backingField);
            il.Emit(OpCodes.Ldstr, prop.Name);
            il.Emit(OpCodes.Call, Methods.MakeDataModelGetPropertyMethod(prop.PropertyType));
            il.Emit(OpCodes.Ret);
        }

        private void InjectAutoPropertySetter(PropertyDefinition prop)
        {
            MethodDefinition setter = prop.SetMethod;
            Collection<Instruction> instructions = setter.Body.Instructions;
            FieldReference backingField = FindBackingFieldOfAutoProperty(instructions);

            if (backingField is null)
            {
                throw new MissingFieldException($"Can not find the backing-field of auto-property '{prop.FullName}'!");
            }

            RemoveCustomAttributes(setter.CustomAttributes, typeof(CompilerGeneratedAttribute));
            instructions.Clear();

            ILProcessor il = setter.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, backingField);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldstr, prop.Name);
            il.Emit(OpCodes.Call, Methods.MakeDataModelSetPropertyMethod(prop.PropertyType));
            il.Emit(OpCodes.Ret);
        }

        private static void InjectComputedPropertyBackingField(PropertyDefinition prop, out FieldDefinition field)
        {
            var fieldName = $"<{prop.Name}>k__BackingField";
            var fieldType = ImportGenericTypeReference(prop.Module, typeof(LazyComputedProperty<>), prop.PropertyType);

            field = new FieldDefinition(fieldName, FieldAttributes.Private, fieldType);
            AddCompilerGeneratedAttribute(prop.Module, field.CustomAttributes);
            prop.DeclaringType.Fields.Add(field);
        }

        private static void InjectComputedPropertyAnonymousMethod(PropertyDefinition prop, out MethodDefinition method)
        {
            var methodName = $"<{prop.Name}>b__LazyComputedGetter";
            var methodAttr = MethodAttributes.Private | MethodAttributes.HideBySig;

            method = new MethodDefinition(methodName, methodAttr, prop.PropertyType);
            AddCompilerGeneratedAttribute(prop.Module, method.CustomAttributes);

            // copy method body from the original getter
            method.Body.InitLocals = prop.GetMethod.Body.InitLocals;

            foreach (Instruction ins in prop.GetMethod.Body.Instructions)
            {
                method.Body.Instructions.Add(ins);
            }

            foreach (VariableDefinition varDef in prop.GetMethod.Body.Variables)
            {
                method.Body.Variables.Add(varDef);
            }

            foreach (ExceptionHandler exHandler in prop.GetMethod.Body.ExceptionHandlers)
            {
                method.Body.ExceptionHandlers.Add(exHandler);
            }

            prop.DeclaringType.Methods.Add(method);
        }

        private void InjectComputedPropertyGetter(PropertyDefinition prop, FieldReference backingField)
        {
            MethodReference valueGetter = Methods.MakeLazyComputedPropertyValueGetter(prop.Module, backingField.FieldType);

            // rewrite the method
            MethodDefinition getter = prop.GetMethod;
            getter.Body.InitLocals = false;
            getter.Body.Instructions.Clear();
            getter.Body.Variables.Clear();
            getter.Body.ExceptionHandlers.Clear();

            ILProcessor il = getter.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, backingField);
            il.Emit(OpCodes.Call, valueGetter); // il.Emit(OpCodes.Callvirt, valueGetter);
            il.Emit(OpCodes.Ret);
        }

        private void InjectComputedPropertyInitialization(PropertyDefinition prop, FieldReference backingField, MethodReference propGetter, bool noBranches)
        {
            MethodReference funcDelegateCtor = Methods.MakeFuncDelegateConstructor(prop.Module, prop.PropertyType);
            MethodReference computedCtor2Args = Methods.MakeLazyComputedPropertyCtor2Args(prop.Module, backingField.FieldType);

            foreach (MethodDefinition method in prop.DeclaringType.Methods)
            {
                if (!method.IsConstructor)
                {
                    continue;
                }

                Collection<Instruction> instructions = method.Body.Instructions;

                if (InstructionsWillInvokeOwnConstructor(prop.DeclaringType, instructions))
                {
                    continue; // 避免多次调用构造方法，重复初始化字段
                }

                ILProcessor il = method.Body.GetILProcessor();

                // 在构造方法最前面插入初始化代码，方便在构造方法中访问属性
                Instruction[] emitILCodes = new[]
                {
                    il.Create(OpCodes.Ldarg_0),
                    il.Create(OpCodes.Ldarg_0),
                    il.Create(OpCodes.Ldftn, propGetter),
                    il.Create(OpCodes.Newobj, funcDelegateCtor),
                    il.Create(noBranches ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                    il.Create(OpCodes.Newobj, computedCtor2Args),
                    il.Create(OpCodes.Stfld, backingField)
                };

                for (int i = emitILCodes.Length - 1; i >= 0; i--)
                {
                    instructions.Insert(0, emitILCodes[i]);
                }
            }
        }

        private static readonly StringSwitchCaseEmitter<PropertyDefinition> s_ModelGetMethodSwitchEmitter = new()
        {
            EmitCaseHandlerDelegate = static (il, methods, switchKey, caseString, prop, userData) =>
            {
                il.Emit(OpCodes.Ldarg_0);
                // 必须要在调用 getter 前 import 一下，因为它可能被定义在其他程序集
                il.Emit(OpCodes.Call, il.Body.Method.Module.ImportReference(prop.GetMethod));

                var propType = prop.PropertyType;
                var genericParameter = (GenericParameter)userData;

                if (propType.IsValueType)
                {
                    il.Emit(OpCodes.Call, methods.MakeCastValueTypeMethod(propType, genericParameter));
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
                il.Emit(OpCodes.Newobj, methods.MissingPublicPropertyExceptionConstructor);
                il.Emit(OpCodes.Throw);
                return false; // 都 throw 了，后面没必要 ret
            }
        };

        private static readonly StringSwitchCaseEmitter<PropertyDefinition> s_ModelSetMethodSwitchEmitter = new()
        {
            EmitCaseHandlerDelegate = static (il, methods, switchKey, caseString, prop, userData) =>
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);

                var propType = prop.PropertyType;
                var genericParameter = (GenericParameter)userData;

                if (propType.IsValueType)
                {
                    il.Emit(OpCodes.Call, methods.MakeCastAnyMethod(genericParameter, propType));
                }
                else
                {
                    il.Emit(OpCodes.Box, genericParameter);
                    il.Emit(OpCodes.Castclass, propType);
                }

                // 必须要在调用 setter 前 import 一下，因为它可能被定义在其他程序集
                il.Emit(OpCodes.Call, il.Body.Method.Module.ImportReference(prop.SetMethod));
                return true;
            },
            EmitFallThroughDelegate = static (il, methods, switchKey, userData) =>
            {
                il.Emit_Ldloc_Or_Ldarg(switchKey);
                il.Emit(OpCodes.Newobj, methods.MissingPublicPropertyExceptionConstructor);
                il.Emit(OpCodes.Throw);
                return false; // 都 throw 了，后面没必要 ret
            }
        };

        private bool InjectModelGetMethodOverride(TypeDefinition type)
        {
            static bool IsValidGetMethod(MethodDefinition method)
            {
                return method.Name == nameof(ReactiveModel.Get)
                    && !method.IsStatic
                    && method.IsVirtual
                    && method.GenericParameters.Count == 1
                    && method.GenericParameters[0] == method.ReturnType
                    && method.Parameters.Count == 1
                    && method.Parameters[0].ParameterType.FullName == typeof(string).FullName;
            }

            // 检查是否已经重载过（只检查自己身上，不检查基类的）
            if (type.Methods.Any(IsValidGetMethod))
            {
                return false;
            }

            // 筛选合适的属性（除了自己的还要收集基类的）
            var props = new Dictionary<string, PropertyDefinition>();

            WalkUpTypesUntil<ReactiveModel>(type, t =>
            {
                foreach (var prop in t.Properties)
                {
                    if (prop.GetMethod is { IsPublic: true, IsStatic: false })
                    {
                        props.Add(prop.Name, prop);
                    }
                }
            });

            if (props.Count == 0)
            {
                return false;
            }

            // 定义重载方法
            var overrideMethod = new MethodDefinition(nameof(ReactiveModel.Get),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                type.Module.TypeSystem.Void);
            var genericParam = new GenericParameter("T", overrideMethod);

            overrideMethod.GenericParameters.Add(genericParam);
            overrideMethod.ReturnType = genericParam;
            overrideMethod.Parameters.Add(new("propertyName", ParameterAttributes.None, type.Module.TypeSystem.String));
            type.Methods.Add(overrideMethod);

            // 写入 IL 代码
            var body = overrideMethod.Body;
            body.InitLocals = true;

            s_ModelGetMethodSwitchEmitter.Emit(body.GetILProcessor(), type.Module.TypeSystem, Methods,
                LocalOrParameter.Parameter(1), props, genericParam);

            if (LogLevel.HasFlag(LogLevel.Method))
            {
                Debug.Log($"Injected UI codes for method <b>'{overrideMethod.FullName}'</b>.");
            }

            return true;
        }

        private bool InjectModelSetMethodOverride(TypeDefinition type)
        {
            static bool IsValidSetMethod(MethodDefinition method)
            {
                return method.Name == nameof(ReactiveModel.Set)
                    && !method.IsStatic
                    && method.IsVirtual
                    && method.ReturnType.FullName == typeof(void).FullName
                    && method.GenericParameters.Count == 1
                    && method.Parameters.Count == 2
                    && method.Parameters[0].ParameterType.FullName == typeof(string).FullName
                    && method.Parameters[1].ParameterType == method.GenericParameters[0];
            }

            // 检查是否已经重载过（只检查自己身上，不检查基类的）
            if (type.Methods.Any(IsValidSetMethod))
            {
                return false;
            }

            // 筛选合适的属性（除了自己的还要收集基类的）
            var props = new Dictionary<string, PropertyDefinition>();

            WalkUpTypesUntil<ReactiveModel>(type, t =>
            {
                foreach (var prop in t.Properties)
                {
                    if (prop.SetMethod is { IsPublic: true, IsStatic: false })
                    {
                        props.Add(prop.Name, prop);
                    }
                }
            });

            if (props.Count == 0)
            {
                return false;
            }

            // 定义重载方法
            var overrideMethod = new MethodDefinition(nameof(ReactiveModel.Set),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                type.Module.TypeSystem.Void);
            var genericParam = new GenericParameter("T", overrideMethod);

            overrideMethod.GenericParameters.Add(genericParam);
            overrideMethod.Parameters.Add(new("propertyName", ParameterAttributes.None, type.Module.TypeSystem.String));
            overrideMethod.Parameters.Add(new("value", ParameterAttributes.None, genericParam));
            type.Methods.Add(overrideMethod);

            // 写入 IL 代码
            var body = overrideMethod.Body;
            body.InitLocals = true;

            s_ModelSetMethodSwitchEmitter.Emit(body.GetILProcessor(), type.Module.TypeSystem, Methods,
                LocalOrParameter.Parameter(1), props, genericParam);

            if (LogLevel.HasFlag(LogLevel.Method))
            {
                Debug.Log($"Injected UI codes for method <b>'{overrideMethod.FullName}'</b>.");
            }

            return true;
        }

        private static FieldReference FindBackingFieldOfAutoProperty(Collection<Instruction> instructions)
        {
            foreach (Instruction ins in instructions)
            {
                if (ins.OpCode.OperandType == OperandType.InlineField)
                {
                    return ins.Operand as FieldReference;
                }
            }

            return null;
        }

        private static bool InstructionsWillInvokeOwnConstructor(TypeDefinition type, Collection<Instruction> instructions)
        {
            return instructions.Any(ins =>
            {
                if (ins.OpCode.OperandType == OperandType.InlineMethod)
                {
                    MethodReference m = ins.Operand as MethodReference;
                    return (m.Name == ".ctor") && (m.DeclaringType.FullName == type.FullName);
                }

                return false;
            });
        }
    }
}