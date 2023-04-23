using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VentiCola.UI.Bindings.LowLevel;
using VentiCola.UI.Internal;

namespace VentiCola.UI
{
    public abstract class BaseUIPageControllerComplex<T> : IViewController, IAnimationUpdater where T : BaseUIPageView
    {
        protected UIConfig Config;

        private UIState m_State;
        private int m_StackIndex;
        private T m_View;
        private List<IAnimatable> m_Animatables;
        private bool m_HasExternalAnimations;
        private RootBinding m_ViewBindingRoot;
        private Action<IViewController> m_ViewChangedCallback;
        private Action<IViewController> m_ClosingCompletedCallback;

        protected BaseUIPageControllerComplex() { }

        protected UIState State => m_State;

        protected T View => m_View;

        ref readonly UIConfig IViewController.Config => ref Config;

        UIState IViewController.State => m_State;

        int IViewController.StackIndex => m_StackIndex;

        GameObject IViewController.ViewInstance => m_View == null ? null : m_View.gameObject;

        event Action<IViewController> IViewController.OnViewChanged
        {
            add => m_ViewChangedCallback += value;
            remove => m_ViewChangedCallback -= value;
        }

        event Action<IViewController> IViewController.OnClosingCompleted
        {
            add => m_ClosingCompletedCallback += value;
            remove => m_ClosingCompletedCallback -= value;
        }

        void IViewController.InitView(GameObject viewInstance)
        {
            m_View = viewInstance.GetComponent<T>();
            m_Animatables = ListPool<IAnimatable>.Get();
            m_HasExternalAnimations = false;

            OnViewDidLoad();
            CreateViewBindings();

            m_View.BindController(this);
        }

        void IViewController.Open(int stackIndex)
        {
            m_State = UIState.Active;
            m_StackIndex = stackIndex;
            m_View.UpdateCanvas(stackIndex);
            m_View.UpdateCanvasGroup(true);

            OnOpen();
        }

        void IViewController.Pause()
        {
            m_State = UIState.Paused;
            m_View.UpdateCanvasGroup(false);

            OnPause();
        }

        void IViewController.Resume()
        {
            m_State = UIState.Active;
            m_View.UpdateCanvasGroup(true);

            OnResume();
        }

        void IViewController.Close()
        {
            m_State = UIState.Closing;
            m_View.UpdateCanvasGroup(false);

            OnClose();
        }

        void IAnimationUpdater.RequestAnimationUpdate(IAnimatable animatable)
        {
            if (m_Animatables.Contains(animatable))
            {
                return;
            }

            StartAnimatable(animatable);
        }

        void IViewController.Update()
        {
            switch (m_State)
            {
                case UIState.Active:
                case UIState.Closing:
                    {
                        // 执行逻辑 + 更新动画
                        OnUpdate();
                        UpdateAllAnimatables();
                        UpdateExternalAnimations(out m_HasExternalAnimations);
                        break;
                    }
                case UIState.Paused:
                    {
                        // 只更新动画
                        UpdateAllAnimatables();
                        UpdateExternalAnimations(out m_HasExternalAnimations);
                        break;
                    }
            }
        }

        void IViewController.LateUpdate()
        {
            switch (m_State)
            {
                case UIState.Active:
                    {
                        OnLateUpdate();
                        UpdateBindings(); // 在每帧所有操作之后渲染一次
                        break;
                    }
                case UIState.Closing:
                    {
                        OnLateUpdate();
                        UpdateBindings(); // 在每帧所有操作之后渲染一次

                        // 放在最最最后检查
                        if (CanFinishClosing() && !m_HasExternalAnimations && m_Animatables.Count == 0)
                        {
                            // 当所有任务、动画都结束后，彻底关闭页面
                            FinishClose();
                        }
                        break;
                    }
                case UIState.Paused:
                    {
                        // 这里也要更新 Binding，防止 OnPause() 里的过渡动画丢失
                        UpdateBindings();
                        break;
                    }
            }
        }

        private void CreateViewBindings()
        {
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

        private void FinishClose()
        {
            m_State = UIState.Closed;

            OnViewWillUnload();

            if (m_ClosingCompletedCallback is not null)
            {
                m_ClosingCompletedCallback(this);
                m_ClosingCompletedCallback = null;
            }

            // *DO NOT* reset Config field

            m_View.UnbindController();

            m_View = null;
            m_ViewChangedCallback = null;
            ListPool<IAnimatable>.Release(m_Animatables);
            m_Animatables = null;

            if (m_ViewBindingRoot != null)
            {
                m_ViewBindingRoot.Dispose();
                m_ViewBindingRoot = null;
            }
        }

        private void UpdateBindings()
        {
            // Binding Root 本身不会 Dirty，所以检查 Children 即可
            if (m_ViewBindingRoot.DirtyChildCount <= 0)
            {
                return;
            }

            m_ViewBindingRoot.Execute(this);
            NotifyViewChanged();
        }

        private void UpdateAllAnimatables()
        {
            int count = m_Animatables.Count;

            if (count <= 0)
            {
                return;
            }

            for (int i = count - 1; i >= 0; i--)
            {
                m_Animatables[i].UpdateAnimation(out bool isFinished);

                if (isFinished)
                {
                    m_Animatables.FastRemoveAt(i);
                }
            }

            NotifyViewChanged();
        }

        protected void StartAnimatable(IAnimatable animatable)
        {
            m_Animatables.Add(animatable);
        }

        protected void StopAllAnimatables()
        {
            m_Animatables.Clear();
        }

        protected void DebugLogBindings()
        {
            Debug.Log("---------------------------- Binding Start ----------------------------");
            DFS(m_ViewBindingRoot, 0);
            Debug.Log("----------------------------  Binding End  ----------------------------");

            static void DFS(BaseBinding binding, int depth)
            {
                Debug.Log($"[Depth: {depth}] {TypeUtility.GetFriendlyTypeName(binding.GetType(), false)}", binding.MountTarget);

                for (int i = 0; i < binding.ChildCount; i++)
                {
                    DFS(binding.GetChild(i), depth + 1);
                }
            }
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

        protected void NotifyViewChanged()
        {
            m_ViewChangedCallback?.Invoke(this);
        }

        /// <summary>
        /// 在这里分配资源
        /// </summary>
        protected virtual void OnViewDidLoad() { }

        protected virtual void OnOpen() { }

        protected virtual void OnPause() { }

        protected virtual void OnResume() { }

        protected virtual void OnClose() { }

        /// <summary>
        /// 在这里释放资源，不能再使用 View 了
        /// </summary>
        protected virtual void OnViewWillUnload() { }

        protected virtual void OnUpdate() { }

        protected virtual void OnLateUpdate() { }

        protected virtual void SetUpViewBindings() { }

        protected virtual void UpdateExternalAnimations(out bool hasExternalAnimations)
        {
            hasExternalAnimations = false;
        }

        protected virtual bool CanFinishClosing()
        {
            return true;
        }
    }
}