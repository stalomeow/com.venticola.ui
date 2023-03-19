using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;

namespace VentiColaEditor.UI.CodeInjection
{
    internal static class BranchUtility
    {
        private struct BranchData
        {
            public int Pos;
            public OpCode OpCode;
            public Label Label;
        }


        [ThreadStatic] private static readonly List<int> s_Labels = new();
        [ThreadStatic] private static readonly List<BranchData> s_BranchInstructions = new();


        public static Label DefineLabel(this ILProcessor _)
        {
            s_Labels.Add(-1);
            return new Label(s_Labels.Count - 1);
        }

        public static void MarkLabel(this ILProcessor il, Label label)
        {
            int value = label.GetLabelValue();

            if (value < 0 || value >= s_Labels.Count)
            {
                throw new ArgumentException("Invalid Label.", nameof(label));
            }

            if (s_Labels[value] != -1)
            {
                throw new InvalidOperationException("Label has already been marked.");
            }

            s_Labels[value] = il.Body.Instructions.Count;
        }

        public static void Emit(this ILProcessor il, OpCode opCode, Label label)
        {
            if (opCode.OperandType == OperandType.ShortInlineBrTarget)
            {
                throw new ArgumentException("Do not use the short form of branch instruction.", nameof(opCode));
            }

            if (opCode.OperandType != OperandType.InlineBrTarget)
            {
                throw new ArgumentException("OpCode is not a branch instruction.", nameof(opCode));
            }

            Collection<Instruction> instructions = il.Body.Instructions;

            s_BranchInstructions.Add(new BranchData
            {
                Pos = instructions.Count,
                OpCode = opCode,
                Label = label
            });

            instructions.Add(il.Create(OpCodes.Nop)); // placeholder
        }

        public static void ApplyAndOptimizeBranches(this ILProcessor il)
        {
            Collection<Instruction> instructions = il.Body.Instructions;

            for (int i = 0; i < s_BranchInstructions.Count; i++)
            {
                BranchData data = s_BranchInstructions[i];
                int labelValue = data.Label.GetLabelValue();
                Instruction target = instructions[s_Labels[labelValue]];
                instructions[data.Pos] = il.Create(data.OpCode, target);
            }

            s_Labels.Clear();
            s_BranchInstructions.Clear();

            OptimizeBranches(instructions);
        }

        private static void ComputeOffsets(Collection<Instruction> instructions)
        {
            int offset = 0;

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }

        private static void OptimizeBranches(Collection<Instruction> instructions)
        {
            bool continueOptimizing;

            do
            {
                continueOptimizing = false;

                ComputeOffsets(instructions);

                for (int i = 0; i < instructions.Count; i++)
                {
                    var instruction = instructions[i];

                    if (instruction.OpCode.OperandType != OperandType.InlineBrTarget)
                    {
                        continue;
                    }

                    var target = (Instruction)instruction.Operand;
                    int offset = target.Offset - instruction.Offset;

                    if (unchecked((sbyte)offset == offset))
                    {
                        instruction.OpCode = GetShortBranch(instruction.OpCode);
                        continueOptimizing = true;
                        break;
                    }
                }
            } while (continueOptimizing);
        }

        /// <summary>
        /// Get a short form of the specified branch op-code.
        /// </summary>
        /// <param name="opCode">Branch op-code.</param>
        /// <returns>Short form of the branch op-code.</returns>
        /// <exception cref="ArgumentException">Specified <paramref name="opCode"/> is not a branch op-code.</exception>
        private static OpCode GetShortBranch(this OpCode opCode) => opCode.Code switch
        {
            Code.Br      => OpCodes.Br_S,
            Code.Brfalse => OpCodes.Brfalse_S,
            Code.Brtrue  => OpCodes.Brtrue_S,
            Code.Beq     => OpCodes.Beq_S,
            Code.Bge     => OpCodes.Bge_S,
            Code.Bgt     => OpCodes.Bgt_S,
            Code.Ble     => OpCodes.Ble_S,
            Code.Blt     => OpCodes.Blt_S,
            Code.Bne_Un  => OpCodes.Bne_Un_S,
            Code.Bge_Un  => OpCodes.Bge_Un_S,
            Code.Bgt_Un  => OpCodes.Bgt_Un_S,
            Code.Ble_Un  => OpCodes.Ble_Un_S,
            Code.Blt_Un  => OpCodes.Blt_Un_S,
            Code.Leave   => OpCodes.Leave_S,
            _ => throw new ArgumentException("Specified OpCode is not a long form branch OpCode.", nameof(opCode)),
        };
    }
}