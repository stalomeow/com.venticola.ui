using UnityEngine;
using UnityEngine.Events;
using VentiCola.UI;
using VentiCola.UI.Bindings;

namespace VentiColaTests.UI
{
    [CustomControllerForUIPage("Test Alert Box")]
    public class TestAlertBoxPageController : BaseUIPageController
    {
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

        protected override void OnDidOpen()
        {
            PageAlpha = 1;
            BoxPositionY = 0;
        }

        protected override void OnPause()
        {
            PageAlpha = 0;
            BoxPositionY = -20;
        }

        protected override void OnResume()
        {
            PageAlpha = 1;
            BoxPositionY = 0;
        }

        protected override void OnWillClose()
        {
            PageAlpha = 0;
            BoxPositionY = -20;
        }

        protected override void SetUpViewBindings()
        {
            var view = (TestAlertBoxPage)View;

            view.PageCanvasGroup.alpha(() => PageAlpha, in view.AlphaTransConfig);
            view.BoxTransfrom.anchoredPositionY(() => BoxPositionY, in view.PositionTransConfig);

            view.TitleText.text(() => Title);
            view.MessageText.text(() => Message);
            view.ConfirmButtonText.text(() => ConfirmButtonText);
            view.CancelButtonText.text(() => CancelButtonText);

            view.ConfirmButton.onClick(() =>
            {
                OnConfirm?.Invoke();
                Test.UIManager.CloseTop();
            });

            view.CancelButton.onClick(() =>
            {
                OnCancel?.Invoke();
                Test.UIManager.CloseTop();
            });
        }
    }
}