using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VentiCola.UI.Bindings.LowLevel;
using VentiCola.UI.Internal;

namespace VentiCola.UI
{
    public abstract class BaseUIPageControllerComplex<TView> : IViewController, IAnimationUpdater where TView : BaseUIPageView
    {
        /// <summary>
        /// 页面的配置信息
        /// </summary>
        protected UIConfig Config;

        private UIState m_State;
        private int m_StackIndex;
        private TView m_View;
        private List<IAnimatable> m_Animatables;
        private bool m_HasExternalAnimations;
        private RootBinding m_ViewBindingRoot;
        private Action<IViewController> m_ViewChangedCallback;
        private Action<IViewController> m_ClosingCompletedCallback;

        protected BaseUIPageControllerComplex() { }

        /// <summary>
        /// 页面的状态
        /// </summary>
        public UIState State => m_State;

        /// <summary>
        /// 页面视图
        /// </summary>
        protected TView View => m_View;

        ref readonly UIConfig IViewController.Config => ref Config;

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
            m_View = viewInstance.GetComponent<TView>();
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

        /// <summary>
        /// 开始更新一个可动画对象上的动画
        /// </summary>
        /// <param name="animatable"></param>
        protected void StartAnimatable(IAnimatable animatable)
        {
            if (m_Animatables.Contains(animatable))
            {
                return;
            }

            m_Animatables.Add(animatable);
        }

        /// <summary>
        /// 停止所有可动画对象
        /// </summary>
        protected void StopAllAnimatables()
        {
            m_Animatables.Clear();
        }

        /// <summary>
        /// 将页面上所有的绑定信息都打印到控制台
        /// </summary>
        protected void PrintBindingsToConsole()
        {
            Debug.Log("---------------------------- Binding Start ----------------------------", m_View);
            DFS(m_ViewBindingRoot, 0);
            Debug.Log("----------------------------  Binding End  ----------------------------", m_View);

            static void DFS(BaseBinding binding, int depth)
            {
                Debug.Log($"[Depth: {depth}] {TypeUtility.GetFriendlyTypeName(binding.GetType(), false)}", binding.MountTarget);

                for (int i = 0; i < binding.ChildCount; i++)
                {
                    DFS(binding.GetChild(i), depth + 1);
                }
            }
        }

        /// <summary>
        /// 开启一个协程
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        /// <seealso cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>
        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            return m_View.StartCoroutine(routine);
        }

        /// <summary>
        /// 停止指定的协程
        /// </summary>
        /// <param name="routine"></param>
        /// <seealso cref="MonoBehaviour.StopCoroutine(Coroutine)"/>
        protected void StopCoroutine(Coroutine routine)
        {
            m_View.StopCoroutine(routine);
        }

        /// <summary>
        /// 停止所有正在执行的协程
        /// </summary>
        /// <seealso cref="MonoBehaviour.StopAllCoroutines"/>
        protected void StopAllCoroutines()
        {
            m_View.StopAllCoroutines();
        }

        /// <summary>
        /// 通知当前页面发生了变化
        /// </summary>
        protected void NotifyViewChanged()
        {
            m_ViewChangedCallback?.Invoke(this);
        }

        /// <summary>
        /// 页面视图加载完成后调用。可以在这里分配资源
        /// </summary>
        protected virtual void OnViewDidLoad() { }

        /// <summary>
        /// 页面被打开时调用
        /// </summary>
        protected virtual void OnOpen() { }

        /// <summary>
        /// 页面被暂停时调用
        /// </summary>
        protected virtual void OnPause() { }

        /// <summary>
        /// 页面被恢复时调用
        /// </summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// 页面被关闭时调用
        /// </summary>
        protected virtual void OnClose() { }

        /// <summary>
        /// 页面视图即将被释放时调用。可以在这里释放资源
        /// </summary>
        protected virtual void OnViewWillUnload() { }

        /// <summary>
        /// Update，只有页面处于活跃状态时才会被调用
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// LateUpdate，只有页面处于活跃状态时才会被调用
        /// </summary>
        protected virtual void OnLateUpdate() { }

        /// <summary>
        /// 用于初始化页面绑定
        /// </summary>
        protected virtual void SetUpViewBindings() { }

        /// <summary>
        /// 用于检查外部动画的运行状态，例如 <see cref="Animator"/>。该方法每帧都会调用一次
        /// </summary>
        /// <param name="hasExternalAnimations">一个 bool 值，指示是否还有外部动画。如果所有外部动画都执行结束，设置为 false，否则设置为 true</param>
        protected virtual void UpdateExternalAnimations(out bool hasExternalAnimations)
        {
            hasExternalAnimations = false;
        }

        /// <summary>
        /// 在页面关闭的过程中，每帧的末尾都会调用一次。返回值指示当前页面是否可以结束关闭
        /// </summary>
        /// <returns>如果允许页面结束关闭，返回 true，否则返回 false</returns>
        protected virtual bool CanFinishClosing()
        {
            return true;
        }
    }
}