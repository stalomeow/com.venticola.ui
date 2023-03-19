using UnityEngine;
using VentiCola.UI.Internals;

namespace VentiCola.UI.Bindings
{
    public interface IBinding : IChangeObserver
    {
        /// <summary>
        /// 当前 Binding 是否需要更新
        /// </summary>
        bool IsSelfDirty { get; set; }

        /// <summary>
        /// 是否是第一次渲染
        /// </summary>
        bool IsFirstRendering { get; set; }

        /// <summary>
        /// 上一级 Binding。对于顶层的 Binding，该值为 null
        /// </summary>
        ILayoutBinding ParentBinding { get; }

        /// <summary>
        /// 设置 <see cref="IChangeObserver.IsPassive"/> 的值
        /// </summary>
        /// <param name="value"></param>
        void SetIsPassive(bool value);

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="parent">上一级 Binding。如果当前 Binding 处于顶层，则该值为 null</param>
        /// <param name="templateObject">模板对象。如果当前 Binding 不是 <see cref="ILayoutBinding"/>，则该值为 null</param>
        void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null);

        /// <summary>
        /// 计算各种数据的值
        /// </summary>
        /// <param name="changed">较上一次的值是否发生了变化</param>
        void CalculateValues(out bool changed);

        /// <summary>
        /// 渲染页面
        /// </summary>
        void RenderSelf();

        /// <summary>
        /// 计算从第一次开始所有的 <see cref="CalculateValues"/> 和 <see cref="RenderSelf"/> 方法调用是否覆盖了所有的分支代码
        /// </summary>
        /// <remarks>分支代码：包含了响应式属性调用的条件分支</remarks>
        /// <returns>如果所有的分支已经被覆盖，返回 true；否则返回 false</returns>
        bool HasCoveredAllBranchesSinceFirstRendering();

        /// <summary>
        /// 获取 Binding 所在的游戏物体
        /// </summary>
        /// <returns></returns>
        GameObject GetOwnerGameObject();
    }
}
