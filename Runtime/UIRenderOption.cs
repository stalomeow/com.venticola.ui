namespace VentiCola.UI
{
    /// <summary>
    /// UI 的渲染设置
    /// </summary>
    public enum UIRenderOption
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 在 UI 打开后，禁用 Main Camera 以及下层所有 UI 的渲染。通常，当一个页面覆盖了全屏并且不透明时可以选择它
        /// </summary>
        FullScreenOpaque = 1,

        /// <summary>
        /// 在 UI 刚显示或下层 UI 变化时进行全屏背景模糊（包括下层 UI），立即应用于屏幕，然后缓存复用，同时禁用 Main Camera 以及下层所有 UI 的渲染
        /// </summary>
        FullScreenBlurStatic = 2,

        /// <summary>
        /// 每帧都进行全屏背景模糊（包括下层 UI），立即应用于屏幕
        /// </summary>
        FullScreenBlurDynamic = 3,

        /// <summary>
        /// 每帧都进行全屏背景模糊（包括下层 UI），但不会将效果直接应用于屏幕，而是保存在名为 <c>_UIBlurTexture</c> 的贴图中。可以在 Shader 中读取该贴图
        /// </summary>
        /// <seealso cref="VentiCola.UI.Misc.BlurBGImage"/>
        /// <seealso cref="VentiCola.UI.Misc.BlurBGRawImage"/>
        FullScreenBlurTexture = 4,
    }
}