using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using VentiCola.UI.Bindings;
using VentiCola.UI.Internal;
using static VentiColaEditor.UI.CodeInjection.MetaDataUtility;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

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

        private class ReactiveAttributeData
        {
            public bool LazyComputed { get; set; }

            public TypeReference EqualityComparer { get; set; }
        }

        public AssemblyDefinition Assembly { get; set; }

        public MethodReferenceCache Methods { get; set; }

        public Action<float> ProgressCallback { get; set; }

        public bool Execute()
        {
            var injectedAnyCode = false;
            var computedPropsBuffer = new List<ComputedPropInfo>();

            ForEachAllTypesIncludeNested(Assembly, (i, count, type) =>
            {
                ProgressCallback((float)(i + 1) / count);

                if (type.IsValueType || type.IsInterface)
                {
                    return;
                }

                injectedAnyCode |= InjectProperties(type, computedPropsBuffer);
            });

            return injectedAnyCode;
        }

        private bool InjectProperties(TypeDefinition type, List<ComputedPropInfo> computedPropsBuffer)
        {
            var injectedAnyCode = false;

            foreach (PropertyDefinition prop in type.Properties)
            {
                if (!IsReactiveProperty(prop, out ReactiveAttributeData attr))
                {
                    continue;
                }

                if (attr.LazyComputed)
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
                }
                else
                {
                    InjectAutoPropertyObserversField(prop, out FieldDefinition observersField);
                    InjectAutoPropertyComparerField(prop, attr.EqualityComparer, out FieldDefinition comparerField);
                    InjectAutoPropertyGetter(prop, observersField);
                    InjectAutoPropertySetter(prop, observersField, comparerField);
                }

                injectedAnyCode = true;
            }

            InjectComputedPropertyInitializations(type, computedPropsBuffer);
            computedPropsBuffer.Clear();
            return injectedAnyCode;
        }

        private static bool IsReactiveProperty(PropertyDefinition prop, out ReactiveAttributeData attr)
        {
            if (!HasCustomAttribute<ReactiveAttribute>(prop.CustomAttributes, out CustomAttribute customAttr))
            {
                attr = null;
                return false;
            }

            attr = new ReactiveAttributeData();

            for (int i = 0; i < customAttr.Properties.Count; i++)
            {
                var propArg = customAttr.Properties[i];
                var propInfo = attr.GetType().GetProperty(propArg.Name, BindingFlags.Public | BindingFlags.Instance);
                propInfo.SetValue(attr, propArg.Argument.Value);
            }

            if (attr.LazyComputed)
            {
                // 只对 getter 作要求
                bool validComputed = prop.GetMethod is not null
                    && prop.GetMethod.HasBody
                    && !IsCompilerGeneratedMethod(prop.GetMethod);

                if (!validComputed)
                {
                    attr = null;
                    Debug.LogWarningFormat("Property '{0}' should be a manually implemented property with at least a getter having body.", prop.FullName);
                }

                return validComputed;
            }

            // 必须是自动实现 get & set 属性
            bool valid = prop.GetMethod is not null
                && prop.GetMethod.HasBody
                && IsCompilerGeneratedMethod(prop.GetMethod)
                && prop.SetMethod is not null
                && prop.SetMethod.HasBody
                && IsCompilerGeneratedMethod(prop.SetMethod);

            if (!valid)
            {
                attr = null;
                Debug.LogWarningFormat("Property '{0}' should be an auto-implemented property with both a getter and a setter.", prop.FullName);
            }

            return valid;
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

        private static void InjectAutoPropertyComparerField(PropertyDefinition prop, TypeReference comparerType, out FieldDefinition field)
        {
            if (comparerType == null)
            {
                field = null;
                return;
            }

            // 因为 comparerType 是从 Attribute 参数里来的，所以此处不知道它是不是 ValueType
            // 换言之，即使 comparerType 是 ValueType，依然有 comparerType.IsValueType === false

            var fieldName = $"<{prop.Name}>k__EqComparer";
            var fieldAttr = FieldAttributes.Private;

            if (!prop.HasThis)
            {
                fieldAttr |= FieldAttributes.Static;
            }

            field = new FieldDefinition(fieldName, fieldAttr, comparerType);
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
                throw new MissingFieldException($"Can not find the backing-field of auto-implemented property '{prop.FullName}'!");
            }

            RemoveCustomAttributes(getter.CustomAttributes, typeof(CompilerGeneratedAttribute));
            instructions.Clear();

            ILProcessor il = getter.Body.GetILProcessor();

            il.Emit_Ldflda_Or_Ldsflda(observersField, prop.HasThis);
            il.Emit(OpCodes.Call, Methods.ChangeUtilityTryAddCurrentObserverMethod);

            il.Emit_Ldfld_Or_Ldsfld(backingField, prop.HasThis);
            il.Emit(OpCodes.Ret);
        }

        private void InjectAutoPropertySetter(PropertyDefinition prop, FieldReference observersField, FieldReference comparerField)
        {
            MethodDefinition setter = prop.SetMethod;
            Collection<Instruction> instructions = setter.Body.Instructions;
            FieldReference backingField = FindBackingFieldOfAutoProperty(instructions);

            if (backingField is null)
            {
                throw new MissingFieldException($"Can not find the backing-field of auto-implemented property '{prop.FullName}'!");
            }

            RemoveCustomAttributes(setter.CustomAttributes, typeof(CompilerGeneratedAttribute));
            instructions.Clear();

            ILProcessor il = setter.Body.GetILProcessor();

            il.Emit_Ldflda_Or_Ldsflda(backingField, prop.HasThis);
            il.Emit(prop.HasThis ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0);
            il.Emit_Ldfld_Or_Ldsfld(observersField, prop.HasThis);

            if (comparerField is not null)
            {
                il.Emit_Ldflda_Or_Ldsflda(comparerField, prop.HasThis);
                il.Emit(OpCodes.Call, Methods.MakeChangeUtilitySetWithNotifyMethodWithRefComparer(prop.PropertyType, comparerField.FieldType));
            }
            else
            {
                il.Emit(OpCodes.Call, Methods.MakeChangeUtilitySetWithNotifyMethod(prop.PropertyType));
            }

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
            if (computedProps.Any(c => !c.Prop.HasThis))
            {
                // Note: .ctor() 至少有一个，但 .cctor() 可能没有
                if (!type.Methods.Any(m => m.IsConstructor && m.IsStatic))
                {
                    var cctor = new MethodDefinition(".cctor",
                        MethodAttributes.Private      |
                        MethodAttributes.Static       |
                        MethodAttributes.HideBySig    |
                        MethodAttributes.SpecialName  |
                        MethodAttributes.RTSpecialName,
                        type.Module.TypeSystem.Void);
                    cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                    // 添加一个 .cctor()
                    type.Methods.Add(cctor);
                }
            }

            var prependInstructions = new List<Instruction>();

            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.IsConstructor)
                {
                    continue;
                }

                Collection<Instruction> instructions = method.Body.Instructions;

                if (method.HasThis && InstructionsWillInvokeOwnInstanceConstructor(type, instructions))
                {
                    continue; // 避免多次调用构造方法，重复初始化字段
                }

                ILProcessor il = method.Body.GetILProcessor();

                foreach (var prop in computedProps)
                {
                    if (prop.Prop.HasThis != method.HasThis)
                    {
                        continue;
                    }

                    // IL 指令没法复用，每次都得新创建
                    AddComputedPropertyInitInstructions(prependInstructions, il, prop);
                }

                for (int i = 0; i < prependInstructions.Count; i++)
                {
                    instructions.Insert(i, prependInstructions[i]);
                }

                prependInstructions.Clear();
            }
        }

        private void AddComputedPropertyInitInstructions(List<Instruction> list, ILProcessor il, ComputedPropInfo prop)
        {
            MethodReference funcDelegateCtor = Methods.MakeFuncDelegateConstructor(prop.Prop.PropertyType);
            MethodReference computedCtor = Methods.MakeLazyComputedPropertyCtor(prop.BackingField.FieldType);

            if (prop.Prop.HasThis)
            {
                list.Add(il.Create(OpCodes.Ldarg_0));
                list.Add(il.Create(OpCodes.Ldarg_0));
                list.Add(il.Create(OpCodes.Ldftn, prop.Getter));
                list.Add(il.Create(OpCodes.Newobj, funcDelegateCtor));
                list.Add(il.Create(OpCodes.Newobj, computedCtor));
                list.Add(il.Create(OpCodes.Stfld, prop.BackingField));
            }
            else
            {
                list.Add(il.Create(OpCodes.Ldnull));
                list.Add(il.Create(OpCodes.Ldftn, prop.Getter));
                list.Add(il.Create(OpCodes.Newobj, funcDelegateCtor));
                list.Add(il.Create(OpCodes.Newobj, computedCtor));
                list.Add(il.Create(OpCodes.Stsfld, prop.BackingField));
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