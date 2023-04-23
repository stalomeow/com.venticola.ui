using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI;
using VentiCola.UI.Bindings.Experimental;

namespace VentiColaTests.UI
{
    public class Test : MonoBehaviour
    {
        [SerializeField] private Button CharButton;
        [SerializeField] private Button AlertButton;

        private void Start()
        {
            Application.targetFrameRate = 120;

            var yingTalents = new ReactiveList<TestTalentModel>()
            {
                new()
                {
                    Name = "普通攻击·异邦草翦",
                    Level = 10,
                    IconPath = "ying_talent_0"
                },
                new()
                {
                    Name = "草缘剑",
                    Level = 10,
                    IconPath = "ying_talent_1"
                },
                new()
                {
                    Name = "偃草若化",
                    Level = 10,
                    IconPath = "ying_talent_2"
                },
                new()
                {
                    Name = "蔓生的埜草",
                    Level = 1,
                    IconPath = "ying_talent_3"
                },
                new()
                {
                    Name = "繁庑的丛草",
                    Level = 1,
                    IconPath = "ying_talent_4"
                }
            };
            TestCharDB.Characters = new ReactiveList<TestCharacterModel>
            {
                new()
                {
                    ElementType = "水",
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
                },
                new()
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
                    LoveExp = 100,
                    LoveMaxExp = 1000,
                    Desc = "？？？？？",
                    AvatarPath = "unknown",
                    Talents = new ReactiveList<TestTalentModel>()
                }
            };

            CharButton.onClick.AddListener(() => ShowCharPanel());
            AlertButton.onClick.AddListener(() => ShowAlert());

            Singleton<ResourcesUIManager>.Instance.PrepareEnvironment();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                ShowAlert();
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                ShowAlert();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Singleton<ResourcesUIManager>.Instance.CloseTop();
            }
        }

        private void ShowCharPanel()
        {
            var controller = Singleton<TestComplexPageController>.Instance;
            controller.CurrentTab = TabType.Talent;
            controller.CurrentChar = TestCharDB.Characters[0];
            Singleton<ResourcesUIManager>.Instance.Show(controller);
        }

        private void ShowAlert()
        {
            Singleton<ResourcesUIManager>.Instance.Show(new TestAlertBoxPageController
            {
                Title = "The Hippocratic Oath",
                Message = "　　I swear by Apollo the physician, by Aesculapius, Hygeia, and Panacea, and I take to witness all the gods, all the goddesses, to keep according to my ability and my judgement the following oath.\n　　To consider dear to me as my parents him who taught me this art, to live in common with him, and if necessary to share my goods with him, to look upon him children as my own brothers, to teach them this art if they so desire without fee or written promise, to impart to my sons and the sons of the master who taught me and the disciples who have enrolled themselves and have agreed to the rules of the profession, but to these alone, the precepts and the instruction.\n　　I will prescribe regimen for the good of my patients according to my ability and my judgement and never do harm to anyone. To please no one will I prescribe a deadly drug, nor give advice which may cause his death. Nor will I give a woman a pessary to procure abortion. But I will preserve the purity of my life and my art. I will not cut for stone, even for patients in whom the disease is manifest. I will leave this operation to be performed by practitioners （specialist in this art）. In every house where I come I will enter only for the good of my patients, keeping myself far from all intentional ill-doing and all seduction. All that may come to my knowledge in the exercise of my profession or in daily commerce with men, which ought not to be spread abroad, I will keep secret and will never reveal it.\n　　If I keep this oath faithfully, may I enjoy my life and practice my art, respected by all men and in all times, but if I swerve from it or violate it, may the reverse be my lot.",
                OnConfirm = () => print("OK"),
                OnCancel = () => print("???"),
            });
        }
    }
}