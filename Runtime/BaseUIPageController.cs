using System.ComponentModel;

namespace VentiCola.UI
{
    public abstract class BaseUIPageController<TView> : BaseUIPageControllerComplex<TView> where TView : BaseUIPageView
    {
        protected BaseUIPageController() { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected sealed override void OnOpen()
        {
            OnViewAppear();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected sealed override void OnResume()
        {
            OnViewAppear();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected sealed override void OnPause()
        {
            OnViewDisappear();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected sealed override void OnClose()
        {
            OnViewDisappear();
        }

        /// <summary>
        /// 当页面视图出现在屏幕上时调用
        /// </summary>
        protected virtual void OnViewAppear() { }

        /// <summary>
        /// 当页面视图从屏幕上消失时调用
        /// </summary>
        protected virtual void OnViewDisappear() { }
    }
}