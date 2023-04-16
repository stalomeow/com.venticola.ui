using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VentiCola.UI.Bindings;
using VentiCola.UI.Internals;
using Object = UnityEngine.Object;

#if PACKAGE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace VentiCola.UI
{
    public abstract class BaseUIManager
    {
        private static readonly Dictionary<Type, CustomControllerForUIPageAttribute> s_ControllerConfigs = new();

        private readonly UIRuntimeSettings m_Settings;
        private readonly Action<object, BaseUIPageView> m_PageResolvedCallback;
        private readonly Action<object, Exception> m_PageRejectedCallback;
        private readonly Action<BaseUIPageController, bool> m_PageClosedCallback;

        private Transform m_UIRoot;
        private Transform m_UIPoolRoot;
        private List<BaseUIPageController> m_PageStack;
        private Queue<BaseUIPageController> m_PendingPages;
        private Dictionary<Type, BaseUIPageController> m_PersistentCache;
        private LRUMultiHashMap<Type, BaseUIPageController> m_LRUCache;

#if PACKAGE_URP
        private Camera m_MainCamera;
        private Camera m_UICamera;
#endif

        // expose the camera of canvas

        protected BaseUIManager()
        {
            m_Settings = UIRuntimeSettings.FindInstance();
            m_PageResolvedCallback = OnPageResolved;
            m_PageRejectedCallback = OnPageRejected;
            m_PageClosedCallback = OnPageClosed;

            m_PageStack = new List<BaseUIPageController>();
            m_PendingPages = new Queue<BaseUIPageController>();
            m_PersistentCache = new Dictionary<Type, BaseUIPageController>();
            m_LRUCache = new LRUMultiHashMap<Type, BaseUIPageController>(m_Settings.LRUCacheSize);
            m_LRUCache.OnEliminated += OnLRUPageEliminated;

            InitUIPoolRoot();
            InitUIRootAndCamera();
            InitBuiltinAnimatableTypes();
        }

        protected UIRuntimeSettings RuntimeSettings => m_Settings;

#if PACKAGE_URP
        public Camera MainCamera
        {
            get => m_MainCamera;
            set
            {
                if (m_MainCamera != null)
                {
                    var prevURPData = m_MainCamera.GetUniversalAdditionalCameraData();
                    prevURPData.cameraStack.Remove(m_UICamera);
                }

                m_MainCamera = value;

                var urpData = m_MainCamera.GetUniversalAdditionalCameraData();
                List<Camera> cameraStack = urpData.cameraStack;

                if (!cameraStack.Contains(m_UICamera))
                {
                    cameraStack.Add(m_UICamera);
                }
            }
        }
#endif

        private void OnLRUPageEliminated(Type key, BaseUIPageController controller)
        {
            var config = GetControllerConfig(controller.GetType());

            controller.Destroy(out BaseUIPageView view);
            Destroy(config.ViewPrefabKey, view);

            Debug.LogWarning($"LRU eliminate ui page '{config.ViewPrefabKey}'");
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
            GameObject go = Object.Instantiate(m_Settings.UIRootPrefab, Vector3.zero, Quaternion.identity);
            Object.DontDestroyOnLoad(go);

            Canvas canvas = go.GetComponentInChildren<Canvas>();

            if (!canvas.isRootCanvas)
            {
                canvas = canvas.rootCanvas;
            }

            m_UIRoot = canvas.transform;

#if PACKAGE_URP
            m_UICamera = canvas.worldCamera;
            MainCamera = Camera.main;
#endif
        }

        private void InitBuiltinAnimatableTypes()
        {
            MakeTypeAnimatable<float>(Mathf.LerpUnclamped);
            MakeTypeAnimatable<Vector2>(Vector2.LerpUnclamped);
            MakeTypeAnimatable<Vector3>(Vector3.LerpUnclamped);
            MakeTypeAnimatable<Vector4>(Vector4.LerpUnclamped);
            MakeTypeAnimatable<Color>(Color.LerpUnclamped);
        }

        protected static CustomControllerForUIPageAttribute GetControllerConfig(Type controllerType)
        {
            if (!s_ControllerConfigs.TryGetValue(controllerType, out var config))
            {
                config = controllerType.GetCustomAttribute<CustomControllerForUIPageAttribute>(false);
                s_ControllerConfigs.Add(controllerType, config);
            }

            return config;
        }

        public T Allocate<T>() where T : BaseUIPageController, new()
        {
            var controllerType = typeof(T);
            var config = GetControllerConfig(controllerType);

            switch (config.CacheType)
            {
                case UICacheType.LastOnly:
                    {
                        if (m_PersistentCache.Remove(controllerType, out var controller))
                        {
                            return (T)controller;
                        }
                        break;
                    }

                case UICacheType.LRU:
                    {
                        if (m_LRUCache.TryTake(controllerType, out var controller))
                        {
                            return (T)controller;
                        }
                        break;
                    }
            }

            return new T();
        }

        public void Open(BaseUIPageController controller, bool additive = false)
        {
            if (!additive)
            {
                PauseTopPageGroupIfNot();
            }

            controller.WillOpen(additive);

            if (controller.View != null)
            {
                OpenPageCore(controller);
                return;
            }

            m_PendingPages.Enqueue(controller); // 在实例化之前加入队列，因为实例化有可能立刻完成

            var config = GetControllerConfig(controller.GetType());
            InstantiateAsync(config.ViewPrefabKey,
                new PromiseCallbacks<BaseUIPageView>(controller, m_PageResolvedCallback, m_PageRejectedCallback));
        }

        private void PauseTopPageGroupIfNot()
        {
            for (int i = m_PageStack.Count - 1; i >= 0; i--)
            {
                var controller = m_PageStack[i];

                if (controller.State != UIPageControllerState.Paused)
                {
                    controller.Pause();
                }

                if (!controller.IsAdditive)
                {
                    break;
                }
            }
        }

        private void ResumeTopPageGroupIfNot()
        {
            for (int i = m_PageStack.Count - 1; i >= 0; i--)
            {
                var controller = m_PageStack[i];

                if (controller.State != UIPageControllerState.Active)
                {
                    controller.Resume();
                }

                if (!controller.IsAdditive)
                {
                    break;
                }
            }
        }

        private void OnPageResolved(object state, BaseUIPageView page)
        {
            var controller = (BaseUIPageController)state;
            controller.SetView(page);
            OpenPendingPages();
        }

        private void OnPageRejected(object state, Exception exception)
        {
            var controller = (BaseUIPageController)state;
            var config = GetControllerConfig(controller.GetType());

            Debug.LogException(exception);
            Debug.LogError($"Failed to instantiate ui page '{config.ViewPrefabKey}'.");

            controller.SetView(null);
            OpenPendingPages();
        }

        private void OpenPendingPages()
        {
            while (m_PendingPages.TryPeek(out BaseUIPageController controller))
            {
                if (controller.State != UIPageControllerState.WillOpen)
                {
                    m_PendingPages.Dequeue();

                    if (controller.State == UIPageControllerState.Error)
                    {
                        controller.Destroy(out _);
                    }

                    ResumeTopPageGroupIfNot();
                    continue;
                }

                if (controller.View == null)
                {
                    break; // View 还在加载
                }

                m_PendingPages.Dequeue();

                if (!controller.IsAdditive)
                {
                    PauseTopPageGroupIfNot();
                }

                OpenPageCore(controller);
            }
        }

        private void OpenPageCore(BaseUIPageController controller)
        {
            SetPageParent(controller.View, m_UIRoot); // 从此处真正开始渲染
            m_PageStack.Add(controller);
            controller.Open();
        }

        private static void SetPageParent(BaseUIPageView page, Transform parent)
        {
            page.transform.SetParent(parent, false);
        }

        public void CloseTop()
        {
            if (m_PendingPages.Count > 0 || m_PageStack.Count == 0)
            {
                return;
            }

            var lastIndex = m_PageStack.Count - 1;
            var topController = m_PageStack[lastIndex];
            m_PageStack.RemoveAt(lastIndex);

            var isTopAdditive = topController.IsAdditive;
            ClosePageCore(topController);

            if (!isTopAdditive)
            {
                ResumeTopPageGroupIfNot();
            }
        }

        private void ClosePageCore(BaseUIPageController topController)
        {
            var topControllerType = topController.GetType();
            var config = GetControllerConfig(topControllerType);

            switch (config.CacheType)
            {
                case UICacheType.LastOnly: // 只保留最后一个
                    if (m_PersistentCache.Remove(topControllerType, out var prevController))
                    {
                        prevController.Destroy(out BaseUIPageView view);
                        Destroy(config.ViewPrefabKey, view);
                    }
                    m_PersistentCache.Add(topControllerType, topController);
                    break;

                case UICacheType.LRU:
                    m_LRUCache.Add(topControllerType, topController);
                    break;
            }

            topController.Close(m_PageClosedCallback);
        }

        private void OnPageClosed(BaseUIPageController controller, bool success)
        {
            var config = GetControllerConfig(controller.GetType());

            if (config.CacheType == UICacheType.Never)
            {
                controller.Destroy(out BaseUIPageView view);
                Destroy(config.ViewPrefabKey, view);
            }
            else
            {
                SetPageParent(controller.View, m_UIPoolRoot);
            }
        }

        public void CloseAll()
        {
            for (int i = m_PageStack.Count - 1; i >= 0; i--)
            {
                ClosePageCore(m_PageStack[i]);
            }

            m_PendingPages.Clear();
            m_PageStack.Clear();
        }

        public void MakeTypeAnimatable<T>(InterpolateFunction<T> interpolateFunction)
        {
            InterpolationCache<T>.InterpolateFunc = interpolateFunction;
        }

        /// <summary>
        /// 异步实例化一个 UI 页面
        /// </summary>
        /// <param name="key">UI 页面的 Key</param>
        /// <param name="callbacks">callbacks</param>
        protected abstract void InstantiateAsync(string key, PromiseCallbacks<BaseUIPageView> callbacks);

        /// <summary>
        /// 销毁一个 UI 页面
        /// </summary>
        /// <param name="key">UI 页面的 Key</param>
        /// <param name="page">UI 页面对象</param>
        protected abstract void Destroy(string key, BaseUIPageView page);
    }
}