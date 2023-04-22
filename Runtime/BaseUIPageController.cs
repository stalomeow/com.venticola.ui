using System.ComponentModel;

namespace VentiCola.UI
{
    public abstract class BaseUIPageController<T> : BaseUIPageControllerComplex<T> where T : BaseUIPageView
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

        protected virtual void OnViewAppear() { }

        protected virtual void OnViewDisappear() { }
    }
}