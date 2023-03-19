using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VentiCola.UI.Bindings;
using VentiCola.UI.Factories;
using VentiCola.UI.Internals;
using VentiCola.UI.Stacking;

#if PACKAGE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace VentiCola.UI
{
    using Object = UnityEngine.Object;

    public static class UIManager
    {
        private class OpenPageTask : IReusableObject
        {
            public string Key;
            public UIPage Page;
            public ReactiveModel Model;
            public bool BlendTransitions;
            public OpenPageTask Prev;
            public OpenPageTask Next;
            public int Version;

            int IReusableObject.Version => Version;

            public void ResetObject()
            {
                Key = default;
                Page = default;
                Model = default;
                BlendTransitions = default;
                Prev = default;
                Next = default;
                Version++;
            }
        }

        private struct OpenPageTaskQueue
        {
            public OpenPageTask Head;
            public OpenPageTask Tail;
            private OpenPageTask m_FreeList;

            public bool IsEmpty
            {
                get => Head is null;
            }

            public OpenPageTask Enqueue(string key, UIPage page, ReactiveModel model, bool blendTransitions)
            {
                OpenPageTask task;

                if (m_FreeList is null)
                {
                    task = new OpenPageTask();
                }
                else
                {
                    task = m_FreeList;
                    m_FreeList = task.Next;
                }

                task.Key = key;
                task.Page = page;
                task.Model = model;
                task.BlendTransitions = blendTransitions;
                task.Prev = Tail;
                task.Next = null;

                if (Tail is null)
                {
                    Head = task;
                }
                else
                {
                    Tail.Next = task;
                }

                Tail = task;

                return task;
            }

            public bool TryPeek(out OpenPageTask task)
            {
                if (Head is null || Head.Page == null)
                {
                    task = default;
                    return false;
                }

                task = Head;
                return true;
            }

            public void RemoveAndRecycle(OpenPageTask task)
            {
                if (task.Prev is null)
                {
                    Head = task.Next;
                }
                else
                {
                    task.Prev.Next = task.Next;
                }

                if (task.Next is null)
                {
                    Tail = task.Prev;
                }
                else
                {
                    task.Next.Prev = task.Prev;
                }

                task.ResetObject();

                task.Next = m_FreeList;
                m_FreeList = task;
            }

            public void Clear()
            {
                var task = Head;

                while (task is not null)
                {
                    var nextTask = task.Next;
                    task.ResetObject();

                    task.Next = m_FreeList;
                    m_FreeList = task;

                    task = nextTask;
                }
            }
        }

        private static readonly UIRuntimeSettings s_Settings;

        private static OpenPageTaskQueue s_OpenPageQueue;
        private static readonly Action<IReusableObject, UIPage> s_PageResolvedCallback;
        private static readonly Action<IReusableObject, Exception> s_PageRejectedCallback;
        private static readonly IAsyncPageFactory s_PageFactory;

        private static Transform s_UIPoolRoot;
        private static Dictionary<string, UIPage> s_UIPool; // 只缓存 1 个

        private static Transform s_UIRoot;
        private static UIStack s_UIStack;

#if PACKAGE_URP
        private static Camera s_MainCamera;
        private static Camera s_UICamera;
#endif

        static UIManager()
        {
            s_Settings = UIRuntimeSettings.FindInstance();

            s_OpenPageQueue = new OpenPageTaskQueue();
            s_PageResolvedCallback = OnPageResolved;
            s_PageRejectedCallback = OnPageRejected;
            s_PageFactory = (IAsyncPageFactory)Activator.CreateInstance(Type.GetType(s_Settings.PageFactoryTypeName));

            InitUIPool();
            InitUIRootAndStackAndCamera();
        }

#if PACKAGE_URP
        public static Camera MainCamera
        {
            get => s_MainCamera;
            set
            {
                if (s_MainCamera != null)
                {
                    var prevURPData = s_MainCamera.GetUniversalAdditionalCameraData();
                    prevURPData.cameraStack.Remove(s_UICamera);
                }

                s_MainCamera = value;

                var urpData = s_MainCamera.GetUniversalAdditionalCameraData();
                List<Camera> cameraStack = urpData.cameraStack;

                for (int i = 0; i < cameraStack.Count; i++)
                {
                    if (cameraStack[i] == s_UICamera)
                    {
                        return;
                    }
                }

                cameraStack.Add(s_UICamera);
            }
        }
#endif

        private static void InitUIPool()
        {
            var go = new GameObject("UI Pool");
            Object.DontDestroyOnLoad(go);

            s_UIPoolRoot = go.transform;
            s_UIPoolRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            s_UIPool = new Dictionary<string, UIPage>();
        }

        private static void InitUIRootAndStackAndCamera()
        {
            GameObject go = Object.Instantiate(s_Settings.UIRootPrefab, Vector3.zero, Quaternion.identity);
            Object.DontDestroyOnLoad(go);

            Canvas canvas = go.GetComponentInChildren<Canvas>();

            if (!canvas.isRootCanvas)
            {
                canvas = canvas.rootCanvas;
            }

            s_UIRoot = canvas.transform;
            s_UIStack = new UIStack(s_Settings.UIStackMinGrow, s_Settings.UIStackMaxGrow, RecyclePage);

#if PACKAGE_URP
            s_UICamera = canvas.worldCamera;
            MainCamera = Camera.main;
#endif
        }

        public static void Initialize() { }

        public static ReactiveModel GetGlobalModel(string name)
        {
            return GetGlobalModel<ReactiveModel>(name);
        }

        public static T GetGlobalModel<T>(string name) where T : ReactiveModel
        {
            if (PropertyProxyUtility.TryGetGlobalModel(name, out ReactiveModel model))
            {
                return model as T;
            }

            return default;
        }

        public static void SetGlobalModel(string name, ReactiveModel model)
        {
            PropertyProxyUtility.SetGlobalModel(name, model);
        }

        private static void SetPageParent(UIPage page, Transform parent)
        {
            page.transform.SetParent(parent, false);
        }

        public static void OpenPageAsync(string key, ReactiveModel model = null, bool blendTransitions = false)
        {
            if (s_UIPool.Remove(key, out UIPage cachedPage))
            {
                if (s_OpenPageQueue.IsEmpty)
                {
                    SetPageParent(cachedPage, s_UIRoot);
                    s_UIStack.Push(key, cachedPage, model, blendTransitions);
                }
                else
                {
                    s_OpenPageQueue.Enqueue(key, cachedPage, model, blendTransitions);
                }

                return;
            }

            var task = s_OpenPageQueue.Enqueue(key, null, model, blendTransitions);
            var handle = new PromiseHandle<UIPage>(task, s_PageResolvedCallback, s_PageRejectedCallback);
            s_PageFactory.InstantiateAsync(key, handle);
        }

        private static void OnPageResolved(IReusableObject state, UIPage page)
        {
            var task = (OpenPageTask)state;
            task.Page = page;
            OpenPendingPages();
        }

        private static void OnPageRejected(IReusableObject state, Exception exception)
        {
            var task = (OpenPageTask)state;

            Debug.LogException(exception);
            Debug.LogError($"Failed to instantiate ui page: {task.Key}.");

            s_OpenPageQueue.RemoveAndRecycle(task);
            OpenPendingPages();
        }

        private static void OpenPendingPages()
        {
            while (s_OpenPageQueue.TryPeek(out OpenPageTask task))
            {
                SetPageParent(task.Page, s_UIRoot);
                s_UIStack.Push(task.Key, task.Page, task.Model, task.BlendTransitions);
                s_OpenPageQueue.RemoveAndRecycle(task);
            }
        }

        public static void ReopenAllPagesAsync()
        {
            // 当输入设备切换时可能需要重新加载全部页面

            List<string> keys = ListPool<string>.Get();
            List<ReactiveModel> models = ListPool<ReactiveModel>.Get();

            // 清理已经打开的页面并记录
            s_UIStack.Clear(keys, models);

            // 清理掉旧的缓存
            foreach (var item in s_UIPool)
            {
                s_PageFactory.Destroy(item.Key, item.Value);
            }

            s_UIPool.Clear();

            // TODO: clean Queue

            // 重新打开之前的页面
            for (int i = 0; i < keys.Count; i++)
            {
                OpenPageAsync(keys[i], models[i]);
            }

            ListPool<string>.Release(keys);
            ListPool<ReactiveModel>.Release(models);
        }

        public static bool CloseTopPage(bool blendTransitions = false)
        {
            if (!s_UIStack.TryPeek(out UIPage page))
            {
                return false;
            }

            page.RequestClose(blendTransitions, out bool success);
            return success;
        }

        public static void CloseAllPages()
        {
            s_OpenPageQueue.Clear();
            s_UIStack.Clear();
        }

        private static void RecyclePage(string key, UIPage page)
        {
            if (!page.IsCacheable || !s_UIPool.TryAdd(key, page))
            {
                s_PageFactory.Destroy(key, page);
            }
            else
            {
                SetPageParent(page, s_UIPoolRoot);
            }
        }
    }
}