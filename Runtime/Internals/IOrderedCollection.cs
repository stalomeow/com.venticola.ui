using System;

namespace VentiCola.UI.Internals
{
    public interface IOrderedCollection
    {
        int Count { get; }

        bool HasKey { get; }

        Type KeyType { get; }

        Type ValueType { get; }

        T CastAndGetKeyAt<T>(int index);

        T CastAndGetValueAt<T>(int index);
    }
}
