using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using VentiCola.UI.Bindings.LowLevel;
using VentiCola.UI.Internal;
using VentiCola.UI.Rendering;
using Object = UnityEngine.Object;

namespace VentiCola.UI
{
    public abstract class BaseUIManager
    {
        protected enum BlockerType
        {
            StaticBlur,
            DynmaicBlur,
            TextureBlur,
            Opaque
        }

        protected struct BlockerInfo
        {
            public int StackIndex;
            public BlockerType Type;
        }

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
        protected readonly List<BlockerInfo> m_BlockerStack;
        protected readonly Dictionary<string, PrefabEntry> m_PrefabCache;
        protected readonly Dictionary<string, GameObject> m_PersistentCache;
        protected readonly LRUMultiHashMap<string, GameObject> m_LRUCache;

        private readonly CameraOverrideSettings m_MainCameraFullSettings;
        private bool m_IsMainCameraRenderingStopped;

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
            m_BlockerStack = new List<BlockerInfo>();
            m_PrefabCache = new Dictionary<string, PrefabEntry>();
            m_PersistentCache = new Dictionary<string, GameObject>();
            m_LRUCache = new LRUMultiHashMap<string, GameObject>(UIRuntimeSettings.Instance.LRUCacheSize);
            m_LRUCache.OnEliminated += OnLRUPageEliminated;

            m_MainCameraFullSettings = new CameraOverrideSettings();
            m_IsMainCameraRenderingStopped = false;

            InitUIPoolRoot();
            InitUIRootAndCamera();
            InitBuiltinAnimatableTypes();

            AdvancedUIRenderer.OnDidRender += frameCountWithoutBlur =>
            {
                if (m_IsMainCameraRenderingStopped || AdvancedUIRenderer.UIChanged)
                {
                    return;
                }

                if (TryGetTopBlockerIfActive(out var blocker) && frameCountWithoutBlur > 10)
                {
                    if (blocker.Type != BlockerType.TextureBlur)
                    {
                        SwitchMainCameraSettings(false);
                    }
                }
            };
        }

        public Camera MainCamera
        {
            get => m_MainCamera;
            set
            {
                if (m_MainCameraUrpData != null)
                {
                    m_MainCameraUrpData.cameraStack.Remove(m_UICamera);
                }

                m_MainCamera = value;
                m_MainCameraUrpData = m_MainCamera.GetUniversalAdditionalCameraData();

                List<Camera> cameraStack = m_MainCameraUrpData.cameraStack;

                if (!cameraStack.Contains(m_UICamera))
                {
                    cameraStack.Add(m_UICamera);
                }
            }
        }

        protected virtual void OnLRUPageEliminated(string prefabKey, GameObject viewInstance)
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
            MainCamera = Camera.main;
        }

        private void InitBuiltinAnimatableTypes()
        {
            MakeAnimatable<float>(Mathf.LerpUnclamped);
            MakeAnimatable<Vector2>(Vector2.LerpUnclamped);
            MakeAnimatable<Vector3>(Vector3.LerpUnclamped);
            MakeAnimatable<Vector4>(Vector4.LerpUnclamped);
            MakeAnimatable<Color>(Color.LerpUnclamped);
            MakeAnimatable<Quaternion>(Quaternion.LerpUnclamped);
        }

        public void PrepareEnvironment() { } // empty method

        public void ShowSingleton<T>() where T : class, IViewController, new()
        {
            Show(Singleton<T>.Instance);
        }

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
                PauseTopPageGroup();
            }

            SetGameObjectLayer(viewInstance, AdvancedUIRenderer.VisibleLayer);
            SetGameObjectParent(viewInstance, m_UIRoot, true);

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
                AddBlocker(new BlockerInfo
                {
                    StackIndex = stackIndex,
                    Type = controller.Config.RenderOption switch
                    {
                        UIRenderOption.FullScreenStaticBlur => BlockerType.StaticBlur,
                        UIRenderOption.FullScreenDynamicBlur => BlockerType.DynmaicBlur,
                        UIRenderOption.FullScreenOpaque => BlockerType.Opaque,
                        UIRenderOption.DynamicBlurToTexture => BlockerType.TextureBlur,
                        _ => throw new NotImplementedException()
                    },
                });
            }

            UpdateRenderer();
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

        protected void PauseTopPageGroup()
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

        protected void AddBlocker(BlockerInfo blocker)
        {
            int lastBlockerIndex = m_BlockerStack.Count - 1;
            int i = lastBlockerIndex < 0 ? 0 : m_BlockerStack[lastBlockerIndex].StackIndex;

            while (i < blocker.StackIndex)
            {
                SetGameObjectLayer(m_ViewStack[i].ViewInstance, AdvancedUIRenderer.HiddenUILayer);
                i++;
            }

            m_BlockerStack.Add(blocker);
        }

        protected void UpdateRenderer()
        {
            if (!TryGetTopBlockerIfActive(out BlockerInfo blocker))
            {
                AdvancedUIRenderer.BlurOpt = BlurOption.Disable;
            }
            else
            {
                AdvancedUIRenderer.BlurOpt = blocker.Type switch
                {
                    BlockerType.StaticBlur => BlurOption.FullScreenStatic,
                    BlockerType.DynmaicBlur => BlurOption.FullScreenDynamic,
                    BlockerType.TextureBlur => BlurOption.TextureDynamic,
                    _ => BlurOption.Disable,
                };
            }

            AdvancedUIRenderer.UIChanged = true;

            SwitchMainCameraSettings(true);

            // Debug.LogFormat("Blocker Count: {0}", m_BlockerStack.Count);
        }

        protected void SwitchMainCameraSettings(bool full)
        {
            if (m_IsMainCameraRenderingStopped != full)
            {
                // 已经满足要求了
                return;
            }

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
                // TODO: 再想想，真的吗？
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
            int stackIndex = controller.StackIndex;
            int blockerCount = 0;
            int opaqueBlockerCount = 0;

            for (int i = m_BlockerStack.Count - 1; i >= 0; i--)
            {
                BlockerInfo blocker = m_BlockerStack[i];

                if (blocker.StackIndex <= stackIndex)
                {
                    break;
                }

                blockerCount++;

                if (blocker.Type == BlockerType.Opaque)
                {
                    opaqueBlockerCount++;
                    break;
                }
            }

            if (blockerCount > 0 && opaqueBlockerCount == 0)
            {
                UpdateRenderer();
            }
        }

        protected bool TryGetTopBlockerIfActive(out BlockerInfo blocker)
        {
            if (m_BlockerStack.Count > 0)
            {
                blocker = m_BlockerStack[^1];

                if (m_ViewStack[blocker.StackIndex].State != UIState.Active)
                {
                    blocker = default;
                    return false;
                }

                return true;
            }

            blocker = default;
            return false;
        }

        protected void ResumeTopPageGroup()
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

        public void CloseTop()
        {
            if (m_ViewStack.Count > 0)
            {
                CloseLast();
            }

            UpdateRenderer();
        }

        public void Close(IViewController controller)
        {
            if (controller.State is (UIState.Closing or UIState.Closed))
            {
                return;
            }

            int stackIndex = controller.StackIndex;

            while (m_ViewStack.Count > stackIndex)
            {
                CloseLast();
            }

            UpdateRenderer();
        }

        public void CloseAll()
        {
            while (m_ViewStack.Count > 0)
            {
                CloseLast();
            }

            UpdateRenderer();
        }

        protected void CloseLast()
        {
            if (m_ViewStack.Count <= 0)
            {
                return;
            }

            IViewController controller = m_ViewStack.PopBackUnsafe();

            if (!controller.Config.IsAdditive)
            {
                ResumeTopPageGroup();
            }

            int lastBlockerIndex = m_BlockerStack.Count - 1;

            if (lastBlockerIndex >= 0)
            {
                BlockerInfo blocker = m_BlockerStack[lastBlockerIndex];

                if (blocker.StackIndex == controller.StackIndex)
                {
                    m_BlockerStack.RemoveAt(lastBlockerIndex);

                    int i = m_BlockerStack.Count > 0
                        ? m_BlockerStack[^1].StackIndex
                        : 0;

                    while (i < blocker.StackIndex)
                    {
                        SetGameObjectLayer(m_ViewStack[i].ViewInstance, AdvancedUIRenderer.VisibleLayer);
                        i++;
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

        public void MakeAnimatable<T>(InterpolateFunction<T> interpolateFunction)
        {
            InterpolationCache<T>.InterpolateFunc = interpolateFunction;
        }

        private static void SetGameObjectParent(GameObject go, Transform parent, bool setAsLastSibling)
        {
            Transform transform = go.transform;
            transform.SetParent(parent, false);

            if (setAsLastSibling)
            {
                transform.SetAsLastSibling();
            }
        }

        private static void SetGameObjectLayer(GameObject go, int layer)
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

        protected abstract void LoadPrefabAsync(string key, Action<string, GameObject> callback);

        protected abstract void ReleasePrefab(string key, GameObject prefab);
    }
}