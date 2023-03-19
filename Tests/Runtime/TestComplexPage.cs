using UnityEngine;
using UnityEngine.EventSystems;
using VentiCola.UI;

namespace VentiColaTests.UI
{
    public enum TabType
    {
        Properties,
        Weapon,
        Talent,
        Info
    }

    public class TestTalentModel : ReactiveModel
    {
        public string Name { get; set; }

        public int Level { get; set; }

        public string IconPath { get; set; }

        [LazyComputed(NoBranches = true)]
        public Sprite Icon => Resources.Load<Sprite>(IconPath);
    }


    public class TestCharacterModelBase : ReactiveModel
    {
        public string Name { get; set; }

        public string ElementType { get; set; }

        public int Level { get; set; }
    }

    public class TestCharacterModel : TestCharacterModelBase
    {
        public int MaxLevel { get; set; }

        public int Exp { get; set; }

        public int MaxExp { get; set; }

        public int MaxHp { get; set; }

        public int ATK { get; set; }

        public int DEF { get; set; }

        public int LoveLevel { get; set; }

        public int LoveExp { get; set; }

        public int LoveMaxExp { get; set; }

        public string Desc { get; set; }

        public string AvatarPath { get; set; }

        public ReactiveArray<TestTalentModel> Talents { get; set; }

        // 极其简单的计算属性，不需要转成 LazyComputed
        public float ExpBarFillAmout => (float)Exp / MaxExp;

        public float LoveExpBarFillAmout => (float)LoveExp / LoveMaxExp;

        [LazyComputed(NoBranches = true)]
        public Sprite Avatar => Resources.Load<Sprite>(AvatarPath);

        //public override void Set<T>(string propertyName, T value)
        //{
        //    switch (propertyName)
        //    {
        //        case "LoveExp":
        //            LoveExp = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "Name":
        //            Name = (string)(object)value;
        //            return;

        //        case "LoveLevel":
        //            LoveLevel = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "MaxHp":
        //            MaxHp = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "AvatarPath":
        //            AvatarPath = (string)(object)value;
        //            return;

        //        case "Level":
        //            Level = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "DEF":
        //            DEF = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "ATK":
        //            ATK = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "ElementType":
        //            ElementType = (string)(object)value;
        //            return;

        //        case "LoveMaxExp":
        //            LoveMaxExp = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "MaxExp":
        //            MaxExp = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "MaxLevel":
        //            MaxLevel = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "Talents":
        //            Talents = (ReactiveArray<TestTalentModel>)(object)value;
        //            return;

        //        case "Exp":
        //            Exp = CastUtility.CastAny<T, int>(value);
        //            return;

        //        case "Desc":
        //            Desc = (string)(object)value;
        //            return;
        //    }

        //    base.Set<T>(propertyName, value);
        //}

        //public override T Get<T>(string propertyName)
        //{
        //    switch (propertyName)
        //    {
        //        case "LoveExp":
        //            return CastUtility.CastValueType<int, T>(LoveExp);
        //        case "Name":
        //            return (T)(object)Name;
        //        case "LoveLevel":
        //            return CastUtility.CastValueType<int, T>(LoveLevel);
        //        case "MaxHp":
        //            return CastUtility.CastValueType<int, T>(MaxHp);
        //        case "AvatarPath":
        //            return (T)(object)AvatarPath;
        //        case "Level":
        //            return CastUtility.CastValueType<int, T>(Level);
        //        case "DEF":
        //            return CastUtility.CastValueType<int, T>(DEF);
        //        case "LoveExpBarFillAmout":
        //            return CastUtility.CastValueType<float, T>(LoveExpBarFillAmout);
        //        case "ATK":
        //            return CastUtility.CastValueType<int, T>(ATK);
        //        case "ElementType":
        //            return (T)(object)ElementType;
        //        case "Avatar":
        //            return (T)(object)Avatar;
        //        case "LoveMaxExp":
        //            return CastUtility.CastValueType<int, T>(LoveMaxExp);
        //        case "ExpBarFillAmout":
        //            return CastUtility.CastValueType<float, T>(ExpBarFillAmout);
        //        case "MaxExp":
        //            return CastUtility.CastValueType<int, T>(MaxExp);
        //        case "MaxLevel":
        //            return CastUtility.CastValueType<int, T>(MaxLevel);
        //        case "Talents":
        //            return (T)(object)Talents;
        //        case "Exp":
        //            return CastUtility.CastValueType<int, T>(Exp);
        //        case "Desc":
        //            return (T)(object)Desc;
        //    }
        //    throw new MissingPublicPropertyException(propertyName);
        //}
    }

    public class TestGlobalCharDBModel : ReactiveModel
    {
        public ReactiveArray<TestCharacterModel> Characters { get; set; }
    }

    public class TestComplexPageModel : ReactiveModel
    {
        public TabType CurrentTab { get; set; }

        public TestCharacterModel CurrentCharacter { get; set; }
    }

    [RequireModel(typeof(TestComplexPageModel))]
    public class TestComplexPage : UIPage
    {
        protected override void OnWillOpen()
        {
            SwitchTab(true, (int)TabType.Properties, false);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Debug.Log($"Drag: {eventData.delta.x}");
            //Transform character = GameObject.Find("ying_with_physics").transform;
            //character.Rotate(new Vector3(0, eventData.delta.x * -0.5f, 0));
        }

        public void SwitchTab(bool value, [EditAsEnum(typeof(TabType))] int tabId, bool applyToModel)
        {
            if (!value)
            {
                return;
            }

            TabType tab = (TabType)tabId;

            if (applyToModel)
            {
                GetModelAs<TestComplexPageModel>().CurrentTab = tab;
                Debug.Log(tab);
            }

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

        public void SwitchChar(PointerEventData eventData, TestCharacterModel charModel)
        {
            GetModelAs<TestComplexPageModel>().CurrentCharacter = charModel;
        }

        public bool ShowTab([EditAsEnum(typeof(TabType))] int expectedTab)
        {
            var model = GetModelAs<TestComplexPageModel>();
            return (int)model.CurrentTab == expectedTab;
        }

        //public override void InvokeMethod<T>(string name, Transform initiator, T arg, VentiCola.UI.Bindings.DynamicArgument[] resetArgs)
        //{
        //    switch (name)
        //    {
        //        case nameof(OnDrag): OnDrag((PointerEventData)(object)arg); break;
        //        case nameof(SwitchTab):
        //            SwitchTab(
        //                VentiCola.UI.Internal.CastUtility.CastAny<T, bool>(arg),
        //                resetArgs[0].Resolve<int>(initiator),
        //                resetArgs[1].Resolve<bool>(initiator)
        //            );
        //            break;
        //        case nameof(SwitchChar):
        //            SwitchChar((PointerEventData)(object)arg, resetArgs[0].Resolve<TestCharacterModel>(initiator));
        //            break;
        //        default:
        //            base.InvokeMethod<T>(name, initiator, arg, resetArgs); break; // throw new System.MissingMethodException(nameof(TestComplexPage), name);
        //    }
        //}

        public string A()
        {
            return "1";
        }

        //public override T InvokeMethod<T>(string name, Transform initiator, VentiCola.UI.Bindings.DynamicArgument[] args)
        //{
        //    return name switch
        //    {
        //        nameof(ShowTab) => VentiCola.UI.Internal.CastUtility.CastAny<bool, T>(ShowTab(args[0].Resolve<int>(initiator))),
        //        "A" => (T)(object)A(),
        //        _ => base.InvokeMethod<T>(name, initiator, args), // throw new System.MissingMethodException(nameof(TestComplexPage), name);
        //    };
        //}
    }
}