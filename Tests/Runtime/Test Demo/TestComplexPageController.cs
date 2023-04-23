using UnityEngine;
using UnityEngine.EventSystems;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using VentiCola.UI.Bindings.Experimental;
using VentiCola.UI.Specialized;

namespace VentiColaTests.UI
{
    public class TestComplexPageController : BaseUIPageController<TestComplexPage>
    {
        private SharedValue<TransitionConfig> m_ExpBarFillAmountSharedTransConfig = new();

        [Reactive]
        public TabType CurrentTab { get; set; }

        [Reactive]
        public TestCharacterModel CurrentChar { get; set; }

        [Reactive]
        private float PageAlpha { get; set; } = 0;

        public TestComplexPageController()
        {
            Config.PrefabKey = "Test Complex UI";
        }

        private void OnDrag(PointerEventData eventData)
        {
            Transform character = GameObject.Find("ying_with_physics").transform;
            character.Rotate(new Vector3(0, eventData.delta.x * -0.5f, 0));
        }

        private void SwitchTab(bool value, TabType tab)
        {
            if (!value)
            {
                return;
            }

            CurrentTab = tab;
            Debug.Log(tab);

            var go = GameObject.Find("ying_with_physics");
            var animator = go.GetComponent<Animator>();
            var renderer = go.GetComponentInChildren<SkinnedMeshRenderer>();

            if (tab == TabType.Properties)
            {
                animator.SetInteger("animBaseInt", 7);
                renderer.SetBlendShapeWeight(16, 100);
                renderer.SetBlendShapeWeight(25, 100);
            }
            else
            {
                renderer.SetBlendShapeWeight(16, 0);
                renderer.SetBlendShapeWeight(25, 0);
            }

            if (tab == TabType.Talent)
            {
                animator.SetInteger("animBaseInt", 5);
            }

            if (tab == TabType.Weapon)
            {
                animator.SetInteger("animBaseInt", 3);
                renderer.SetBlendShapeWeight(33, 40);
                renderer.SetBlendShapeWeight(41, 100);
            }
            else
            {
                renderer.SetBlendShapeWeight(33, 0);
                renderer.SetBlendShapeWeight(41, 0);
            }

            if (tab == TabType.Info)
            {
                animator.SetInteger("animBaseInt", 2);
                renderer.SetBlendShapeWeight(3, 100);
                renderer.SetBlendShapeWeight(30, 100);
                renderer.SetBlendShapeWeight(40, 100);
            }
            else
            {
                renderer.SetBlendShapeWeight(3, 0);
                renderer.SetBlendShapeWeight(30, 0);
                renderer.SetBlendShapeWeight(40, 0);
            }
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
            // View.Animator.SetTrigger("Open");
        }

        protected override void OnViewDisappear()
        {
            PageAlpha = 0;
            // View.Animator.SetTrigger("Close");
        }

        protected override void SetUpViewBindings()
        {
            BindTopBar();
            BindTabs();

            View.PageCanvasGroup.alpha(() => PageAlpha, in View.PageAlphaTransConfig);
            View.CloseButton.onClick(() => Singleton<ResourcesUIManager>.Instance.Close(this));

            View.PropertiesTab.ShowIf(() => CurrentTab == TabType.Properties, () =>
            {
                View.CharNameText.text(() => CurrentChar.Name);
                View.CharLevelText.text(() => $"等级{CurrentChar.Level} / {CurrentChar.MaxLevel}");

                View.CharExpBarInnerImage.fillAmount(() => CurrentChar.ExpBarFillAmount, m_ExpBarFillAmountSharedTransConfig);
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
                View.TalentItem.RepeatForEachOf(() => CurrentChar.Talents, (int index, TestTalentModel talentModel) =>
                {
                    View.TalentItemNameText.text(() => talentModel.Name);
                    View.TalentItemLevelText.text(() => $"Lv.{talentModel.Level}");
                    View.TalentItemIconImage.sprite(() => talentModel.Icon);
                });
            });

            View.InfoTab.ShowIf(() => CurrentTab == TabType.Info);

            m_ExpBarFillAmountSharedTransConfig.Value = View.ExpBarFillAmountTransConfig;
        }

        private void BindTopBar()
        {
            View.CharInfoText.text(() => $"{CurrentChar.ElementType}元素 / {CurrentChar.Name}");

            View.CharListItemObj.RepeatForEachOf(() => TestCharDB.Characters, (int index, TestCharacterModel charModel) =>
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