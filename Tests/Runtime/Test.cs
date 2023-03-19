using UnityEngine;
using VentiCola.UI;
using VentiCola.UI.Internals;

namespace VentiColaTests.UI
{
    public class Test : MonoBehaviour
    {
        public class BoxedInt : IReusableObject
        {
            public int Value;

            public override string ToString() => Value.ToString();

            public int Version { get; set; }

            void IReusableObject.ResetObject() { }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                var chars = UIManager.GetGlobalModel<TestGlobalCharDBModel>("Character DB");

                UIManager.OpenPageAsync("Test Complex UI", new TestComplexPageModel
                {
                    CurrentTab = TabType.Properties,
                    CurrentCharacter = chars.Characters[0]
                });
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                UIManager.OpenPageAsync("Test UI", new TestModel
                {
                    Health = Random.Range(10, 101)
                });
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                UIManager.CloseAllPages();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIManager.CloseTopPage();
            }
        }

        private void Start()
        {
            ReactiveArray<TestCharacterModel> chars = new ReactiveArray<TestCharacterModel>(2);
            ReactiveArray<TestTalentModel> yingTalents = new ReactiveArray<TestTalentModel>(5);

            yingTalents[0] = new TestTalentModel
            {
                Name = "普通攻击·异邦草翦",
                Level = 10,
                IconPath = "ying_talent_0"
            };
            yingTalents[1] = new TestTalentModel
            {
                Name = "草缘剑",
                Level = 10,
                IconPath = "ying_talent_1"
            };
            yingTalents[2] = new TestTalentModel
            {
                Name = "偃草若化",
                Level = 10,
                IconPath = "ying_talent_2"
            };
            yingTalents[3] = new TestTalentModel
            {
                Name = "蔓生的埜草",
                Level = 1,
                IconPath = "ying_talent_3"
            };
            yingTalents[4] = new TestTalentModel
            {
                Name = "繁庑的丛草",
                Level = 1,
                IconPath = "ying_talent_4"
            };

            chars[0] = new TestCharacterModel
            {
                ElementType = "草",
                Name = "荧",
                Level = 88,
                MaxLevel = 90,
                Exp = 6000,
                MaxExp = 25000,
                MaxHp = 25025,
                ATK = 1860,
                DEF = 250,
                LoveLevel = 10,
                LoveExp = 960,
                LoveMaxExp = 1000,
                Desc = "从世界之外漂流而来的旅行者，被神带走血亲，自此踏上寻找七神之路。",
                AvatarPath = "ying",
                Talents = yingTalents
            };
            chars[1] = new TestCharacterModel
            {
                ElementType = "？",
                Name = "？？？",
                Level = 85,
                MaxLevel = 90,
                Exp = 9000,
                MaxExp = 20000,
                MaxHp = 14026,
                ATK = 2125,
                DEF = 300,
                LoveLevel = 10,
                LoveExp = 900,
                LoveMaxExp = 1000,
                Desc = "？？？？？",
                AvatarPath = "unknown",
                Talents = new ReactiveArray<TestTalentModel>(0)
            };

            UIManager.SetGlobalModel("Character DB", new TestGlobalCharDBModel { Characters = chars });

            //var hashSet = new UnityWeakHashSet<BoxedInt>();

            //// 随便几个数字
            //BoxedInt[] ints = new[]
            //{
            //    new BoxedInt { Value = 0 },
            //    new BoxedInt { Value = 1 },
            //    new BoxedInt { Value = 2 },
            //    new BoxedInt { Value = 3 },
            //};

            //// 初始化 UnityWeakHashSet
            //Array.ForEach(ints, item => hashSet.Add(item));

            //// 重复添加
            //hashSet.Add(ints[0]);

            //// 移除
            //hashSet.Remove(ints[1]);

            //// 改变元素的版本，该元素也会被当作失效
            //ints[2].Version = 114;

            //Assert.AreEqual("0, 3", string.Join(", ", hashSet.OrderBy(i => i.Value)));

            //// 强制保留 ints[3] 对象
            //var strongRef = ints[3];

            //// 清除其他强引用
            //for (int i = 0; i < ints.Length; i++)
            //    ints[i] = null;

            //// 强制一次 GC
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            //Assert.AreEqual("3", string.Join(", ", hashSet.OrderBy(i => i.Value)));

            //// 清空
            //hashSet.Clear();

            //Assert.AreEqual("", string.Join(", ", hashSet.OrderBy(i => i.Value)));

            //// 再随便加几个
            //hashSet.Add(new BoxedInt { Value = 666 });
            //hashSet.Add(new BoxedInt { Value = 666 });
            //hashSet.Add(new BoxedInt { Value = 999 });

            //Assert.AreEqual("666, 666, 999", string.Join(", ", hashSet.OrderBy(i => i.Value)));

            //GC.KeepAlive(strongRef);
        }
    }
}