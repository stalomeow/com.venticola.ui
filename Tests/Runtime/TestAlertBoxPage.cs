using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI;
using VentiCola.UI.Bindings;

namespace VentiColaTests.UI
{
    public class TestAlertBoxPage : BaseUIPageView
    {
        [Header("Components")]
        public CanvasGroup PageCanvasGroup;
        public RectTransform BoxTransfrom;

        public Text TitleText;
        public Text MessageText;

        public Button ConfirmButton;
        public Text ConfirmButtonText;

        public Button CancelButton;
        public Text CancelButtonText;

        [Header("Transitions")]
        public TransitionConfig AlphaTransConfig;
        public TransitionConfig PositionTransConfig;
    }
}