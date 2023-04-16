using UnityEngine;
using UnityEngine.EventSystems;
using VentiCola.UI;
using VentiCola.UI.Bindings;

namespace VentiColaTests.UI
{
    [CustomControllerForUIPage("Test Complex UI", CacheType = UICacheType.LastOnly, ClearHistory = false)]
    public class TestComplexPageController : BaseUIPageController
    {
        private SharedValue<TransitionConfig> m_ExpBarFillAmountSharedTransConfig = new();

        [Reactive]
        public TabType CurrentTab { get; set; }

        [Reactive]
        public TestCharacterModel CurrentChar { get; set; }

        [Reactive]
        private float PageAlpha { get; set; } = 0;

        private void OnDrag(PointerEventData eventData)
        {
            Debug.Log($"Drag: {eventData.delta.x}");
            //Transform character = GameObject.Find("ying_with_physics").transform;
            //character.Rotate(new Vector3(0, eventData.delta.x * -0.5f, 0));
        }

        private void SwitchTab(bool value, TabType tab)
        {
            if (!value)
            {
                return;
            }

            CurrentTab = tab;
            Debug.Log(tab);

            //var go = GameObject.Find("ying_with_physics");
            //var animator = go.GetComponent<Animator>();
            //var renderer = go.GetComponentInChildren<SkinnedMeshRenderer>();

            //if (tab == TabType.Properties)
            //{
            //    animator.SetInteger("animBaseInt", 7);
            //    renderer.SetBlendShapeWeight(16, 100);
            //    renderer.SetBlendShapeWeight(25, 100);
            //}
            //else
            //{
            //    renderer.SetBlendShapeWeight(16, 0);
            //    renderer.SetBlendShapeWeight(25, 0);
            //}

            //if (tab == TabType.Talent)
            //{
            //    animator.SetInteger("animBaseInt", 5);
            //}

            //if (tab == TabType.Weapon)
            //{
            //    animator.SetInteger("animBaseInt", 3);
            //    renderer.SetBlendShapeWeight(33, 40);
            //    renderer.SetBlendShapeWeight(41, 100);
            //}
            //else
            //{
            //    renderer.SetBlendShapeWeight(33, 0);
            //    renderer.SetBlendShapeWeight(41, 0);
            //}

            //if (tab == TabType.Info)
            //{
            //    animator.SetInteger("animBaseInt", 2);
            //    renderer.SetBlendShapeWeight(3, 100);
            //    renderer.SetBlendShapeWeight(30, 100);
            //    renderer.SetBlendShapeWeight(40, 100);
            //}
            //else
            //{
            //    renderer.SetBlendShapeWeight(3, 0);
            //    renderer.SetBlendShapeWeight(30, 0);
            //    renderer.SetBlendShapeWeight(40, 0);
            //}
        }

        protected override void OnDidOpen()
        {
            PageAlpha = 1;
        }

        protected override void OnResume()
        {
            PageAlpha = 1;
        }

        protected override void OnPause()
        {
            PageAlpha = 0;
        }

        protected override void OnWillClose()
        {
            PageAlpha = 0;
        }

        protected override void SetUpViewBindings()
        {
            var view = (TestComplexPage)View;

            BindTopBar(view);
            BindTabs(view);

            view.PageCanvasGroup.alpha(() => PageAlpha, in view.PageAlphaTransConfig);
            view.CloseButton.onClick(() => Test.UIManager.CloseTop());

            view.PropertiesTab.ShowIf(() => CurrentTab == TabType.Properties, () =>
            {
                view.CharNameText.text(() => CurrentChar.Name);
                view.CharLevelText.text(() => $"等级{CurrentChar.Level} / {CurrentChar.MaxLevel}");

                view.CharExpBarInnerImage.fillAmount(() => CurrentChar.ExpBarFillAmount, m_ExpBarFillAmountSharedTransConfig);
                view.CharExpBarLabelText.text(() => $"{CurrentChar.Exp} / {CurrentChar.MaxExp}");

                view.CharMaxHPValueText.text(() => CurrentChar.MaxHp.ToString());
                view.CharATKValueText.text(() => CurrentChar.ATK.ToString());
                view.CharDEFValueText.text(() => CurrentChar.DEF.ToString());

                view.CharLoveLevelValueText.text(() => CurrentChar.LoveLevel.ToString());
                view.CharLoveBarInnerImage.fillAmount(() => CurrentChar.LoveExpBarFillAmount, view.LoveBarFillAmountTransConfig);

                view.CharDescText.text(() => CurrentChar.Desc);
            });

            view.WeaponTab.ShowIf(() => CurrentTab == TabType.Weapon);

            view.TalentTab.ShowIf(() => CurrentTab == TabType.Talent, () =>
            {
                view.TalentItem.RepeatForEachOf(() => CurrentChar.Talents, (int index, TestTalentModel talentModel) =>
                {
                    view.TalentItemNameText.text(() => talentModel.Name);
                    view.TalentItemLevelText.text(() => $"Lv.{talentModel.Level}");
                    view.TalentItemIconImage.sprite(() => talentModel.Icon);
                });
            });

            view.InfoTab.ShowIf(() => CurrentTab == TabType.Info);

            m_ExpBarFillAmountSharedTransConfig.Value = view.ExpBarFillAmountTransConfig;
        }

        private void BindTopBar(TestComplexPage view)
        {
            view.CharInfoText.text(() => $"{CurrentChar.ElementType}元素 / {CurrentChar.Name}");

            view.CharListItemObj.RepeatForEachOf(() => TestCharDB.Characters, (int index, TestCharacterModel charModel) =>
            {
                view.CharListItemImage.sprite(() => charModel.Avatar);
                view.CharListItemButton.onClick(() => CurrentChar = charModel);
            });
        }

        private void BindTabs(TestComplexPage view)
        {
            view.PropertiesTabToggle
                .isOn(() => CurrentTab == TabType.Properties)
                .onValueChanged(value => SwitchTab(value, TabType.Properties));

            view.WeaponTabToggle
                .isOn(() => CurrentTab == TabType.Weapon)
                .onValueChanged(value => SwitchTab(value, TabType.Weapon));

            view.TalentTabToggle
                .isOn(() => CurrentTab == TabType.Talent)
                .onValueChanged(value => SwitchTab(value, TabType.Talent));

            view.InfoTabToggle
                .isOn(() => CurrentTab == TabType.Info)
                .onValueChanged(value => SwitchTab(value, TabType.Info));
        }
    }
}