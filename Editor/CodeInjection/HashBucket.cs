using Mono.Cecil.Cil;

namespace VentiColaEditor.UI.CodeInjection
{
    internal struct HashBucket<T>
    {
        public T[] Entries { get; }

        public Label[] Labels { get; }

        public HashBucket(ILProcessor il, T[] entries)
        {
            Entries = entries;
            Labels = new Label[entries.Length];

            for (int i = 0; i < Labels.Length; i++)
            {
                Labels[i] = il.DefineLabel();
            }
        }
    }
}