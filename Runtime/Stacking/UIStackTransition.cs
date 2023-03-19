using System;

namespace VentiCola.UI.Stacking
{
    internal abstract class UIStackTransition
    {
        protected readonly Action<UIPage, bool> TransitionEndCallback;
        protected UIPage TopPage;
        protected UIPage BottomPage;

        protected UIStackTransition()
        {
            TransitionEndCallback = OnPageDidTransition;
        }

        protected void BaseStartNew(UIPage topPage, UIPage bottomPage)
        {
            TopPage = topPage;
            BottomPage = bottomPage;

            // 先设置字段，再调用方法！保证回调方法一定是在初始化之后被调用！
            StartNewImpl();
        }

        protected virtual void CleanUp()
        {
            TopPage = null;
            BottomPage = null;
        }

        public abstract void ForceFinishIfNot();

        protected abstract void StartNewImpl();

        protected abstract void OnPageDidTransition(UIPage page, bool isInstant);
    }
}