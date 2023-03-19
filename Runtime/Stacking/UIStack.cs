using System;
using System.Collections.Generic;
using UnityEngine;

// 1. UI 的过渡动画一个一个单独执行。这样的好处是切换 UI 的时候只有一次 Layout Rebuild。
// 2. 保证栈中只有最顶层的 UI 是活动的，并且保证 UI 的加载顺序。这样的好处是避免多个页面意外重叠加载，或者以相反顺序加载。

namespace VentiCola.UI.Stacking
{
    internal class UIStack
    {
        private static class TransitionCache
        {
            private static UIStackPushTransition s_BlendPush;
            private static UIStackPushTransition s_SequencePush;
            private static UIStackPopTransition s_BlendPop;
            private static UIStackPopTransition s_SequencePop;

            public static UIStackPushTransition GetPushTransition(bool blendMode)
            {
                if (blendMode)
                {
                    return s_BlendPush ??= new UIStackPushTransition.Blend();
                }

                return s_SequencePush ??= new UIStackPushTransition.Sequence();
            }

            public static UIStackPopTransition GetPopTransition(bool blendMode)
            {
                if (blendMode)
                {
                    return s_BlendPop ??= new UIStackPopTransition.Blend();
                }

                return s_SequencePop ??= new UIStackPopTransition.Sequence();
            }
        }

        private struct Element
        {
            public string Key;
            public UIPage Value;
        }

        private int m_TopIndex;
        private Element[] m_Elements;
        private UIStackTransition m_LastTransition;
        private readonly int m_MinGrow;
        private readonly int m_MaxGrow;
        private readonly Func<UIPage, bool, bool> m_PageRequestCloseHandler;
        private readonly Action<string, UIPage> m_CloseCallback;

        public UIStack(int minGrow, int maxGrow, Action<string, UIPage> closeCallback)
        {
            if (minGrow < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minGrow));
            }

            if (maxGrow < minGrow)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGrow));
            }

            m_TopIndex = -1;
            m_Elements = Array.Empty<Element>();
            m_LastTransition = null;
            m_MinGrow = minGrow;
            m_MaxGrow = maxGrow;
            m_PageRequestCloseHandler = OnPageRequestClose;
            m_CloseCallback = closeCallback;
        }

        private void StartPushTransition(bool blendTransitions, ReactiveModel model)
        {
            m_LastTransition?.ForceFinishIfNot();

            UIPage topPage = m_Elements[m_TopIndex].Value;
            UIPage bottomPage = m_TopIndex > 0 ? m_Elements[m_TopIndex - 1].Value : null;

            var pushTransition = TransitionCache.GetPushTransition(blendTransitions);
            pushTransition.StartNew(topPage, bottomPage, model, m_PageRequestCloseHandler);
            m_LastTransition = pushTransition;
        }

        private void StartPopTransition(bool blendTransitions, string poppedPageKey, UIPage poppedPage)
        {
            m_LastTransition?.ForceFinishIfNot();

            UIPage bottomPage = m_TopIndex >= 0 ? m_Elements[m_TopIndex].Value : null;

            var popTransition = TransitionCache.GetPopTransition(blendTransitions);
            popTransition.StartNew(poppedPage, bottomPage, poppedPageKey, m_CloseCallback);
            m_LastTransition = popTransition;
        }

        public void Push(string key, UIPage page, ReactiveModel model, bool blendTransitions)
        {
            m_TopIndex++;
            EnsureArraySize();

            ref Element top = ref m_Elements[m_TopIndex];

            top.Key = key;
            top.Value = page;

            StartPushTransition(blendTransitions, model);
        }

        private bool OnPageRequestClose(UIPage page, bool blendTransitions)
        {
            // 只有最上层的页面才能被关闭！
            if (m_TopIndex < 0 || (GetTopPage() != page))
            {
                Debug.LogWarning("Could not close page. Reason: The page is not the top one on stack.", page);
                return false;
            }

            ref Element top = ref m_Elements[m_TopIndex];

            string topKey = top.Key;
            top = default;
            m_TopIndex--;

            StartPopTransition(blendTransitions, topKey, page);
            return true;
        }

        public void Clear(List<string> keys = null, List<ReactiveModel> models = null)
        {
            m_LastTransition?.ForceFinishIfNot();
            m_LastTransition = null;

            if (m_TopIndex < 0)
            {
                return;
            }

            for (int i = m_TopIndex; i >= 0; i--)
            {
                string key = m_Elements[i].Key;
                UIPage page = m_Elements[i].Value;

                keys?.Add(key);
                models?.Add(page.Model);

                page.Hide(true);
                page.DidClose();

                m_CloseCallback?.Invoke(key, page);
            }

            Array.Clear(m_Elements, 0, m_TopIndex + 1);

            m_TopIndex = -1;
        }

        public bool TryPeek(out UIPage page)
        {
            if (m_TopIndex < 0)
            {
                page = null;
                return false;
            }

            page = GetTopPage();
            return true;
        }

        private UIPage GetTopPage()
        {
            return m_Elements[m_TopIndex].Value;
        }

        private void EnsureArraySize()
        {
            if (m_TopIndex < m_Elements.Length)
            {
                return;
            }

            // 默认增长一倍
            int growSize = Mathf.Clamp(m_Elements.Length, m_MinGrow, m_MaxGrow);
            Array.Resize(ref m_Elements, m_Elements.Length + growSize);
        }
    }
}