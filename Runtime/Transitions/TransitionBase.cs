using UnityEngine;

namespace VentiCola.UI.Transitions
{
    public abstract class TransitionBase : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private string m_Label = "Transition";
#endif

        [SerializeField, HideInInspector] private bool m_EnableProperty = true;

        protected TransitionBase() { }

        public abstract bool IsFinished { get; }

        /// <summary>
        /// 开始过渡动画
        /// </summary>
        /// <param name="type">过渡动画的类型</param>
        /// <param name="forceRestart">是否强制重新开始。如果该值为 <c>false</c>，会从上一次的值开始进行过渡；否则会从相应状态的初始值开始进行过渡</param>
        /// <param name="instant">是否立刻完成</param>
        public abstract void BeginTransition(TransitionType type, bool forceRestart, bool instant);

        public abstract void UpdateTransition();
    }
}