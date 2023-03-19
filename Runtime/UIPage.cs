using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using VentiCola.UI.Bindings;
using VentiCola.UI.Internals;
using VentiCola.UI.Transitions;

namespace VentiCola.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPage : LayoutBindingAuthoring, ILayoutBinding, ICustomScope
    {
        internal bool IsCacheable
        {
            get => (m_Flags & UIPageFlags.FrequentlyUsed) != 0;
        }

        internal UIPageFlags Flags
        {
            get => m_Flags;
        }

        internal UIPageOpenMode OpenMode
        {
            get => m_OpenMode;
        }    

        public UIPageStatus Status
        {
            get => m_Status;
        }

        internal ReactiveModel Model
        {
            get => m_Model;
        }

        [Header("Configs")]

        [SerializeField]
        private UIPageFlags m_Flags = UIPageFlags.None;

        [SerializeField]
        private UIPageOpenMode m_OpenMode = UIPageOpenMode.Single;

        [SerializeField]
        [HideInInspector]
        private TransitionBase[] m_Transitions;


        [NonSerialized] private UIPageStatus m_Status = UIPageStatus.Inactive;
        [NonSerialized] private TransitionType m_TransitionType = TransitionType.Enter;
        [NonSerialized] private Action<UIPage, bool> m_TransitionEndCallback = null;
        [NonSerialized] private ReactiveModel m_Model = null;
        [NonSerialized] private Func<UIPage, bool, bool> m_RequestCloseHandler = null;
        [NonSerialized] private CanvasGroup m_CanvasGroup;
        [NonSerialized] private BaseRaycaster m_Raycaster;
        [NonSerialized] private TemplateObject m_Template;
        [NonSerialized] private TemplateInstance m_Instance;
        [NonSerialized] private HashSet<IBinding> m_DirtyBindings;


        T ICustomScope.GetVariable<T>(string name)
        {
            return (name is "Model") ? (T)(object)m_Model : default;
        }

#if UNITY_EDITOR
        (Type type, string name, string tooltip)[] ICustomScope.GetVarHintsInEditor()
        {
            var type = GetType();
            var attrs = (RequireModelAttribute[])type.GetCustomAttributes(typeof(RequireModelAttribute), true);

            if (attrs.Length == 0 || attrs[0].ModelType is null || !typeof(ReactiveModel).IsAssignableFrom(attrs[0].ModelType))
            {
                return Array.Empty<(Type, string, string)>();
            }

            return new (Type, string, string)[] { (attrs[0].ModelType, "Model", "The Model.") };
        }
#endif


        protected T GetModelAs<T>() where T : ReactiveModel
        {
            return m_Model as T;
        }

        public void RequestClose()
        {
            RequestClose(false, out _);
        }

        public void RequestClose(bool blendTransitions)
        {
            RequestClose(blendTransitions, out _);
        }

        public void RequestClose(bool blendTransitions, out bool success)
        {
            success = (m_RequestCloseHandler is not null) && m_RequestCloseHandler(this, blendTransitions);
        }


        internal void WillOpen(ReactiveModel model, Func<UIPage, bool, bool> requestCloseHandler)
        {
            m_Model = model;
            m_RequestCloseHandler = requestCloseHandler;
            m_Template = new TemplateObject(gameObject, true);
            m_Instance = m_Template.Instantiate();
            m_DirtyBindings = HashSetPool<IBinding>.Get();

            m_Instance.InitializeBindingsAndRender(this);

            //for (int i = 0; i < m_Instance.Bindings.Count; i++)
            //{
            //    dfs(m_Instance.Bindings[i], 1);
            //}

            OnWillOpen();

            gameObject.SetActive(true);
        }

        private void dfs(IBinding s, int depth)
        {
            Debug.Log($"{depth}: {s.GetType().Name}", s.GetOwnerGameObject());

            if (s is ILayoutBinding layoutBinding)
            {
                var children = new List<IBinding>();
                layoutBinding.GetChildBindings(children);

                for (int i = 0; i < children.Count; i++)
                {
                    dfs(children[i], depth + 1);
                }
            }
        }

        internal void DidClose()
        {
            gameObject.SetActive(false);

            OnDidClose();

            m_Template.Dispose();
            m_Instance.Dispose(false);
            HashSetPool<IBinding>.Release(m_DirtyBindings);

            m_Model = null;
            m_RequestCloseHandler = null;
            m_TransitionEndCallback = null;
            m_DirtyBindings = null;
        }

        internal void Refocus()
        {
            if (m_Raycaster != null)
            {
                m_Raycaster.enabled = true;
            }

            OnRefocus();
        }

        internal void Blur()
        {
            if (m_Raycaster != null)
            {
                m_Raycaster.enabled = false;
            }

            OnBlur();
        }

        internal void Show(bool instant, Action<UIPage, bool> callback = null)
        {
            StartTransitions(TransitionType.Enter, instant, callback);
        }

        internal void Hide(bool instant, Action<UIPage, bool> callback = null)
        {
            StartTransitions(TransitionType.Exit, instant, callback);
        }

        /// <summary>
        /// 开始执行过渡动画
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instant"></param>
        /// <param name="callback"></param>
        private void StartTransitions(TransitionType type, bool instant, Action<UIPage, bool> callback)
        {
            if (m_Status == UIPageStatus.Transiting)
            {
                // 当前执行的过渡动画和新的一样
                if (m_TransitionType == type)
                {
                    if (instant)
                    {
                        // 需要立即结束当前的动画。但是在调用回调时需要分情况
                        FinishTransitions(false); // 对于旧的回调，instant 为 false
                        callback?.Invoke(this, true); // 对于新的回调，instant 为 true
                    }
                    else
                    {
                        // 继续执行当前动画，把新的回调和之前的拼起来即可
                        m_TransitionEndCallback += callback;
                    }

                    return;
                }

                // 打断之前的动画，执行新的动画
                FinishTransitions(false);
            }

            Assert.AreNotEqual(UIPageStatus.Transiting, m_Status);

            // preparation
            if (type == TransitionType.Enter && m_Status == UIPageStatus.Inactive)
            {
                OnWillAppear();

                // show the view
                m_CanvasGroup.alpha = 1;
                m_CanvasGroup.blocksRaycasts = true;
                // m_CanvasGroup.interactable = true; // no need to do that...

                if (m_Raycaster != null)
                {
                    m_Raycaster.enabled = true;
                }
            }
            else if (type == TransitionType.Exit && m_Status == UIPageStatus.Active)
            {
                OnWillDisappear();
                StopAllCoroutines();
            }
            else
            {
                // no need to do transitions
                callback?.Invoke(this, instant);
                return;
            }

            // 设置状态
            m_Status = UIPageStatus.Transiting;
            m_TransitionType = type;
            m_TransitionEndCallback = callback;

            // 将自己放在当前层级最上方，保证让玩家看见
            transform.SetAsLastSibling();

            for (int i = 0; i < m_Transitions.Length; i++)
            {
                m_Transitions[i].BeginTransition(type, true, instant);
            }

            if (instant)
            {
                FinishTransitions(true);
            }
        }

        private void FinishTransitions(bool isInstant)
        {
            Assert.AreEqual(UIPageStatus.Transiting, m_Status);

            if (m_TransitionType == TransitionType.Enter)
            {
                m_Status = UIPageStatus.Active;

                OnDidAppear();
            }
            else // TransitionType.Exit
            {
                m_Status = UIPageStatus.Inactive;

                // hide the view
                m_CanvasGroup.alpha = 0;
                m_CanvasGroup.blocksRaycasts = false;
                // m_CanvasGroup.interactable = false; // no need to do that...

                if (m_Raycaster != null)
                {
                    m_Raycaster.enabled = false;
                }

                OnDidDisappear();
            }

            if (m_TransitionEndCallback is not null)
            {
                m_TransitionEndCallback(this, isInstant);
                m_TransitionEndCallback = null;
            }
        }

        private void UpdateTransitions()
        {
            bool allTransitionFinished = true;

            for (int i = 0; i < m_Transitions.Length; i++)
            {
                TransitionBase transition = m_Transitions[i];

                if (!transition.IsFinished)
                {
                    allTransitionFinished = false;
                    transition.UpdateTransition();
                }
            }

            if (allTransitionFinished)
            {
                FinishTransitions(false);
            }
        }


        public virtual void InvokeMethod<T>(string name, T firstArg, DynamicArgument[] resetArgs)
        {
            Debug.LogWarning($"Invoke method '{name}' in '{GetType().Name}' using reflection!", this);

            MethodInfo method = GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            object[] parameters = new object[resetArgs.Length + 1];
            parameters[0] = firstArg;

            for (int i = 0; i < resetArgs.Length; i++)
            {
                parameters[i + 1] = resetArgs[i].GetValue<object>();
            }

            method.Invoke(this, parameters);
        }

        public virtual T InvokeMethod<T>(string name, DynamicArgument[] args)
        {
            Debug.LogWarning($"Invoke method '{name}' in '{GetType().Name}' using reflection!", this);

            MethodInfo method = GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            object[] parameters = new object[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                parameters[i] = args[i].GetValue<object>();
            }

            return (T)method.Invoke(this, parameters);
        }


        private void Awake()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            m_Raycaster = GetComponent<BaseRaycaster>();
            gameObject.SetActive(false);
            OnCreate();
            Debug.Log("Page Awake");
        }

        private void Update()
        {
            if (m_Status == UIPageStatus.Transiting)
            {
                UpdateTransitions();
            }
            else if (m_Status == UIPageStatus.Active)
            {
                OnUpdate();
            }
        }

        private void LateUpdate()
        {
            if (m_Status != UIPageStatus.Active || m_DirtyBindings.Count == 0)
            {
                return;
            }

            // update dirty bindings

            foreach (IBinding binding in m_DirtyBindings)
            {
                BindingUtility.RenderIfDirty(binding);
            }

            m_DirtyBindings.Clear();
        }

        protected virtual void OnCreate() { }

        protected virtual void OnDestroy() { }

        protected virtual void OnUpdate() { }

        protected virtual void OnRefocus() { }

        protected virtual void OnBlur() { }

        protected virtual void OnWillOpen() { }

        protected virtual void OnDidClose() { }

        protected virtual void OnWillAppear() { }

        protected virtual void OnDidAppear() { }

        protected virtual void OnWillDisappear() { }

        protected virtual void OnDidDisappear() { }


        public sealed override TemplateObject ConvertToTemplate() => throw new NotSupportedException();

        public sealed override ILayoutBinding ProvideBinding() => throw new NotSupportedException();

        bool IChangeObserver.IsPassive => throw new NotSupportedException();

        int IReusableObject.Version => throw new NotSupportedException();

        bool ILayoutBinding.EnableChildBindingRendering => throw new NotSupportedException();

        HashSet<IBinding> ILayoutBinding.DirtyChildBindings => m_DirtyBindings;

        bool IBinding.IsSelfDirty { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        bool IBinding.IsFirstRendering { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        ILayoutBinding IBinding.ParentBinding => null;

        void IBinding.InitializeObject(ILayoutBinding parent, TemplateObject? templateObject) => throw new NotSupportedException();

        void IChangeObserver.NotifyChanged() => throw new NotSupportedException();

        void IReusableObject.ResetObject() => throw new NotSupportedException();

        void ILayoutBinding.GetChildBindings(List<IBinding> results) => throw new NotSupportedException();

        GameObject IBinding.GetOwnerGameObject() => throw new NotSupportedException();

        void IBinding.SetIsPassive(bool value) => throw new NotSupportedException();

        void IBinding.CalculateValues(out bool changed) => throw new NotSupportedException();

        void IBinding.RenderSelf() => throw new NotSupportedException();

        bool IBinding.HasCoveredAllBranchesSinceFirstRendering() => throw new NotSupportedException();
    }
}