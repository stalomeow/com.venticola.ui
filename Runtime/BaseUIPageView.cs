using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VentiCola.UI
{
    [DisallowMultipleComponent]
    public abstract class BaseUIPageView : MonoBehaviour
    {
        [NonSerialized] private BaseUIPageController m_Controller;
        [NonSerialized] private BaseRaycaster m_Raycaster;

        protected BaseUIPageView() { }

        public BaseRaycaster Raycaster => m_Raycaster;

        internal void BindController(BaseUIPageController controller)
        {
            m_Controller = controller;
            m_Raycaster = GetComponent<BaseRaycaster>();
        }

        internal void CleanUpView()
        {
            m_Controller = null;
            m_Raycaster = null;
        }

        protected virtual void Update()
        {
            m_Controller.Update();
        }

        protected virtual void LateUpdate()
        {
            m_Controller.LateUpdate();
        }
    }
}