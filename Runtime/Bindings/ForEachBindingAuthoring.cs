using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using VentiCola.UI.Internals;

namespace VentiCola.UI.Bindings
{
    public class ForEachBinding : ILayoutBinding
    {
        private int m_Version = 0;
        private bool m_IsPassive;
        private bool m_IsDirty;
        private bool m_IsFirstRendering;
        private TemplateObject m_TemplateObject;
        private ForEachBindingAuthoring m_TemplateAuthoring;
        private ILayoutBinding m_Parent;
        private HashSet<IBinding> m_DirtyChildren;
        private List<TemplateInstance> m_Instances;

        public bool EnableChildBindingRendering => true;

        public HashSet<IBinding> DirtyChildBindings => m_DirtyChildren;

        public bool IsSelfDirty
        {
            get => m_IsDirty;
            set => m_IsDirty = value;
        }

        public bool IsFirstRendering
        {
            get => m_IsFirstRendering;
            set => m_IsFirstRendering = value;
        }

        public ILayoutBinding ParentBinding => m_Parent;

        public bool IsPassive => m_IsPassive;

        public int Version => m_Version;

        public void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null)
        {
            m_IsPassive = false;
            m_IsDirty = true;
            m_IsFirstRendering = true;

            m_TemplateObject = templateObject ?? throw new ArgumentNullException(nameof(templateObject));
            m_TemplateAuthoring = m_TemplateObject.GetRawAuthoringAs<ForEachBindingAuthoring>();
            m_Parent = parent;
            m_DirtyChildren = HashSetPool<IBinding>.Get();
            m_Instances = ListPool<TemplateInstance>.Get();
        }

        public void ResetObject()
        {
            m_Version++;

            for (int i = 0; i < m_Instances.Count; i++)
            {
                m_Instances[i].Dispose(true);
            }

            HashSetPool<IBinding>.Release(m_DirtyChildren);
            ListPool<TemplateInstance>.Release(m_Instances);

            m_TemplateObject.Dispose();
            m_TemplateObject = default;
            m_Parent = null;
            m_DirtyChildren = null;
        }

        public void CalculateValues(out bool changed)
        {
            changed = true;
        }

        public void RenderSelf()
        {
            PropertyLikeProxy collection = m_TemplateAuthoring.Collection;

            Type type = collection.PropertyType;
            int length;

            if (type == typeof(int))
            {
                length = collection.GetValue<int>();
            }
            else
            {
                length = collection.GetValue<IOrderedCollection>().Count;
            }

            if (m_Instances.Count > 0)
            {
                for (int i = 0; i < m_Instances.Count; i++)
                {
                    m_Instances[i].Dispose(true);
                }

                m_Instances.Clear();
            }

            m_DirtyChildren.Clear();

            for (int i = 0; i < length; i++)
            {
                TemplateInstance instance = m_TemplateObject.Instantiate();
                instance.GetAuthoringAs<ForEachBindingAuthoring>().SetData(i, this);
                instance.InitializeBindingsAndRender(this);
                m_Instances.Add(instance);
            }
        }

        public bool HasCoveredAllBranchesSinceFirstRendering()
        {
            return m_TemplateAuthoring.Collection.IsRealProperty;
        }

        public void NotifyChanged()
        {
            BindingUtility.SetDirty(this);
        }

        public void GetChildBindings(List<IBinding> results)
        {
            for (int i = 0; i < m_Instances.Count; i++)
            {
                results.AddRange(m_Instances[i].Bindings);
            }
        }

        public GameObject GetOwnerGameObject()
        {
            return m_TemplateObject.RawGameObject;
        }

        public void SetIsPassive(bool value)
        {
            m_IsPassive = value;
        }
    }

    [AddComponentMenu("UI Bindings/For Each Item in <Collection>")]
    public class ForEachBindingAuthoring : LayoutBindingAuthoring, ICustomScope
    {
        [SerializeField]
        [FormerlySerializedAs("m_Collection")]
        [PropertyTypeConstraints(typeof(ReactiveArray<>))]
        [PropertyTypeConstraints(typeof(ReactiveMap<,>))]
        [PropertyTypeConstraints(typeof(int))]
        internal PropertyLikeProxy Collection;

        [NonSerialized] private int m_Index;
        [NonSerialized] private ForEachBinding m_Binding;

        public override ILayoutBinding ProvideBinding()
        {
            return new ForEachBinding();
        }

        public override TemplateObject ConvertToTemplate()
        {
            return new TemplateObject(gameObject, false);
        }

        internal void SetData(int index, ForEachBinding binding)
        {
            m_Index = index;
            m_Binding = binding;
        }

#if UNITY_EDITOR
        (Type type, string name, string tooltip)[] ICustomScope.GetVarHintsInEditor()
        {
            Type collectionType = Collection.PropertyType;

            if (collectionType == null)
            {
                return Array.Empty<(Type, string, string)>();
            }
            else if (collectionType == typeof(int))
            {
                return new (Type, string, string)[]
                {
                    (typeof(int), "Index", "Current index."),
                    (typeof(int), "Item", "Current array element.")
                };
            }
            else if (TypeUtility.IsDerivedFromSpecificGenericType(collectionType, typeof(ReactiveArray<>), out Type[] arrayArgs))
            {
                return new (Type, string, string)[]
                {
                    (typeof(int), "Index", "Current index."),
                    (arrayArgs[0], "Item", "Current array element.")
                };
            }
            else if (TypeUtility.IsDerivedFromSpecificGenericType(collectionType, typeof(ReactiveMap<,>), out Type[] mapArgs))
            {
                return new (Type, string, string)[]
                {
                    (typeof(int), "Index", "Current index."),
                    (mapArgs[0], "Key", "Current map key."),
                    (mapArgs[1], "Item", "Current map item.")
                };
            }
            else
            {
                Debug.LogError($"Wrong Collection Type: {TypeUtility.GetFriendlyTypeName(collectionType, true)}", this);
                return Array.Empty<(Type, string, string)>();
            }
        }
#endif

        T ICustomScope.GetVariable<T>(string name)
        {
            int index = m_Index;

            if (name is "Index")
            {
                return CastUtility.CastValueType<int, T>(index);
            }
            else if (name is "Key")
            {
                ChangeUtility.BeginObservedRegion(m_Binding);

                try
                {
                    var collection = Collection.GetValue<IOrderedCollection>();
                    return collection.CastAndGetKeyAt<T>(index);
                }
                finally
                {
                    ChangeUtility.EndObservedRegion(m_Binding);
                }
            }
            else if (name is "Item")
            {
                ChangeUtility.BeginObservedRegion(m_Binding);

                try
                {
                    Type type = Collection.PropertyType;

                    if (type == null)
                    {
                        throw new InvalidOperationException();
                    }
                    else if (type == typeof(int))
                    {
                        int count = Collection.GetValue<int>();
                        int result = count > 0 ? (index + 1) : 0;
                        return CastUtility.CastValueType<int, T>(result);
                    }
                    else
                    {
                        var collection = Collection.GetValue<IOrderedCollection>();
                        return collection.CastAndGetValueAt<T>(index);
                    }
                }
                finally
                {
                    ChangeUtility.EndObservedRegion(m_Binding);
                }
            }

            return default;
        }
    }
}