using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI;
using VentiCola.UI.Bindings;

namespace VentiColaTests.UI
{
    public enum TabType
    {
        Properties,
        Weapon,
        Talent,
        Info
    }

    public class TestTalentModel
    {
        [Reactive]
        public string Name { get; set; }

        [Reactive]
        public int Level { get; set; }

        [Reactive]
        public string IconPath { get; set; }

        [Reactive(LazyComputed = true)]
        public Sprite Icon => Resources.Load<Sprite>(IconPath);
    }

    public class TestCharacterModel
    {
        [Reactive]
        public string Name { get; set; }

        [Reactive]
        public string ElementType { get; set; }

        [Reactive]
        public int Level { get; set; }

        [Reactive]
        public int MaxLevel { get; set; }

        [Reactive]
        public int Exp { get; set; }

        [Reactive]
        public int MaxExp { get; set; }

        [Reactive]
        public int MaxHp { get; set; }

        [Reactive]
        public int ATK { get; set; }

        [Reactive]
        public int DEF { get; set; }

        [Reactive]
        public int LoveLevel { get; set; }

        [Reactive]
        public int LoveExp { get; set; }

        [Reactive]
        public int LoveMaxExp { get; set; }

        [Reactive]
        public string Desc { get; set; }

        [Reactive]
        public string AvatarPath { get; set; }

        [Reactive]
        public ReactiveList<TestTalentModel> Talents { get; set; }

        [Reactive(LazyComputed = true)]
        public float ExpBarFillAmount => (float)Exp / MaxExp;

        [Reactive(LazyComputed = true)]
        public float LoveExpBarFillAmount => (float)LoveExp / LoveMaxExp;

        [Reactive(LazyComputed = true)]
        public Sprite Avatar => Resources.Load<Sprite>(AvatarPath);
    }

    public static class TestCharDB
    {
        [Reactive]
        public static ReactiveList<TestCharacterModel> Characters { get; set; }
    }

    public class TestComplexPage : BaseUIPageView
    {
        public CanvasGroup PageCanvasGroup;
        public Button CloseButton;

        [Header("Top Bar")]
        public Text CharInfoText;
        public GameObject CharListItemObj;
        public Image CharListItemImage;
        public Button CharListItemButton;

        [Header("Tab Toggle Group")]
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
    }
}