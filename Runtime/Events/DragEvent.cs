using UnityEngine;
using UnityEngine.EventSystems;
using VentiCola.UI.Bindings;

namespace VentiCola.UI.Events
{
    [AddComponentMenu("UI Events/Drag")]
    public class DragEvent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private VoidMethodProxy<PointerEventData> m_Begin;
        [SerializeField] private VoidMethodProxy<PointerEventData> m_Performing;
        [SerializeField] private VoidMethodProxy<PointerEventData> m_End;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            m_Begin.Invoke(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            m_Performing.Invoke(eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            m_End.Invoke(eventData);
        }
    }
}