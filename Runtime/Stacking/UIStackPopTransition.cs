using System;

namespace VentiCola.UI.Stacking
{
    internal abstract class UIStackPopTransition : UIStackTransition
    {
        protected string TopPageKey;
        protected Action<string, UIPage> TopPageCloseCallback;
        protected int RemainTaskCount;

        public void StartNew(UIPage topPage, UIPage bottomPage, string topPageKey, Action<string, UIPage> topPageCloseCallback)
        {
            TopPageKey = topPageKey;
            TopPageCloseCallback = topPageCloseCallback;

            if (bottomPage == null)
            {
                RemainTaskCount = 1;
            }
            else if (topPage.OpenMode == UIPageOpenMode.Additive)
            {
                bottomPage.Refocus();
                bottomPage = null;
                RemainTaskCount = 1;
            }
            else
            {
                RemainTaskCount = 2;
            }

            BaseStartNew(topPage, bottomPage);
        }

        protected override void CleanUp()
        {
            TopPageKey = null;
            TopPageCloseCallback = null;

            base.CleanUp();
        }

        public class Sequence : UIStackPopTransition
        {
            public override void ForceFinishIfNot()
            {
                if (RemainTaskCount > 0)
                {
                    TopPage.Hide(true); // 立刻结束第一个过渡动画

                    if (RemainTaskCount > 0)
                    {
                        BottomPage.Show(true); // 立刻结束第二个过渡动画
                    }
                }
            }

            protected override void StartNewImpl()
            {
                TopPage.Hide(false, TransitionEndCallback);
            }

            protected override void OnPageDidTransition(UIPage page, bool isInstant)
            {
                RemainTaskCount--;

                if (page == TopPage)
                {
                    TopPage.DidClose();
                    TopPageCloseCallback?.Invoke(TopPageKey, TopPage);
                }

                if (RemainTaskCount > 0)
                {
                    BottomPage.Show(false, TransitionEndCallback);
                }
                else
                {
                    CleanUp();
                }
            }
        }

        public class Blend : UIStackPopTransition
        {
            public override void ForceFinishIfNot()
            {
                if (RemainTaskCount > 0)
                {
                    TopPage.Hide(true); // 立刻结束第一个过渡动画

                    if (RemainTaskCount > 0)
                    {
                        BottomPage.Show(true); // 立刻结束第二个过渡动画
                    }
                }
            }

            protected override void StartNewImpl()
            {
                TopPage.Hide(false, TransitionEndCallback);

                if (BottomPage != null)
                {
                    BottomPage.Show(false, TransitionEndCallback);
                }
            }

            protected override void OnPageDidTransition(UIPage page, bool isInstant)
            {
                RemainTaskCount--;

                if (page == TopPage)
                {
                    TopPage.DidClose();
                    TopPageCloseCallback?.Invoke(TopPageKey, TopPage);
                }

                if (RemainTaskCount <= 0)
                {
                    CleanUp();
                }
            }
        }
    }
}