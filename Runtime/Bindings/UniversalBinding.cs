using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    /// <summary>
    /// 非常泛用的 Binding
    /// </summary>
    public class UniversalBinding : BaseBinding
    {
        // 这个类型很常用，为了更好地重复利用其对象，该类型不再使用泛型
        // CustomArg 可以一定程度上消除委托中的 local variable capture，进而使委托对象也可以重复使用
        // 大多数情况下，CustomArg 存的都是引用类型对象，很少存值类型对象，所以不用太担心装箱和拆箱的开销

        private Action<UniversalBinding, IAnimationUpdater> m_RenderAction;
        private Action<UniversalBinding> m_DestroyAction;
        private object m_CustomArg0;
        private object m_CustomArg1;
        private object m_CustomArg2;
        private object m_CustomArg3;

        public object CustomArg0
        {
            get => m_CustomArg0;
            set => m_CustomArg0 = value;
        }

        public object CustomArg1
        {
            get => m_CustomArg1;
            set => m_CustomArg1 = value;
        }

        public object CustomArg2
        {
            get => m_CustomArg2;
            set => m_CustomArg2 = value;
        }

        public object CustomArg3
        {
            get => m_CustomArg3;
            set => m_CustomArg3 = value;
        }

        public void Initialize(
            GameObject mountTarget,
            Action<UniversalBinding> initAction = null,
            Action<UniversalBinding, IAnimationUpdater> renderAction = null,
            Action<UniversalBinding> destroyAction = null,
            object customArg0 = null,
            object customArg1 = null,
            object customArg2 = null,
            object customArg3 = null)
        {
            BaseInitialize(ref mountTarget, setDirty: (renderAction is not null));

            m_RenderAction = renderAction;
            m_DestroyAction = destroyAction;
            m_CustomArg0 = customArg0;
            m_CustomArg1 = customArg1;
            m_CustomArg2 = customArg2;
            m_CustomArg3 = customArg3;

            initAction?.Invoke(this);
        }

        protected override void OnDetach()
        {
            try
            {
                m_DestroyAction?.Invoke(this);
            }
            finally
            {
                m_RenderAction = null;
                m_DestroyAction = null;
                m_CustomArg0 = null;
                m_CustomArg1 = null;
                m_CustomArg2 = null;
                m_CustomArg3 = null;

                base.OnDetach();
            }
        }

        protected override void OnExecute(IAnimationUpdater animationUpdater)
        {
            m_RenderAction(this, animationUpdater);
        }
    }
}