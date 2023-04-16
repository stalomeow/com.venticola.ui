using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace VentiColaEditor.UI.CodeInjection
{
    internal static class EmitUtility
    {
        public static void Emit_Ldc_I4(this ILProcessor il, int num)
        {
            switch (num)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case  0: il.Emit(OpCodes.Ldc_I4_0); return;
                case  1: il.Emit(OpCodes.Ldc_I4_1); return;
                case  2: il.Emit(OpCodes.Ldc_I4_2); return;
                case  3: il.Emit(OpCodes.Ldc_I4_3); return;
                case  4: il.Emit(OpCodes.Ldc_I4_4); return;
                case  5: il.Emit(OpCodes.Ldc_I4_5); return;
                case  6: il.Emit(OpCodes.Ldc_I4_6); return;
                case  7: il.Emit(OpCodes.Ldc_I4_7); return;
                case  8: il.Emit(OpCodes.Ldc_I4_8); return;
            }

            if (unchecked((sbyte)num == num))
            {
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)num);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, num);
            }
        }

        public static void Emit_Ldarg(this ILProcessor il, int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldarg_0); return;
                case 1: il.Emit(OpCodes.Ldarg_1); return;
                case 2: il.Emit(OpCodes.Ldarg_2); return;
                case 3: il.Emit(OpCodes.Ldarg_3); return;
            }

            if (index <= byte.MaxValue)
            {
                il.Emit(OpCodes.Ldarg_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Ldarg, index);
            }
        }

        public static void Emit_Ldloc(this ILProcessor il, int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldloc_0); return;
                case 1: il.Emit(OpCodes.Ldloc_1); return;
                case 2: il.Emit(OpCodes.Ldloc_2); return;
                case 3: il.Emit(OpCodes.Ldloc_3); return;
            }

            if (index <= byte.MaxValue)
            {
                il.Emit(OpCodes.Ldloc_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, index);
            }
        }

        public static void Emit_Stloc(this ILProcessor il, int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            switch (index)
            {
                case 0: il.Emit(OpCodes.Stloc_0); return;
                case 1: il.Emit(OpCodes.Stloc_1); return;
                case 2: il.Emit(OpCodes.Stloc_2); return;
                case 3: il.Emit(OpCodes.Stloc_3); return;
            }

            if (index <= byte.MaxValue)
            {
                il.Emit(OpCodes.Stloc_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Stloc, index);
            }
        }

        public static void Emit_Ldloc_Or_Ldarg(this ILProcessor il, LocalOrParameter localOrParameter)
        {
            if (localOrParameter.IsLocal)
            {
                il.Emit_Ldloc(localOrParameter.Index);
            }
            else
            {
                il.Emit_Ldarg(localOrParameter.Index);
            }
        }

        public static void Emit_Ldfld_Or_Ldsfld(this ILProcessor il, FieldReference field, bool hasThis)
        {
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
            }
            else
            {
                il.Emit(OpCodes.Ldsfld, field);
            }
        }

        public static void Emit_Ldflda_Or_Ldsflda(this ILProcessor il, FieldReference field, bool hasThis)
        {
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, field);
            }
            else
            {
                il.Emit(OpCodes.Ldsflda, field);
            }
        }
    }
}