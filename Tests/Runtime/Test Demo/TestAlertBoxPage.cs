using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VentiCola.UI;
using VentiCola.UI.Bindings;

namespace VentiColaTests.UI
{
    public class TestAlertBoxPage : BaseUIPageView, IPointerClickHandler
    {
        [Header("Components")]
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

        public UnityAction<PointerEventData> OnClickHandler;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            OnClickHandler?.Invoke(eventData);
        }
    }
}