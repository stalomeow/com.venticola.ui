using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VentiCola.UI.Bindings.LowLevel;
using VentiCola.UI.Internal;
using VentiCola.UI.Rendering;
using Object = UnityEngine.Object;

namespace VentiCola.UI
{
    public abstract class BaseUIManager
    {
        protected class PrefabEntry
        {
            public GameObject Prefab;
            public int RefCount;
            public List<IViewController> WaitingList;
        }

        protected readonly Action<string, GameObject> m_LoadPrefabCallback;
        protected readonly Action<IViewController> m_ChangedCallback;
        protected readonly Action<IViewController> m_ClosedCallback;

        protected readonly List<IViewController> m_ViewStack;
        protected readonly List<int> m_BlockerStack;
        protected readonly Dictionary<string, PrefabEntry> m_PrefabCache;
        protected readonly Dictionary<string, GameObject> m_PersistentCache;
        protected readonly LRUMultiHashMap<string, GameObject> m_LRUCache;

        protected readonly CameraOverrideSettings m_MainCameraFullSettings;
        protected bool m_IsMainCameraRenderingStopped;

        protected Transform m_UIRoot;
        protected Transform m_UIPoolRoot;
        protected Camera m_MainCamera;
        protected UniversalAdditionalCameraData m_MainCameraUrpData;
        protected Camera m_UICamera;

        protected BaseUIManager()
        {
            m_LoadPrefabCallback = OnLoadPrefabCompleted;
            m_ChangedCallback = OnViewChanged;
            m_ClosedCallback = OnViewClosed;

            m_ViewStack = new List<IViewController>();
            m_BlockerStack = new List<int>();
            m_PrefabCache = new Dictionary<string, PrefabEntry>();
            m_PersistentCache = new Dictionary<string, GameObject>();
            m_LRUCache = new LRUMultiHashMap<string, GameObject>(UIRuntimeSettings.Instance.LRUCacheSize);
            m_LRUCache.OnEliminated += OnLRUViewEliminated;

            m_MainCameraFullSettings = new CameraOverrideSettings();
            m_IsMainCameraRenderingStopped = false;

            InitUIPoolRoot();
            InitUIRootAndCamera();
            InitBuiltinAnimatableTypes();

            AdvancedUIRenderer.OnDidRender += (int frameCountWithoutBlur) =>
            {
                if (m_IsMainCameraRenderingStopped || AdvancedUIRenderer.UIChanged || frameCountWithoutBlur < 10)
                {
                    return;
                }

                if (TryGetTopBlockerIfActive(out _, out _))
                {
                    SwitchMainCameraSettings(false);
                }
            };

            SceneManager.activeSceneChanged += (Scene prev, Scene curr) =>
            {
                Camera mainCamera = Camera.main;

                if (mainCamera != m_MainCamera)
                {
                    MainCamera = mainCamera;
                }
            };
        }

        /// <summary>
        /// 主摄像机
        /// </summary>
        public Camera MainCamera
        {
            get => m_MainCamera;
            set
            {
                if (m_MainCameraUrpData != null)
                {
                    // 还原之前的 Main Camera
                    if (m_IsMainCameraRenderingStopped)
                    {
                        SwitchMainCameraSettings(true);
                    }

                    m_MainCameraUrpData.cameraStack.Remove(m_UICamera);
                }

                m_MainCamera = value;
                m_MainCameraUrpData = m_MainCamera.GetUniversalAdditionalCameraData();
                m_IsMainCameraRenderingStopped = false;

                List<Camera> cameraStack = m_MainCameraUrpData.cameraStack;

                if (!cameraStack.Contains(m_UICamera))
                {
                    cameraStack.Add(m_UICamera);
                }
            }
        }

        /// <summary>
        /// UI 摄像机
        /// </summary>
        public Camera UICamera
        {
            get => m_UICamera;
        }

        protected virtual void OnLRUViewEliminated(string prefabKey, GameObject viewInstance)
        {
            DestroyViewInstance(prefabKey, viewInstance);
            Debug.LogWarning($"LRU eliminate view instance '{prefabKey}'");
        }

        protected GameObject InstantiateView(PrefabEntry prefabEntry)
        {
            prefabEntry.RefCount++;
            return Object.Instantiate(prefabEntry.Prefab, Vector3.zero, Quaternion.identity);
        }

        protected void DestroyViewInstance(string prefabKey, GameObject viewInstance)
        {
            Object.Destroy(viewInstance);

            PrefabEntry entry = m_PrefabCache[prefabKey];
            entry.RefCount--;

            if (entry.RefCount <= 0)
            {
                m_PrefabCache.Remove(prefabKey);
                ReleasePrefab(prefabKey, entry.Prefab);
            }
        }

        private void InitUIPoolRoot()
        {
            var go = new GameObject("UI Pool");
            Object.DontDestroyOnLoad(go);

            m_UIPoolRoot = go.transform;
            m_UIPoolRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void InitUIRootAndCamera()
        {
            GameObject go = Object.Instantiate(UIRuntimeSettings.Instance.UIRootPrefab, Vector3.zero, Quaternion.identity);
            Object.DontDestroyOnLoad(go);

            Canvas canvas = go.GetComponentInChildren<Canvas>();

            if (!canvas.isRootCanvas)
            {
                canvas = canvas.rootCanvas;
            }

            m_UIRoot = canvas.transform;
            m_UICamera = canvas.worldCamera;
            MainCamera = Camera.main; // Set the MainCamera Property, not the m_MainCamera Field
        }

        private void InitBuiltinAnimatableTypes()
        {
            MakeTypeAnimatable<int>((a, b, t) => Mathf.RoundToInt(Mathf.LerpUnclamped(a, b, t)));
            MakeTypeAnimatable<float>(Mathf.LerpUnclamped);
            MakeTypeAnimatable<Vector2>(Vector2.LerpUnclamped);
            MakeTypeAnimatable<Vector3>(Vector3.LerpUnclamped);
            MakeTypeAnimatable<Vector4>(Vector4.LerpUnclamped);
            MakeTypeAnimatable<Color>(Color.LerpUnclamped);
            MakeTypeAnimatable<Color32>(Color32.LerpUnclamped);
            MakeTypeAnimatable<Quaternion>(Quaternion.LerpUnclamped);
        }

        /// <summary>
        /// 空方法。在某些情况下用于显式初始化 UIManager
        /// </summary>
        public void PrepareEnvironment() { }

        /// <summary>
        /// 显示指定的页面
        /// </summary>
        /// <param name="controller"></param>
        public void Show(IViewController controller)
        {
            switch (controller.State)
            {
                case UIState.Active:
                    return;

                case UIState.Paused:
                    Close(m_ViewStack[controller.StackIndex + 1]);
                    return;
            }

            if (controller.ViewInstance != null)
            {
                ShowNewView(controller);
                return;
            }

            string prefabKey = controller.Config.PrefabKey;

            if (!TryGetViewInstanceFromCache(prefabKey, controller.Config.CacheType, out GameObject viewInstance))
            {
                if (m_PrefabCache.TryGetValue(prefabKey, out PrefabEntry prefabEntry))
                {
                    if (prefabEntry.Prefab == null)
                    {
                        if (!prefabEntry.WaitingList.Contains(controller))
                        {
                            prefabEntry.WaitingList.Add(controller);
                        }
                        return;
                    }

                    viewInstance = InstantiateView(prefabEntry);
                }
                else
                {
                    List<IViewController> waitingList = ListPool<IViewController>.Get();
                    waitingList.Add(controller);

                    m_PrefabCache.Add(prefabKey, new PrefabEntry { WaitingList = waitingList });
                    LoadPrefabAsync(prefabKey, m_LoadPrefabCallback);
                    return;
                }
            }

            controller.InitView(viewInstance);
            ShowNewView(controller);
        }

        protected void ShowNewView(IViewController controller)
        {
            int stackIndex = m_ViewStack.Count;
            GameObject viewInstance = controller.ViewInstance;

            if (!controller.Config.IsAdditive)
            {
                PauseTopViewGroup();
            }

            // reparent it first, update your data, and then enable it
            SetGameObjectParent(viewInstance, m_UIRoot, true);
            SetGameObjectLayer(viewInstance, AdvancedUIRenderer.VisibleUILayer);

            m_ViewStack.Add(controller);
            controller.Open(stackIndex);
            controller.OnViewChanged += m_ChangedCallback;
            controller.OnClosingCompleted += m_ClosedCallback;

            if (!viewInstance.activeSelf)
            {
                viewInstance.SetActive(true);
            }

            if (controller.Config.RenderOption != UIRenderOption.None)
            {
                AddBlocker(stackIndex);
            }

            UpdateRendererBlurOptionAndRerender();
        }

        protected bool TryGetViewInstanceFromCache(string key, UICacheType cacheType, out GameObject viewInstance)
        {
            switch (cacheType)
            {
                case UICacheType.One:
                    return m_PersistentCache.Remove(key, out viewInstance);

                case UICacheType.LRU:
                    return m_LRUCache.TryTake(key, out viewInstance);
            }

            viewInstance = null;
            return false;
        }

        protected void PauseTopViewGroup()
        {
            for (int i = m_ViewStack.Count - 1; i >= 0; i--)
            {
                IViewController controller = m_ViewStack[i];

                if (controller.State != UIState.Paused)
                {
                    controller.Pause();
                }

                if (!controller.Config.IsAdditive)
                {
                    break;
                }
            }
        }

        protected void AddBlocker(int stackIndex)
        {
            for (int i = m_BlockerStack.PeekBackOrDefault(0); i < stackIndex; i++)
            {
                SetGameObjectLayer(m_ViewStack[i].ViewInstance, AdvancedUIRenderer.HiddenUILayer);
            }

            m_BlockerStack.Add(stackIndex);
        }

        protected void UpdateRendererBlurOptionAndRerender()
        {
            if (!TryGetTopBlockerIfActive(out _, out UIRenderOption renderOption))
            {
                AdvancedUIRenderer.BlurOpt = BlurOption.Disable;
            }
            else
            {
                AdvancedUIRenderer.BlurOpt = renderOption switch
                {
                    UIRenderOption.FullScreenBlurStatic => BlurOption.FullScreenStatic,
                    UIRenderOption.FullScreenBlurDynamic => BlurOption.FullScreenDynamic,
                    UIRenderOption.FullScreenBlurTexture => BlurOption.TextureDynamic,
                    _ => BlurOption.Disable,
                };
            }

            RequestUIRerendering();
        }

        protected void RequestUIRerendering()
        {
            AdvancedUIRenderer.UIChanged = true;
            SwitchMainCameraSettings(true);
        }

        protected void SwitchMainCameraSettings(bool full)
        {
            if (m_IsMainCameraRenderingStopped != full)
            {
                // 已经满足要求了
                return;
            }

            // TODO: Optimize this method.

            var settings = UIRuntimeSettings.Instance;

            if (full)
            {
                if (settings.EnableMainCameraOverrideSettings)
                {
                    m_MainCameraFullSettings.ApplyTo(m_MainCamera, m_MainCameraUrpData);

                    // 启用 UI 相机前面的堆叠摄像机
                    List<Camera> cameraStack = m_MainCameraUrpData.cameraStack;

                    for (int i = 0; i < cameraStack.Count; i++)
                    {
                        var camera = cameraStack[i];

                        if (camera == m_UICamera)
                        {
                            break;
                        }

                        camera.enabled = true;
                    }
                }

                if (settings.EnableMainCameraRendererSettings)
                {
                    m_MainCameraUrpData.SetRenderer(settings.MainCameraRendererSettings.FullFeature);
                }

                m_IsMainCameraRenderingStopped = false;
            }
            else
            {
                // 每次都拷贝，防止 EnableMainCameraOverrideSettings 突然变化后 Camera 设置混乱
                m_MainCameraFullSettings.CopyFrom(m_MainCamera, m_MainCameraUrpData);

                if (settings.EnableMainCameraOverrideSettings)
                {
                    settings.MainCameraOverrideSettings.ApplyTo(m_MainCamera, m_MainCameraUrpData);

                    // 禁用 UI 相机前面的堆叠摄像机
                    List<Camera> cameraStack = m_MainCameraUrpData.cameraStack;

                    for (int i = 0; i < cameraStack.Count; i++)
                    {
                        var camera = cameraStack[i];

                        if (camera == m_UICamera)
                        {
                            break;
                        }

                        camera.enabled = false;
                    }
                }

                if (settings.EnableMainCameraRendererSettings)
                {
                    m_MainCameraUrpData.SetRenderer(settings.MainCameraRendererSettings.LightWeight);
                }

                m_IsMainCameraRenderingStopped = true;
            }
        }

        protected virtual void OnLoadPrefabCompleted(string prefabKey, GameObject prefab)
        {
            PrefabEntry entry = m_PrefabCache[prefabKey];

            if (prefab == null)
            {
                m_PrefabCache.Remove(prefabKey);
            }
            else
            {
                entry.Prefab = prefab;

                for (int i = 0; i < entry.WaitingList.Count; i++)
                {
                    IViewController controller = entry.WaitingList[i];
                    controller.InitView(InstantiateView(entry));
                    ShowNewView(controller);
                }
            }

            ListPool<IViewController>.Release(entry.WaitingList);
            entry.WaitingList = null;
        }

        protected virtual void OnViewChanged(IViewController controller)
        {
            if (AdvancedUIRenderer.UIChanged)
            {
                return;
            }

            // 当 Blur 层下的可见 UI 发生变化时，重新进行一次 Blur

            int stackIndex = controller.StackIndex;
            int totalBlockerCount = 0;
            int opaqueBlockerCount = 0;

            for (int i = m_BlockerStack.Count - 1; i >= 0; i--)
            {
                int blockerStackIndex = m_BlockerStack[i];

                if (blockerStackIndex <= stackIndex)
                {
                    break;
                }

                totalBlockerCount++;

                if (m_ViewStack[blockerStackIndex].Config.RenderOption == UIRenderOption.FullScreenOpaque)
                {
                    opaqueBlockerCount++;
                    break;
                }
            }

            if (totalBlockerCount > 0 && opaqueBlockerCount == 0)
            {
                RequestUIRerendering();
            }
        }

        protected bool TryGetTopBlockerIfActive(out int stackIndex, out UIRenderOption renderOption)
        {
            int topIndex = m_BlockerStack.Count - 1;

            if (topIndex >= 0)
            {
                stackIndex = m_BlockerStack[topIndex];
                IViewController controller = m_ViewStack[stackIndex];

                if (controller.State == UIState.Active)
                {
                    renderOption = controller.Config.RenderOption;
                    return true;
                }
            }

            stackIndex = -1;
            renderOption = default;
            return false;
        }

        protected void ResumeTopViewGroup()
        {
            for (int i = m_ViewStack.Count - 1; i >= 0; i--)
            {
                var controller = m_ViewStack[i];

                if (controller.State != UIState.Active)
                {
                    controller.Resume();
                }

                if (!controller.Config.IsAdditive)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 关闭最上层的页面
        /// </summary>
        public void CloseTop()
        {
            if (m_ViewStack.Count <= 0)
            {
                return;
            }

            CloseLastNoCheck();
            UpdateRendererBlurOptionAndRerender();
        }

        /// <summary>
        /// 关闭指定的页面以及之上的所有页面
        /// </summary>
        /// <param name="controller"></param>
        public void Close(IViewController controller)
        {
            if (controller.State is (UIState.Closing or UIState.Closed))
            {
                return;
            }

            int stackIndex = controller.StackIndex;

            while (m_ViewStack.Count > stackIndex)
            {
                CloseLastNoCheck();
            }

            UpdateRendererBlurOptionAndRerender();
        }

        /// <summary>
        /// 关闭所有的页面
        /// </summary>
        public void CloseAll()
        {
            if (m_ViewStack.Count <= 0)
            {
                return;
            }

            while (m_ViewStack.Count > 0)
            {
                CloseLastNoCheck();
            }

            UpdateRendererBlurOptionAndRerender();
        }

        protected void CloseLastNoCheck()
        {
            IViewController controller = m_ViewStack.PopBackUnsafe();

            if (!controller.Config.IsAdditive)
            {
                ResumeTopViewGroup();
            }

            int lastBlockerIndex = m_BlockerStack.Count - 1;

            if (lastBlockerIndex >= 0)
            {
                int stackIndex = m_BlockerStack[lastBlockerIndex];

                if (stackIndex == controller.StackIndex)
                {
                    m_BlockerStack.RemoveAt(lastBlockerIndex);

                    for (int i = m_BlockerStack.PeekBackOrDefault(0); i < stackIndex; i++)
                    {
                        SetGameObjectLayer(m_ViewStack[i].ViewInstance, AdvancedUIRenderer.VisibleUILayer);
                    }
                }
            }

            controller.Close();
        }

        protected virtual void OnViewClosed(IViewController controller)
        {
            string prefabKey = controller.Config.PrefabKey;
            GameObject viewInstance = controller.ViewInstance;

            switch (controller.Config.CacheType)
            {
                case UICacheType.One:
                    {
                        if (m_PersistentCache.ContainsKey(prefabKey))
                        {
                            goto case UICacheType.Never;
                        }

                        m_PersistentCache.Add(prefabKey, viewInstance);
                        break;
                    }

                case UICacheType.LRU:
                    {
                        m_LRUCache.Add(prefabKey, viewInstance);
                        break;
                    }

                case UICacheType.Never:
                    {
                        DestroyViewInstance(prefabKey, viewInstance);
                        return;
                    }
            }

            // Disable the object first, then reparent it into the pool.
            viewInstance.SetActive(false);
            SetGameObjectParent(viewInstance, m_UIPoolRoot, false);
        }

        /// <summary>
        /// 使 <typeparamref name="T"/> 类型可以被应用动画（可以被插值）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interpolateFunction"></param>
        public void MakeTypeAnimatable<T>(InterpolateFunction<T> interpolateFunction)
        {
            InterpolationCache<T>.InterpolateFunc = interpolateFunction;
        }

        protected static void SetGameObjectParent(GameObject go, Transform parent, bool setAsLastSibling)
        {
            Transform transform = go.transform;
            transform.SetParent(parent, false);

            if (setAsLastSibling)
            {
                transform.SetAsLastSibling();
            }
        }

        protected static void SetGameObjectLayer(GameObject go, int layer)
        {
            List<Transform> stack = ListPool<Transform>.Get();
            stack.Add(go.transform);

            while (stack.Count > 0)
            {
                Transform top = stack.PopBackUnsafe();
                GameObject topGO = top.gameObject;

                if (topGO.layer == layer)
                {
                    break;
                }

                topGO.layer = layer;

                int childCount = top.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    stack.Add(top.GetChild(i));
                }
            }

            ListPool<Transform>.Release(stack);
        }

        /// <summary>
        /// 对接资源管理系统，用于加载 UI 预制体
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback">在加载完成后调用，传入的参数分别为 <paramref name="key"/> 和加载出的 prefab。如果 prefab 为 null 则视为加载失败</param>
        protected abstract void LoadPrefabAsync(string key, Action<string, GameObject> callback);

        /// <summary>
        /// 对接资源管理系统，用于释放 UI 预制体
        /// </summary>
        /// <param name="key"></param>
        /// <param name="prefab"></param>
        protected abstract void ReleasePrefab(string key, GameObject prefab);
    }
}