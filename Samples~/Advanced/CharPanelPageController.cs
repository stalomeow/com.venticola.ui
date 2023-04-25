using UnityEngine;
using UnityEngine.EventSystems;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using VentiCola.UI.Bindings.Experimental;
using VentiCola.UI.Specialized;

namespace VentiColaTests.UI
{
    public enum TabType
    {
        Properties,
        Weapon,
        Talent,
        Info
    }

    public class TalentModel
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

    public class CharacterModel
    {
        [Reactive]
        public static ReactiveList<CharacterModel> Characters { get; set; }

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
        public ReactiveList<TalentModel> Talents { get; set; }

        [Reactive(LazyComputed = true)]
        public float ExpBarFillAmount => (float)Exp / MaxExp;

        [Reactive(LazyComputed = true)]
        public float LoveExpBarFillAmount => (float)LoveExp / LoveMaxExp;

        [Reactive(LazyComputed = true)]
        public Sprite Avatar => Resources.Load<Sprite>(AvatarPath);
    }

    public class CharPanelPageController : BaseUIPageController<CharPanelPage>
    {
        [Reactive]
        public TabType CurrentTab { get; set; }

        [Reactive]
        public CharacterModel CurrentChar { get; set; }

        [Reactive]
        private float PageAlpha { get; set; } = 0; // 默认值

        public CharPanelPageController()
        {
            Config.PrefabKey = "CharPanelPage";
        }

        private void OnDrag(PointerEventData eventData)
        {
            Transform character = GameObject.FindWithTag("Player").transform;
            character.Rotate(new Vector3(0, eventData.delta.x * -0.5f, 0));
        }

        private void SwitchTab(bool value, TabType tab)
        {
            if (!value)
            {
                return;
            }

            CurrentTab = tab;

            var playerRenderer = GameObject.FindWithTag("Player").GetComponent<MeshRenderer>();
            playerRenderer.material.color = tab switch
            {
                TabType.Properties => Color.blue,
                TabType.Weapon => Color.green,
                TabType.Talent => Color.red,
                TabType.Info => Color.black,
                _ => throw new System.NotImplementedException(),
            };
        }

        protected override void OnViewDidLoad()
        {
            Rect safeArea = Screen.safeArea;
            Vector2 tempPos;

            tempPos = View.CharInfoText.GetComponent<RectTransform>().anchoredPosition;
            tempPos.x = Mathf.Max(safeArea.x, tempPos.x);
            View.CharInfoText.GetComponent<RectTransform>().anchoredPosition = tempPos;

            tempPos = View.CloseButton.GetComponent<RectTransform>().anchoredPosition;
            tempPos.x = Mathf.Min(-safeArea.x, tempPos.x);
            View.CloseButton.GetComponent<RectTransform>().anchoredPosition = tempPos;

            tempPos = View.TabTogglePanel.GetComponent<RectTransform>().anchoredPosition;
            tempPos.x = Mathf.Max(safeArea.x, tempPos.x);
            View.TabTogglePanel.GetComponent<RectTransform>().anchoredPosition = tempPos;

            tempPos = View.PropertiesTab.GetComponent<RectTransform>().anchoredPosition;
            tempPos.x = Mathf.Min(-safeArea.x, tempPos.x);
            View.PropertiesTab.GetComponent<RectTransform>().anchoredPosition = tempPos;

            tempPos = View.WeaponTab.GetComponent<RectTransform>().anchoredPosition;
            tempPos.x = Mathf.Min(-safeArea.x, tempPos.x);
            View.WeaponTab.GetComponent<RectTransform>().anchoredPosition = tempPos;

            tempPos = View.TalentTab.GetComponent<RectTransform>().anchoredPosition;
            tempPos.x = Mathf.Min(-safeArea.x, tempPos.x);
            View.TalentTab.GetComponent<RectTransform>().anchoredPosition = tempPos;

            tempPos = View.InfoTab.GetComponent<RectTransform>().anchoredPosition;
            tempPos.x = Mathf.Min(-safeArea.x, tempPos.x);
            View.InfoTab.GetComponent<RectTransform>().anchoredPosition = tempPos;

            View.OnDragHandler = OnDrag;
        }

        protected override void OnViewWillUnload()
        {
            View.OnDragHandler = null;
        }

        protected override void OnViewAppear()
        {
            PageAlpha = 1;
            SwitchTab(true, CurrentTab); // 第一次只能手动调用
        }

        protected override void OnViewDisappear()
        {
            PageAlpha = 0;
        }

        protected override void SetUpViewBindings()
        {
            BindTopBar();
            BindTabs();

            View.canvasGroup.alpha(() => PageAlpha, in View.PageAlphaTransConfig);
            View.CloseButton.onClick(() => Singleton<ResourcesUIManager>.Instance.Close(this));

            View.PropertiesTab.ShowIf(() => CurrentTab == TabType.Properties, () =>
            {
                View.CharNameText.text(() => CurrentChar.Name);
                View.CharLevelText.text(() => $"等级{CurrentChar.Level} / {CurrentChar.MaxLevel}");

                View.CharExpBarInnerImage.fillAmount(() => CurrentChar.ExpBarFillAmount, View.ExpBarFillAmountTransConfig);
                View.CharExpBarLabelText.text(() => $"{CurrentChar.Exp} / {CurrentChar.MaxExp}");

                View.CharMaxHPValueText.text(() => CurrentChar.MaxHp.ToString());
                View.CharATKValueText.text(() => CurrentChar.ATK.ToString());
                View.CharDEFValueText.text(() => CurrentChar.DEF.ToString());

                View.CharLoveLevelValueText.text(() => CurrentChar.LoveLevel.ToString());
                View.CharLoveBarInnerImage.fillAmount(() => CurrentChar.LoveExpBarFillAmount, View.LoveBarFillAmountTransConfig);

                View.CharDescText.text(() => CurrentChar.Desc);
            });

            View.WeaponTab.ShowIf(() => CurrentTab == TabType.Weapon);

            View.TalentTab.ShowIf(() => CurrentTab == TabType.Talent, () =>
            {
                View.TalentItem.RepeatForEachOf(() => CurrentChar.Talents, (int index, TalentModel talentModel) =>
                {
                    View.TalentItemNameText.text(() => talentModel.Name);
                    View.TalentItemLevelText.text(() => $"Lv.{talentModel.Level}");
                    View.TalentItemIconImage.sprite(() => talentModel.Icon);
                });
            });

            View.InfoTab.ShowIf(() => CurrentTab == TabType.Info);
        }

        private void BindTopBar()
        {
            View.CharInfoText.text(() => $"{CurrentChar.ElementType}元素 / {CurrentChar.Name}");

            View.CharListItemObj.RepeatForEachOf(() => CharacterModel.Characters, (int index, CharacterModel charModel) =>
            {
                View.CharListItemImage.sprite(() => charModel.Avatar);
                View.CharListItemButton.onClick(() => CurrentChar = charModel);
            });
        }

        private void BindTabs()
        {
            View.PropertiesTabToggle
                .isOn(() => CurrentTab == TabType.Properties)
                .onValueChanged(value => SwitchTab(value, TabType.Properties));

            View.WeaponTabToggle
                .isOn(() => CurrentTab == TabType.Weapon)
                .onValueChanged(value => SwitchTab(value, TabType.Weapon));

            View.TalentTabToggle
                .isOn(() => CurrentTab == TabType.Talent)
                .onValueChanged(value => SwitchTab(value, TabType.Talent));

            View.InfoTabToggle
                .isOn(() => CurrentTab == TabType.Info)
                .onValueChanged(value => SwitchTab(value, TabType.Info));
        }
    }
}