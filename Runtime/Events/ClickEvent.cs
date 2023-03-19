using UnityEngine;
using UnityEngine.EventSystems;
using VentiCola.UI.Bindings;

namespace VentiCola.UI.Events
{
    [AddComponentMenu("UI Events/Click")]
    public class ClickEvent : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private VoidMethodProxy<PointerEventData> m_Performing;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            m_Performing.Invoke(eventData);
        }
    }
}