using UnityEngine;
using VentiCola.UI;

namespace VentiColaTests.UI
{
    [RequireModel(typeof(TestModel))]
    public class TestPage : UIPage
    {
        protected override void OnWillAppear()
        {
            VentiCola.UI.Effects.UIBackgroundBlurFeature.Instance.UIChanged = true;
        }

        public void OnButtonClick()
        {
            var model = GetModelAs<TestModel>();

            if (model.Health > 0)
            {
                model.Health -= Mathf.Min(model.Health, 20);
            }
            else
            {
                RequestClose();
            }
        }
    }
}