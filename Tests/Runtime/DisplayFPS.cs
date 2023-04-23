using System;
using UnityEngine;

namespace VentiColaTests.UI
{
    [DisallowMultipleComponent]
    public class DisplayFPS : MonoBehaviour
    {
        public enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
        }

        public Corner DisplayCorner = Corner.TopLeft;
        public float RectWidth = 400;
        public float RectHeight = 200;
        public int FontSize = 12;

        [NonSerialized] private GUIStyle m_Style;
        [NonSerialized] private float m_StartTime;
        [NonSerialized] private float m_FrameCount;
        [NonSerialized] private string m_FPSText;

        private void Update()
        {
            float time = Time.realtimeSinceStartup;

            if (time - m_StartTime > 1)
            {
                m_FPSText = $"FPS {m_FrameCount}";
                m_StartTime = time;
                m_FrameCount = 0;
            }
            else
            {
                m_FrameCount++;
            }
        }

        private void OnGUI()
        {
            m_Style ??= new GUIStyle(GUI.skin.label);
            m_Style.fontSize = FontSize;

            Rect safeRect = Screen.safeArea;
            Rect rect = DisplayCorner switch
            {
                Corner.TopLeft => new Rect(safeRect.x, safeRect.y, RectWidth, RectHeight),
                Corner.TopRight => new Rect(safeRect.xMax - RectWidth, safeRect.y, RectWidth, RectHeight),
                Corner.BottomLeft => new Rect(safeRect.x, safeRect.yMax - RectHeight, RectWidth, RectHeight),
                Corner.BottomRight => new Rect(safeRect.xMax - RectWidth, safeRect.yMax - RectHeight, RectWidth, RectHeight),
                _ => throw new NotImplementedException(),
            };

            GUI.Label(rect, m_FPSText ?? string.Empty, m_Style);
        }
    }
}