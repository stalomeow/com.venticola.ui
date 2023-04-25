using UnityEngine;
using VentiCola.UI;
using VentiCola.UI.Specialized;

public class HelloWorld : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Singleton<ResourcesUIManager>.Instance.Show(new HelloWorldPageController()
            {
                DisplayString = "Hello World!"
            });
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Singleton<ResourcesUIManager>.Instance.CloseTop();
        }
    }
}