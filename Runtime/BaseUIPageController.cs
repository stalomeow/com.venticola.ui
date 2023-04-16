using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VentiCola.UI.Bindings;
using VentiCola.UI.Internals;

namespace VentiCola.UI
{
    public abstract class BaseUIPageController : IAnimationUpdater
    {
        private UIPageControllerState m_State;
        private bool m_IsAdditive;
        private List<IAnimatable> m_Animatables;
        private BaseUIPageView m_View;
        private RootBinding m_ViewBindingRoot;
        private Action<BaseUIPageController, bool> m_DidCloseCallback;

        protected BaseUIPageController() { }

        public BaseUIPageView View => m_View;

        public UIPageControllerState State => m_State;

        public bool IsAdditive => m_IsAdditive;

        internal void WillOpen(bool isAdditive)
        {
            m_State = UIPageControllerState.WillOpen;
            m_IsAdditive = isAdditive;
            m_Animatables = ListPool<IAnimatable>.Get();
            OnWillOpen();
        }

        internal void SetView(BaseUIPageView view)
        {
            if (view == null)
            {
                m_View = null;
                m_State = UIPageControllerState.Error;
                return;
            }

            m_View = view;
            m_View.BindController(this);
        }

        private void CreateViewBindingsIfNot()
        {
            if (m_ViewBindingRoot != null)
            {
                return;
            }

            m_ViewBindingRoot = BaseBinding.Allocate<RootBinding>();
            m_ViewBindingRoot.Initialize(m_View.gameObject);

            using (new BaseBinding.ContextScope(m_ViewBindingRoot))
            {
                SetUpViewBindings();
            }

            // 在真正被渲染前立刻更新页面，避免之后出现两次大型的 Canvas Rebuild
            m_ViewBindingRoot.Execute(this);
            UpdateAllAnimatables(); // 第一次动画是立即完成的！
        }

        internal void Open()
        {
            m_State = UIPageControllerState.Active;
            CreateViewBindingsIfNot();
            OnDidOpen();
        }

        internal void Pause()
        {
            m_State = UIPageControllerState.Paused;

            if (m_View.Raycaster != null)
            {
                m_View.Raycaster.enabled = false;
            }

            OnPause();
        }

        internal void Resume()
        {
            m_State = UIPageControllerState.Active;

            if (m_View.Raycaster != null)
            {
                m_View.Raycaster.enabled = true;
            }

            OnResume();
        }

        internal void Close(Action<BaseUIPageController, bool> callback)
        {
            OnWillClose();
            m_State = UIPageControllerState.Closing;
            m_DidCloseCallback = callback;
        }

        private void FinishClose()
        {
            m_State = UIPageControllerState.Closed;

            OnDidClose();

            ListPool<IAnimatable>.Release(m_Animatables);
            m_Animatables = null;

            if (m_ViewBindingRoot != null)
            {
                m_ViewBindingRoot.Dispose();
                m_ViewBindingRoot = null;
            }

            if (m_DidCloseCallback is not null)
            {
                m_DidCloseCallback(this, true);
                m_DidCloseCallback = null;
            }

            // 保留 View
        }

        internal void Destroy(out BaseUIPageView view)
        {
            view = m_View;

            if (m_View != null)
            {
                m_View.CleanUpView();
                m_View = null;
            }

            OnDestroy();
        }

        internal void Update()
        {
            switch (m_State)
            {
                case UIPageControllerState.Active:
                case UIPageControllerState.Closing:
                    {
                        UpdateAllAnimatables(); // 渲染优先
                        OnUpdate();
                        break;
                    }
                case UIPageControllerState.Paused:
                    {
                        UpdateAllAnimatables(); // 只渲染动画
                        break;
                    }
            }
        }

        internal void LateUpdate()
        {
            switch (m_State)
            {
                case UIPageControllerState.Active:
                    {
                        OnLateUpdate();
                        m_ViewBindingRoot.Execute(this); // 在每帧所有操作之后渲染一次
                        break;
                    }
                case UIPageControllerState.Closing:
                    {
                        OnLateUpdate();
                        m_ViewBindingRoot.Execute(this); // 在每帧所有操作之后渲染一次

                        // 放在 Binding 更新以后
                        if (m_Animatables.Count == 0)
                        {
                            FinishClose();
                        }
                        break;
                    }
                case UIPageControllerState.Paused:
                    {
                        // 防止动画丢失
                        m_ViewBindingRoot.Execute(this); // 在每帧所有操作之后渲染一次
                        break;
                    }
            }
        }

        private void UpdateAllAnimatables()
        {
            int currentFrameCount = m_Animatables.Count;

            for (int i = currentFrameCount - 1; i >= 0; i--)
            {
                m_Animatables[i].UpdateAnimation(out bool isFinished);

                if (isFinished)
                {
                    ListUtility.FastRemoveAt(m_Animatables, i);
                }
            }
        }

        void IAnimationUpdater.RequestAnimationUpdate(IAnimatable animatable)
        {
            // Animation 数量一般不会很多（我猜的...
            if (m_Animatables.Contains(animatable))
            {
                return;
            }

            m_Animatables.Add(animatable);
        }

        protected void StopAllAnimatables()
        {
            m_Animatables.Clear();
        }

        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            return m_View.StartCoroutine(routine);
        }

        protected void StopCoroutine(Coroutine routine)
        {
            m_View.StopCoroutine(routine);
        }

        protected void StopAllCoroutines()
        {
            m_View.StopAllCoroutines();
        }

        protected void LogBindingTree()
        {
            Debug.Log("---------------------------- Binding Tree Start ----------------------------");
            DFS(m_ViewBindingRoot, 0);
            Debug.Log("----------------------------  Binding Tree End  ----------------------------");

            static void DFS(BaseBinding binding, int depth)
            {
                Debug.Log($"[Depth: {depth}] {TypeUtility.GetFriendlyTypeName(binding.GetType(), false)}", binding.MountTarget);

                for (int i = 0; i < binding.ChildCount; i++)
                {
                    DFS(binding.GetChild(i), depth + 1);
                }
            }
        }

        protected virtual void OnWillOpen() { }

        protected virtual void OnDidOpen() { }

        protected virtual void OnPause() { }

        protected virtual void OnResume() { }

        protected virtual void OnWillClose() { }

        protected virtual void OnDidClose() { }

        protected virtual void OnUpdate() { }

        protected virtual void OnLateUpdate() { }

        protected virtual void OnDestroy() { }

        protected virtual void SetUpViewBindings() { }
    }
}