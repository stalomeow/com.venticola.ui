using System;
using UnityEngine;

namespace VentiCola.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseUIPageView : MonoBehaviour
    {
        [NonSerialized] private IViewController m_Controller;
        [NonSerialized] private Canvas m_Canvas;
        [NonSerialized] private CanvasGroup m_CanvasGroup;

        protected BaseUIPageView() { }

#pragma warning disable IDE1006 // Naming Styles

        public Canvas canvas => m_Canvas;

        public CanvasGroup canvasGroup => m_CanvasGroup;

#pragma warning restore IDE1006 // Naming Styles

        protected virtual void Awake()
        {
            m_Canvas = GetComponent<Canvas>();
            m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        protected virtual void Update()
        {
            m_Controller?.Update();
        }

        protected virtual void LateUpdate()
        {
            m_Controller?.LateUpdate();
        }

        internal void BindController(IViewController controller)
        {
            m_Controller = controller;
        }

        internal void UpdateCanvas(int? sortingOrder)
        {
            // 注：只有 nested canvas 才能设置 overrideSorting
            // 如果不是 nested，赋值会被忽略

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

        internal void UpdateCanvasGroup(bool blocksRaycasts)
        {
            m_CanvasGroup.blocksRaycasts = blocksRaycasts;
        }

        internal void UnbindController()
        {
            m_Controller = null;
        }
    }
}