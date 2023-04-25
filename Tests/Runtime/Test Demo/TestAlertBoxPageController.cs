using UnityEngine.Events;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using VentiCola.UI.Specialized;

namespace VentiColaTests.UI
{
    public class TestAlertBoxPageController : BaseUIPageController<TestAlertBoxPage>
    {
        // only expose parameters

        [Reactive]
        public string Title { get; set; }

        [Reactive]
        public string Message { get; set; }

        [Reactive]
        public string ConfirmButtonText { get; set; } = "确认";

        [Reactive]
        public string CancelButtonText { get; set; } = "取消";

        public UnityAction OnConfirm { get; set; }

        public UnityAction OnCancel { get; set; }

        [Reactive]
        private float PageAlpha { get; set; } = 0;

        [Reactive]
        private float BoxPositionY { get; set; } = -20;

        public TestAlertBoxPageController()
        {
            Config.PrefabKey = "Test Alert Box";
            Config.RenderOption = UIRenderOption.FullScreenBlurDynamic;
            Config.IsAdditive = true;
        }

        protected override void OnViewDidLoad()
        {
            View.OnClickHandler = e =>
            {
                if (e.rawPointerPress != View.gameObject)
                {
                    return;
                }

                Singleton<ResourcesUIManager>.Instance.Close(this);
            };
        }

        protected override void OnViewWillUnload()
        {
            View.OnClickHandler = null;
        }

        protected override void OnViewAppear()
        {
            PageAlpha = 1;
            BoxPositionY = 0;
        }

        protected override void OnViewDisappear()
        {
            PageAlpha = 0;
            BoxPositionY = -20;
        }

        protected override void SetUpViewBindings()
        {
            View.canvasGroup.alpha(() => PageAlpha, in View.AlphaTransConfig);
            View.BoxTransfrom.anchoredPositionY(() => BoxPositionY, in View.PositionTransConfig);

            View.TitleText.text(() => Title);
            View.MessageText.text(() => Message);
            View.ConfirmButtonText.text(() => ConfirmButtonText);
            View.CancelButtonText.text(() => CancelButtonText);

            View.ConfirmButton.onClick(() =>
            {
                OnConfirm?.Invoke();
                Singleton<ResourcesUIManager>.Instance.Close(this);
            });

            View.CancelButton.onClick(() =>
            {
                OnCancel?.Invoke();
                Singleton<ResourcesUIManager>.Instance.Close(this);
            });
        }
    }
}