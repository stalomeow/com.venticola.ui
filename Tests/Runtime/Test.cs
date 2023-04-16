using UnityEngine;
using VentiCola.UI;

namespace VentiColaTests.UI
{
    public class Test : MonoBehaviour
    {
        //public Vector4 TestLocalPos1;
        //public Vector4 TestLocalPos2;

        //private void Update()
        //{
        //    NormalRendering("Test Obj 1", TestLocalPos1);
        //    NormalRendering("Test Obj 2", TestLocalPos2);

        //    CameraRelativeRendering("Test Obj 1", TestLocalPos1);
        //    CameraRelativeRendering("Test Obj 2", TestLocalPos2);
        //}

        //private void NormalRendering(string objName, Vector4 localPos)
        //{
        //    Camera camera = Camera.main;
        //    Matrix4x4 vp = camera.projectionMatrix * camera.worldToCameraMatrix;
        //    Vector4 clipPos = vp * (transform.localToWorldMatrix * localPos);
        //    Vector3 ndcPos = new Vector3(clipPos.x, clipPos.y, clipPos.z) / clipPos.w;
        //    print($"{objName}: {ndcPos}");
        //}

        //private void CameraRelativeRendering(string objName, Vector4 localPos)
        //{
        //    Camera camera = Camera.main;
        //    Vector3 cameraPos = camera.transform.position;

        //    Matrix4x4 modelMatrix = transform.localToWorldMatrix;
        //    modelMatrix.m03 = (float)((double)modelMatrix.m03 - cameraPos.x);
        //    modelMatrix.m13 = (float)((double)modelMatrix.m13 - cameraPos.y);
        //    modelMatrix.m23 = (float)((double)modelMatrix.m23 - cameraPos.z);

        //    Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        //    viewMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

        //    Matrix4x4 mvp = camera.projectionMatrix * viewMatrix * modelMatrix;
        //    Vector4 clipPos = mvp * localPos;
        //    Vector3 ndcPos = new Vector3(clipPos.x, clipPos.y, clipPos.z) / clipPos.w;
        //    print($"* {objName}: {ndcPos}");
        //}

        public static ResourcesUIManager UIManager;

        private void Start()
        {
            UIManager = new ResourcesUIManager();

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
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                var controller = UIManager.Allocate<TestComplexPageController>();
                controller.CurrentTab = TabType.Talent;
                controller.CurrentChar = TestCharDB.Characters[0];
                UIManager.Open(controller);
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                var controller = UIManager.Allocate<TestAlertBoxPageController>();
                controller.Title = "The Hippocratic Oath";
                controller.Message = "　　I swear by Apollo the physician, by Aesculapius, Hygeia, and Panacea, and I take to witness all the gods, all the goddesses, to keep according to my ability and my judgement the following oath.\n　　To consider dear to me as my parents him who taught me this art, to live in common with him, and if necessary to share my goods with him, to look upon him children as my own brothers, to teach them this art if they so desire without fee or written promise, to impart to my sons and the sons of the master who taught me and the disciples who have enrolled themselves and have agreed to the rules of the profession, but to these alone, the precepts and the instruction.\n　　I will prescribe regimen for the good of my patients according to my ability and my judgement and never do harm to anyone. To please no one will I prescribe a deadly drug, nor give advice which may cause his death. Nor will I give a woman a pessary to procure abortion. But I will preserve the purity of my life and my art. I will not cut for stone, even for patients in whom the disease is manifest. I will leave this operation to be performed by practitioners （specialist in this art）. In every house where I come I will enter only for the good of my patients, keeping myself far from all intentional ill-doing and all seduction. All that may come to my knowledge in the exercise of my profession or in daily commerce with men, which ought not to be spread abroad, I will keep secret and will never reveal it.\n　　If I keep this oath faithfully, may I enjoy my life and practice my art, respected by all men and in all times, but if I swerve from it or violate it, may the reverse be my lot.";
                controller.OnConfirm = () => print("OK");
                controller.OnCancel = () => print("???");
                UIManager.Open(controller, true);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIManager.CloseTop();
            }
        }
    }
}