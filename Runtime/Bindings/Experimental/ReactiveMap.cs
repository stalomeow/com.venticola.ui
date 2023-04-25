// TODO
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using VentiCola.UI.Internal;

//namespace VentiCola.UI.Bindings.Experimental
//{
//    public class ReactiveMap<TKey, TValue> : IReactiveCollection, IDictionary<TKey, TValue>
//    {
//        private List<TKey> m_Keys;
//        private Dictionary<TKey, TValue> m_Dict;

//        public TValue this[TKey key]
//        {
//            get => throw new NotImplementedException();
//            set => throw new NotImplementedException();
//        }

//        ICollection<TKey> IDictionary<TKey, TValue>.Keys => throw new NotImplementedException();

//        ICollection<TValue> IDictionary<TKey, TValue>.Values => throw new NotImplementedException();

//        int ICollection<KeyValuePair<TKey, TValue>>.Count => throw new NotImplementedException();

//        int IReactiveCollection.Count => throw new NotImplementedException();

//        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => throw new NotImplementedException();

//        // bool IReactiveCollection.HasKey => true;
//        //
//        // Type IReactiveCollection.KeyType => typeof(TKey);
//        //
//        // Type IReactiveCollection.ValueType => typeof(TValue);

//        public KeyValuePair<TKey, TValue> GetAt(int index)
//        {
//            TKey key = m_Keys[index];
//            return new KeyValuePair<TKey, TValue>(key, m_Dict[key]);
//        }

//        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
//        {
//            throw new NotImplementedException();
//        }

//        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
//        {
//            throw new NotImplementedException();
//        }

//        T IReactiveCollection.GetKeyAt<T>(Index index)
//        {
//            return CastUtility.CastAny<TKey, T>(m_Keys[index]);
//        }

//        T IReactiveCollection.GetValueAt<T>(Index index)
//        {
//            return CastUtility.CastAny<TValue, T>(m_Dict[m_Keys[index]]);
//        }

//        public void CopyTo(ref IReactiveCollection destination, Range range)
//        {
//            throw new NotImplementedException();
//        }

//        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
//        {
//            throw new NotImplementedException();
//        }

//        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
//        {
//            throw new NotImplementedException();
//        }

//        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
//        {
//            throw new NotImplementedException();
//        }

//        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
//        {
//            throw new NotImplementedException();
//        }

//        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
//        {
//            throw new NotImplementedException();
//        }

//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            throw new NotImplementedException();
//        }

//        bool IDictionary<TKey, TValue>.Remove(TKey key)
//        {
//            throw new NotImplementedException();
//        }

//        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
//        {
//            throw new NotImplementedException();
//        }

//        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}