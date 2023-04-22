using System;

namespace VentiColaEditor.UI.CodeInjection
{
    public readonly struct Label : IEquatable<Label>
    {
        private readonly int m_Label;

        public Label(int label)
        {
            m_Label = label;
        }

        public int GetLabelValue()
        {
            return m_Label;
        }

        public override int GetHashCode()
        {
            return m_Label;
        }

        public override bool Equals(object obj)
        {
            return (obj is Label label) && Equals(label);
        }

        public bool Equals(Label other)
        {
            return m_Label == other.m_Label;
        }

        public static bool operator == (Label lhs, Label rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator != (Label lhs, Label rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}