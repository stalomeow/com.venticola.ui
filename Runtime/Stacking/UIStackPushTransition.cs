using System;
using UnityEngine;

namespace VentiCola.UI.Stacking
{
    internal abstract class UIStackPushTransition : UIStackTransition
    {
        protected ReactiveModel TopPageModel;
        protected Func<UIPage, bool, bool> TopPageRequestCloseHandler;
        protected int RemainTaskCount;

        public void StartNew(UIPage topPage, UIPage bottomPage, ReactiveModel topPageModel, Func<UIPage, bool, bool> topPageRequestCloseHandler)
        {
            TopPageModel = topPageModel;
            TopPageRequestCloseHandler = topPageRequestCloseHandler;

            if (bottomPage == null)
            {
                RemainTaskCount = 1;
            }
            else if (topPage.OpenMode == UIPageOpenMode.Additive)
            {
                bottomPage.Blur();
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
            TopPageModel = null;
            TopPageRequestCloseHandler = null;
            RemainTaskCount = 0;

            base.CleanUp();
        }

        public class Sequence : UIStackPushTransition
        {
            public override void ForceFinishIfNot()
            {
                if (RemainTaskCount == 2)
                {
                    BottomPage.Hide(true); // 立即结束，执行下一个
                }

                if (RemainTaskCount == 1)
                {
                    TopPage.Show(true); // 立即结束
                }
            }

            protected override void StartNewImpl()
            {
                ExecuteNext();
            }

            protected override void OnPageDidTransition(UIPage page, bool isInstant)
            {
                RemainTaskCount--;
                ExecuteNext();
            }

            private void ExecuteNext()
            {
                switch (RemainTaskCount)
                {
                    case 0:
                        CleanUp();
                        break;

                    case 1:
                        TopPage.WillOpen(TopPageModel, TopPageRequestCloseHandler);
                        TopPage.Show(false, TransitionEndCallback);
                        break;

                    case 2:
                        BottomPage.Hide(false, TransitionEndCallback);
                        break;

                    default:
                        Debug.LogError($"Invalid Task Count: {RemainTaskCount}.");
                        goto case 0;
                }
            }
        }

        public class Blend : UIStackPushTransition
        {
            public override void ForceFinishIfNot()
            {
                if (RemainTaskCount > 0)
                {
                    TopPage.Show(true); // 立即结束

                    if (RemainTaskCount > 0)
                    {
                        BottomPage.Hide(true);
                    }
                }
            }

            protected override void StartNewImpl()
            {
                TopPage.WillOpen(TopPageModel, TopPageRequestCloseHandler);
                TopPage.Show(false, TransitionEndCallback);

                if (BottomPage != null)
                {
                    BottomPage.Hide(false, TransitionEndCallback);
                }
            }

            protected override void OnPageDidTransition(UIPage page, bool isInstant)
            {
                RemainTaskCount--;

                if (RemainTaskCount <= 0)
                {
                    CleanUp();
                }
            }
        }
    }
}