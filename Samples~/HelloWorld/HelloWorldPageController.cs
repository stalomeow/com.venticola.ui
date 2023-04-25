using VentiCola.UI;
using VentiCola.UI.Specialized;

public class HelloWorldPageController : BaseUIPageController<HelloWorldPage>
{
    public string DisplayString { get; set; }

    public HelloWorldPageController()
    {
        Config.PrefabKey = "HelloWorldPage";
    }

    protected override void OnViewAppear()
    {
        View.Text.text = DisplayString;
        View.CloseButton.onClick.AddListener(() => Singleton<ResourcesUIManager>.Instance.Close(this));
    }

    protected override void OnViewDisappear()
    {
        View.CloseButton.onClick.RemoveAllListeners();
    }
}