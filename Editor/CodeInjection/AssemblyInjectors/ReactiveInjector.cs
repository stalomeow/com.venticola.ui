using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using VentiCola.UI;
using VentiCola.UI.Internals;
using static VentiColaEditor.UI.CodeInjection.MetaDataUtility;

namespace VentiColaEditor.UI.CodeInjection.AssemblyInjectors
{
    internal class ReactiveInjector : IAssemblyInjector
    {
        private struct ComputedPropInfo
        {
            public PropertyDefinition Prop;
            public FieldDefinition BackingField;
            public MethodDefinition Getter;
        }

        public string DisplayTitle => "Reactive Injector";

        public AssemblyDefinition Assembly { get; set; }

        public MethodReferenceCache Methods { get; set; }

        public LogLevel LogLevel { get; set; }

        public IProgress<float> Progress { get; set; }

        public bool InjectAssembly()
        {
            var injectedAnyCode = false;
            var computedPropsBuffer = new List<ComputedPropInfo>();

            ForEachAllTypes(Assembly, (i, count, type) =>
            {
                Progress.Report((float)(i + 1) / count);

                if (type.IsValueType || type.IsInterface)
                {
                    return;
                }

                bool injectedAnyProps = InjectProperties(type, computedPropsBuffer);
                injectedAnyCode |= injectedAnyProps;
                computedPropsBuffer.Clear();

                if (injectedAnyProps && LogLevel.HasFlag(LogLevel.Type))
                {
                    Debug.Log($"Injected UI codes for type <b>'{type.FullName}'</b>.");
                }
            });

            return injectedAnyCode;
        }

        private bool InjectProperties(TypeDefinition type, List<ComputedPropInfo> computedPropsBuffer)
        {
            var injectedAnyCode = false;

            foreach (PropertyDefinition prop in type.Properties)
            {
                if (!IsReactiveProperty(prop, out bool isComputed))
                {
                    continue;
                }

                if (isComputed)
                {
                    InjectComputedPropertyBackingField(prop, out FieldDefinition field);
                    InjectComputedPropertyAnonymousMethod(prop, out MethodDefinition method);
                    InjectComputedPropertyGetter(prop, field);

                    computedPropsBuffer.Add(new ComputedPropInfo
                    {
                        Prop = prop,
                        BackingField = field,
                        Getter = method
                    });

                    if (LogLevel.HasFlag(LogLevel.Property))
                    {
                        Debug.Log($"Injected UI codes for lazy-computed-property <b>'{prop.FullName}'</b>.");
                    }
                }
                else
                {
                    InjectAutoPropertyObserversField(prop, out FieldDefinition observersField);
                    InjectAutoPropertyGetter(prop, observersField);
                    InjectAutoPropertySetter(prop, observersField);

                    if (LogLevel.HasFlag(LogLevel.Property))
                    {
                        Debug.Log($"Injected UI codes for auto-property <b>'{prop.FullName}'</b>.");
                    }
                }

                injectedAnyCode = true;
            }

            InjectComputedPropertyInitializations(type, computedPropsBuffer);
            return injectedAnyCode;
        }

        private static bool IsReactiveProperty(PropertyDefinition prop, out bool isComputed)
        {
            if (!HasCustomAttribute<ReactiveAttribute>(prop.CustomAttributes, out CustomAttribute attr))
            {
                isComputed = false;
                return false;
            }

            isComputed = (attr.HasProperties && (bool)attr.Properties[0].Argument.Value);

            if (isComputed)
            {
                // 只对 getter 作要求
                return prop.GetMethod is not null
                    && prop.GetMethod.HasBody
                    && !IsCompilerGeneratedMethod(prop.GetMethod);
            }

            // 必须是自动实现 get & set 属性
            return prop.GetMethod is not null
                && prop.GetMethod.HasBody
                && IsCompilerGeneratedMethod(prop.GetMethod)
                && prop.SetMethod is not null
                && prop.SetMethod.HasBody
                && IsCompilerGeneratedMethod(prop.SetMethod);
        }

        private static void InjectAutoPropertyObserversField(PropertyDefinition prop, out FieldDefinition field)
        {
            var fieldName = $"<{prop.Name}>k__Observers";
            var fieldType = ImportTypeReference(prop.Module, typeof(WeakHashSet<IChangeObserver>));
            var fieldAttr = FieldAttributes.Private;

            if (!prop.HasThis)
            {
                fieldAttr |= FieldAttributes.Static;
            }

            field = new FieldDefinition(fieldName, fieldAttr, fieldType);
            AddCompilerGeneratedAttribute(prop.Module, field.CustomAttributes);
            prop.DeclaringType.Fields.Add(field);
        }

        private void InjectAutoPropertyGetter(PropertyDefinition prop, FieldReference observersField)
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

            il.Emit_Ldflda_Or_Ldsflda(observersField, prop.HasThis);
            il.Emit(OpCodes.Call, Methods.ChangeUtilityTryAddCurrentObserverMethod);

            il.Emit_Ldfld_Or_Ldsfld(backingField, prop.HasThis);
            il.Emit(OpCodes.Ret);
        }

        private void InjectAutoPropertySetter(PropertyDefinition prop, FieldReference observersField)
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

            il.Emit_Ldflda_Or_Ldsflda(backingField, prop.HasThis);
            il.Emit(prop.HasThis ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0);
            il.Emit_Ldfld_Or_Ldsfld(observersField, prop.HasThis);
            il.Emit(OpCodes.Call, Methods.MakeChangeUtilitySetWithNotifyMethod(prop.PropertyType));
            il.Emit(OpCodes.Ret);
        }

        private static void InjectComputedPropertyBackingField(PropertyDefinition prop, out FieldDefinition field)
        {
            var fieldName = $"<{prop.Name}>k__BackingField";
            var fieldType = ImportTypeReference(prop.Module, typeof(LazyComputedProperty<>), prop.PropertyType);
            var fieldAttr = FieldAttributes.Private;

            if (!prop.HasThis)
            {
                fieldAttr |= FieldAttributes.Static;
            }

            field = new FieldDefinition(fieldName, fieldAttr, fieldType);
            AddCompilerGeneratedAttribute(prop.Module, field.CustomAttributes);
            prop.DeclaringType.Fields.Add(field);
        }

        private static void InjectComputedPropertyAnonymousMethod(PropertyDefinition prop, out MethodDefinition method)
        {
            var methodName = $"<{prop.Name}>b__LazyComputedGetter";
            var methodAttr = MethodAttributes.Private | MethodAttributes.HideBySig;

            if (!prop.HasThis)
            {
                methodAttr |= MethodAttributes.Static;
            }

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
            MethodReference valueGetter = Methods.MakeLazyComputedPropertyValueGetter(backingField.FieldType);

            // rewrite the method
            MethodDefinition getter = prop.GetMethod;
            getter.Body.InitLocals = false;
            getter.Body.Instructions.Clear();
            getter.Body.Variables.Clear();
            getter.Body.ExceptionHandlers.Clear();

            ILProcessor il = getter.Body.GetILProcessor();

            il.Emit_Ldfld_Or_Ldsfld(backingField, prop.HasThis);
            il.Emit(OpCodes.Call, valueGetter);
            il.Emit(OpCodes.Ret);
        }

        private void InjectComputedPropertyInitializations(TypeDefinition type, List<ComputedPropInfo> computedProps)
        {
            var prependInstructions = new List<Instruction>();

            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.IsConstructor)
                {
                    continue;
                }

                // '.cctor' or '.ctor'
                bool hasThis = (method.Name == ".ctor");
                Collection<Instruction> instructions = method.Body.Instructions;

                if (hasThis && InstructionsWillInvokeOwnInstanceConstructor(type, instructions))
                {
                    continue; // 避免多次调用构造方法，重复初始化字段
                }

                ILProcessor il = method.Body.GetILProcessor();

                foreach (var prop in computedProps)
                {
                    if (prop.Prop.HasThis != hasThis)
                    {
                        continue;
                    }

                    MethodReference funcDelegateCtor = Methods.MakeFuncDelegateConstructor(prop.Prop.PropertyType);
                    MethodReference computedCtor = Methods.MakeLazyComputedPropertyCtor(prop.BackingField.FieldType);

                    // IL 指令没法复用，每次都得新创建
                    if (hasThis)
                    {
                        prependInstructions.Add(il.Create(OpCodes.Ldarg_0));
                        prependInstructions.Add(il.Create(OpCodes.Ldarg_0));
                        prependInstructions.Add(il.Create(OpCodes.Ldftn, prop.Getter));
                        prependInstructions.Add(il.Create(OpCodes.Newobj, funcDelegateCtor));
                        prependInstructions.Add(il.Create(OpCodes.Newobj, computedCtor));
                        prependInstructions.Add(il.Create(OpCodes.Stfld, prop.BackingField));
                    }
                    else
                    {
                        prependInstructions.Add(il.Create(OpCodes.Ldnull));
                        prependInstructions.Add(il.Create(OpCodes.Ldftn, prop.Getter));
                        prependInstructions.Add(il.Create(OpCodes.Newobj, funcDelegateCtor));
                        prependInstructions.Add(il.Create(OpCodes.Newobj, computedCtor));
                        prependInstructions.Add(il.Create(OpCodes.Stsfld, prop.BackingField));
                    }
                }

                for (int i = 0; i < prependInstructions.Count; i++)
                {
                    instructions.Insert(i, prependInstructions[i]);
                }

                prependInstructions.Clear();
            }
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

        private static bool InstructionsWillInvokeOwnInstanceConstructor(TypeDefinition type, Collection<Instruction> instructions)
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