using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VentiCola.UI;
using VentiCola.UI.Bindings;

namespace VentiColaTests.UI
{
    public class CharPanelPage : BaseUIPageView, IDragHandler
    {
        public Button CloseButton;

        [Header("Top Bar")]
        public Text CharInfoText;
        public GameObject CharListItemObj;
        public Image CharListItemImage;
        public Button CharListItemButton;

        [Header("Tab Toggle Group")]
        public GameObject TabTogglePanel;
        public Toggle PropertiesTabToggle;
        public Toggle WeaponTabToggle;
        public Toggle TalentTabToggle;
        public Toggle InfoTabToggle;

        [Header("Properties Tab")]
        public GameObject PropertiesTab;
        public Text CharNameText;
        public Text CharLevelText;
        public Image CharExpBarInnerImage;
        public Text CharExpBarLabelText;
        public Text CharMaxHPValueText;
        public Text CharATKValueText;
        public Text CharDEFValueText;
        public Text CharLoveLevelValueText;
        public Image CharLoveBarInnerImage;
        public Text CharDescText;

        [Header("Weapon Tab")]
        public GameObject WeaponTab;

        [Header("Talent Tab")]
        public GameObject TalentTab;
        public GameObject TalentItem;
        public Text TalentItemNameText;
        public Text TalentItemLevelText;
        public Image TalentItemIconImage;

        [Header("Info Tab")]
        public GameObject InfoTab;

        [Header("Transition Configs")]
        public TransitionConfig PageAlphaTransConfig;
        public TransitionConfig ExpBarFillAmountTransConfig;
        public TransitionConfig LoveBarFillAmountTransConfig;

        public UnityAction<PointerEventData> OnDragHandler;

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            OnDragHandler?.Invoke(eventData);
        }
    }
}