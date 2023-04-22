using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using VentiCola.UI.Internal;

namespace VentiColaEditor.UI.CodeInjection
{
    public class StringSwitchCaseEmitter<T>
    {
        // 返回值表示是否需要在后面插入转跳代码（例如 ret、br、等）
        public delegate bool CaseHandlerDelegate(ILProcessor il, MethodReferenceCache methods,
            LocalOrParameter switchKey, string caseString, T customCaseValue, object userData);

        // 返回值表示是否需要在后面插入转跳代码（例如 ret、br、等）
        public delegate bool NoCaseDelegate(ILProcessor il, MethodReferenceCache methods,
            LocalOrParameter switchKey, object userData);

        private struct CaseData
        {
            public uint Hash;
            public Label Label;
        }

        private ILProcessor m_IL;
        private TypeSystem m_TypeSystem;
        private MethodReferenceCache m_Methods;
        private LocalOrParameter m_SwitchKey;
        private IReadOnlyDictionary<string, T> m_StringValues;
        private object m_UserData;
        private Dictionary<uint, HashBucket<string>> m_StringHashMap;
        private CaseData[] m_SortedCases;

        public CaseHandlerDelegate EmitCaseHandlerDelegate { get; set; }

        public NoCaseDelegate EmitFallThroughDelegate { get; set; }

        public NoCaseDelegate EmitReturnDelegate { get; set; }

        public void Emit(
            ILProcessor il,
            TypeSystem typeSystem,
            MethodReferenceCache methods,
            LocalOrParameter switchKey,
            IReadOnlyDictionary<string, T> stringValues,
            object userData = null)
        {
            m_IL = il;
            m_TypeSystem = typeSystem;
            m_Methods = methods;
            m_SwitchKey = switchKey;
            m_StringValues = stringValues;
            m_UserData = userData;

            ComputeStringHashMapAndCases();

            if (m_SortedCases.Length == 0)
            {
                return;
            }

            // TODO: Add more optimizations.
            // 我想，绝大多数情况下，字符串的哈希值都比较稀疏，这里就直接用二分搜索了。
            // 然而在 Roslyn 的实现里，会做更多优化操作。（但我懒...
            bool useBinarySearch = ShouldGenerateHashTableSwitch(m_StringValues.Count);

            Label fallThroughLabel = il.DefineLabel();
            Label returnLabel = il.DefineLabel();

            if (useBinarySearch)
            {
                int keyHashLocalVarIndex = DefineKeyHashLocalVariable();
                EmitHashComputationForKey(keyHashLocalVarIndex);
                EmitBinarySearchForStringHash(0, m_SortedCases.Length - 1, keyHashLocalVarIndex, fallThroughLabel);
            }

            EmitBranchConditions(fallThroughLabel, useBinarySearch);
            EmitCaseHandlers(returnLabel);

            il.MarkLabel(fallThroughLabel);
            bool? jump = EmitFallThroughDelegate?.Invoke(il, methods, switchKey, userData);

            il.MarkLabel(returnLabel);

            if (EmitReturnDelegate is not null)
            {
                jump = EmitReturnDelegate(il, methods, switchKey, userData);
            }

            if (jump.GetValueOrDefault(true))
            {
                il.Emit(OpCodes.Ret);
            }

            il.ApplyAndOptimizeBranches();
        }

        private void ComputeStringHashMapAndCases()
        {
            m_StringHashMap = (
                from str in m_StringValues.Keys
                let hash = StringHashUtility.ComputeStringHash(str)
                group str by hash into entries
                select entries
            ).ToDictionary(
                entries => entries.Key,
                entries => new HashBucket<string>(m_IL, entries.ToArray())
            );

            m_SortedCases = (
                from hash in m_StringHashMap.Keys
                orderby hash ascending
                select new CaseData { Hash = hash, Label = m_IL.DefineLabel() }
            ).ToArray();
        }

        private int DefineKeyHashLocalVariable()
        {
            Collection<VariableDefinition> locals = m_IL.Body.Variables;
            locals.Add(new VariableDefinition(m_TypeSystem.UInt32));
            return locals.Count - 1;
        }

        private void EmitHashComputationForKey(int keyHashLocalVarIndex)
        {
            m_IL.Emit_Ldloc_Or_Ldarg(m_SwitchKey);
            m_IL.Emit(OpCodes.Call, m_Methods.StringHashMethod);
            m_IL.Emit_Stloc(keyHashLocalVarIndex);
        }

        private void EmitBinarySearchForStringHash(
            int low,
            int high,
            int keyHashLocalVarIndex,
            Label fallThroughLabel,
            Label? firstInstructionLabel = null)
        {
            if (high - low < 3)
            {
                for (int i = low; i <= high; i++)
                {
                    if (i == low && firstInstructionLabel.HasValue)
                    {
                        m_IL.MarkLabel(firstInstructionLabel.Value);
                    }

                    CaseData caseData = m_SortedCases[i];
                    m_IL.Emit_Ldloc(keyHashLocalVarIndex);
                    m_IL.Emit(OpCodes.Ldc_I4, (int)caseData.Hash);
                    m_IL.Emit(OpCodes.Beq, caseData.Label);
                }

                m_IL.Emit(OpCodes.Br, fallThroughLabel);
            }
            else
            {
                int mid = (high + low + 1) / 2 - 1;
                Label nextBlock = m_IL.DefineLabel();

                if (firstInstructionLabel.HasValue)
                {
                    m_IL.MarkLabel(firstInstructionLabel.Value);
                }

                CaseData caseData = m_SortedCases[mid];
                m_IL.Emit_Ldloc(keyHashLocalVarIndex);
                m_IL.Emit(OpCodes.Ldc_I4, (int)caseData.Hash);
                m_IL.Emit(OpCodes.Bgt_Un, nextBlock);

                EmitBinarySearchForStringHash(low, mid, keyHashLocalVarIndex, fallThroughLabel);
                EmitBinarySearchForStringHash(mid + 1, high, keyHashLocalVarIndex, fallThroughLabel, nextBlock);
            }
        }

        private void EmitBranchConditions(Label fallThroughLabel, bool switchCaseMode)
        {
            for (int i = 0; i < m_SortedCases.Length; i++)
            {
                var bucket = m_StringHashMap[m_SortedCases[i].Hash];

                for (int j = 0; j < bucket.Entries.Length; j++)
                {
                    if (switchCaseMode && j == 0)
                    {
                        m_IL.MarkLabel(m_SortedCases[i].Label);
                    }

                    m_IL.Emit_Ldloc_Or_Ldarg(m_SwitchKey);
                    m_IL.Emit(OpCodes.Ldstr, bucket.Entries[j]);
                    m_IL.Emit(OpCodes.Call, m_Methods.StringEqualityOperator);
                    m_IL.Emit(OpCodes.Brtrue, bucket.Labels[j]);
                }

                if (switchCaseMode || i == m_SortedCases.Length - 1)
                {
                    m_IL.Emit(OpCodes.Br, fallThroughLabel);
                }
            }
        }

        private void EmitCaseHandlers(Label returnLabel)
        {
            for (int i = 0; i < m_SortedCases.Length; i++)
            {
                var bucket = m_StringHashMap[m_SortedCases[i].Hash];

                for (int j = 0; j < bucket.Entries.Length; j++)
                {
                    m_IL.MarkLabel(bucket.Labels[j]);

                    string stringValue = bucket.Entries[j];
                    bool? jump = EmitCaseHandlerDelegate?.Invoke(m_IL, m_Methods,
                        m_SwitchKey, stringValue, m_StringValues[stringValue], m_UserData);

                    if (jump.GetValueOrDefault(true))
                    {
                        if (EmitReturnDelegate is null)
                        {
                            m_IL.Emit(OpCodes.Ret);
                        }
                        else
                        {
                            m_IL.Emit(OpCodes.Br, returnLabel);
                        }
                    }
                }
            }
        }

        private static bool ShouldGenerateHashTableSwitch(int stringCount)
        {
            return stringCount >= 7;
        }
    }
}