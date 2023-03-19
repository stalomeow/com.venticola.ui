using VentiCola.UI;

namespace VentiColaTests.UI
{
    /// <summary>
    /// test
    /// </summary>
    public class TestModel : ReactiveModel
    {
        //public static int InvalidProp0 { get; set; }

        //private int InvalidProp1 { get; set; }

        //public int InvalidProp2 { get; }

        //public int InvalidProp3 { get; private set; }

        //public int InvalidProp4 { internal get; set; }

        //public int InvalidProp5 { set { } }


        public int Health { get; set; }

        public float HpFillAmount
        {
            get => Health / 100f;
        }

        public ReactiveMap<string, TestItemModel> ItemMap { get; set; }

        public ReactiveArray<TestItemModel> Items { get; set; }
    }
}