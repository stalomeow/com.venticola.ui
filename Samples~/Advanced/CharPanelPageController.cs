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

            View.canvasGroup.alpha(_ => PageAlpha, in View.PageAlphaTransConfig);
            View.CloseButton.onClick(_ => Singleton<ResourcesUIManager>.Instance.Close(this));

            View.PropertiesTab.ShowIf(_ => CurrentTab == TabType.Properties, () =>
            {
                View.CharNameText.text(_ => CurrentChar.Name);
                View.CharLevelText.text(_ => $"等级{CurrentChar.Level} / {CurrentChar.MaxLevel}");

                View.CharExpBarInnerImage.fillAmount(_ => CurrentChar.ExpBarFillAmount, View.ExpBarFillAmountTransConfig);
                View.CharExpBarLabelText.text(_ => $"{CurrentChar.Exp} / {CurrentChar.MaxExp}");

                View.CharMaxHPValueText.text(_ => CurrentChar.MaxHp.ToString());
                View.CharATKValueText.text(_ => CurrentChar.ATK.ToString());
                View.CharDEFValueText.text(_ => CurrentChar.DEF.ToString());

                View.CharLoveLevelValueText.text(_ => CurrentChar.LoveLevel.ToString());
                View.CharLoveBarInnerImage.fillAmount(_ => CurrentChar.LoveExpBarFillAmount, View.LoveBarFillAmountTransConfig);

                View.CharDescText.text(_ => CurrentChar.Desc);
            });

            View.WeaponTab.ShowIf(_ => CurrentTab == TabType.Weapon);

            View.TalentTab.ShowIf(_ => CurrentTab == TabType.Talent, () =>
            {
                View.TalentItem.RepeatForEachOf(_ => CurrentChar.Talents, (ForEachItem<TalentModel> item) =>
                {
                    View.TalentItemNameText.text(_ => item.Value.Name);
                    View.TalentItemLevelText.text(_ => $"Lv.{item.Value.Level}");
                    View.TalentItemIconImage.sprite(_ => item.Value.Icon);
                });
            });

            View.InfoTab.ShowIf(_ => CurrentTab == TabType.Info);
        }

        private void BindTopBar()
        {
            View.CharInfoText.text(_ => $"{CurrentChar.ElementType}元素 / {CurrentChar.Name}");

            View.CharListItemObj.RepeatForEachOf(_ => CharacterModel.Characters, (ForEachItem<CharacterModel> item) =>
            {
                View.CharListItemImage.sprite(_ => item.Value.Avatar);
                View.CharListItemButton.onClick(_ => CurrentChar = item.Value);
            });
        }

        private void BindTabs()
        {
            View.PropertiesTabToggle
                .isOn(_ => CurrentTab == TabType.Properties)
                .onValueChanged((_, value) => SwitchTab(value, TabType.Properties));

            View.WeaponTabToggle
                .isOn(_ => CurrentTab == TabType.Weapon)
                .onValueChanged((_, value) => SwitchTab(value, TabType.Weapon));

            View.TalentTabToggle
                .isOn(_ => CurrentTab == TabType.Talent)
                .onValueChanged((_ ,value) => SwitchTab(value, TabType.Talent));

            View.InfoTabToggle
                .isOn(_ => CurrentTab == TabType.Info)
                .onValueChanged((_, value) => SwitchTab(value, TabType.Info));
        }
    }
}