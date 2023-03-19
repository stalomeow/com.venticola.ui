using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace VentiCola.UI.Bindings
{
    public struct TemplateInstance
    {
        private LayoutBindingAuthoring m_Authoring;
        private List<IBinding> m_Bindings;

        public TemplateInstance(LayoutBindingAuthoring authoring)
        {
            m_Authoring = authoring;
            m_Bindings = ListPool<IBinding>.Get();
        }

        public List<IBinding> Bindings => m_Bindings;

        public T GetAuthoringAs<T>() where T : LayoutBindingAuthoring
        {
            return m_Authoring as T;
        }

        public void InitializeBindingsAndRender(ILayoutBinding parent)
        {
            var transforms = new Stack<Transform>();
            transforms.Push(m_Authoring.transform);

            var components = ListPool<IBinding>.Get();
            var isRoot = true;

            while (transforms.TryPop(out Transform transform))
            {
                var go = transform.gameObject;

                if (!isRoot)
                {
                    if (!go.activeSelf)
                    {
                        continue;
                    }

                    if (go.TryGetComponent<LayoutBindingAuthoring>(out var layoutBindingAuthoring))
                    {
                        ILayoutBinding layoutBinding = layoutBindingAuthoring.ProvideBinding();
                        TemplateObject templateObject = layoutBindingAuthoring.ConvertToTemplate();

                        layoutBinding.InitializeObject(parent, templateObject);
                        BindingUtility.RenderIfDirty(layoutBinding);

                        m_Bindings.Add(layoutBinding);
                        continue; // layout binding 会自己处理子节点
                    }
                }

                go.GetComponents(components);

                for (int i = components.Count - 1; i >= 0; i--)
                {
                    if (components[i] is ILayoutBinding)
                    {
                        components.RemoveAt(i);
                        continue;
                    }

                    components[i].InitializeObject(parent);
                    BindingUtility.RenderIfDirty(components[i]);
                }

                m_Bindings.AddRange(components);

                // add children
                int childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    transforms.Push(transform.GetChild(i));
                }

                isRoot = false;
            }

            ListPool<IBinding>.Release(components);
        }

        public void ResetAllBindings()
        {
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                m_Bindings[i].ResetObject();
            }
        }

        public void Dispose(bool destroy)
        {
            ResetAllBindings();
            ListPool<IBinding>.Release(m_Bindings);

            if (destroy)
            {
                Object.Destroy(m_Authoring.gameObject);
            }

            m_Authoring = null;
            m_Bindings = null;
        }
    }
}
