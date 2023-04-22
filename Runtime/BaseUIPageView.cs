using System;
using UnityEngine;

namespace VentiCola.UI
{
    [DisallowMultipleComponent]
    public abstract class BaseUIPageView : MonoBehaviour
    {
        [NonSerialized] private IViewController m_Controller;
        // [NonSerialized] private Animator m_Animator;
        [NonSerialized] private Canvas m_Canvas;

        protected BaseUIPageView() { }

        // public Animator Animator => m_Animator;

        public Canvas Canvas => m_Canvas;

        internal void BindController(IViewController controller)
        {
            m_Controller = controller;
            // m_Animator = GetComponent<Animator>();
            m_Canvas = GetComponent<Canvas>();
        }

        internal void UpdateCanvas(int? sortingOrder)
        {
            if (sortingOrder.HasValue)
            {
                m_Canvas.overrideSorting = true;
                m_Canvas.sortingOrder = sortingOrder.Value;
            }
            else
            {
                m_Canvas.overrideSorting = false;
            }
        }

        internal void UnbindController()
        {
            m_Controller = null;
            // m_Animator = null;
        }

        protected virtual void Update()
        {
            m_Controller?.Update();
        }

        protected virtual void LateUpdate()
        {
            m_Controller?.LateUpdate();
        }

        //private void CheckAnimator(out bool isRunning)
        //{
        //    var animator = GetViewInternal().Animator;

        //    if (animator != null && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
        //    {
        //        isRunning = true;
        //        m_OnChangedCallback(this);
        //    }
        //    else
        //    {
        //        isRunning = false;
        //    }
        //}
    }
}