using System;
using UnityEngine;
using UnityEngine.Events;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings.LowLevel
{
    public static class BindingUtility
    {
        private static class DelegateCache<TComponent> where TComponent : class
        {
            public static readonly Action<UniversalBinding> ValueInitAction = self =>
            {
                self.CustomArg0 = self.MountTarget.GetComponent<TComponent>();
            };

            public static readonly Action<UniversalBinding> EventInitAction = self =>
            {
                var getter = (Func<TComponent, UnityEvent>)self.CustomArg1;
                var unityEvent = getter(self.MountTarget.GetComponent<TComponent>());
                var handler = WrapEventHandler(self, (UnityAction)self.CustomArg2);
                unityEvent.AddListener(handler);

                self.CustomArg0 = unityEvent;
                self.CustomArg2 = handler;
            };

            public static readonly Action<UniversalBinding> EventDestroyAction = self =>
            {
                var unityEvent = (UnityEvent)self.CustomArg0;
                var handler = (UnityAction)self.CustomArg2;
                unityEvent.RemoveListener(handler);
            };
        }

        private static class DelegateCache<TComponent, TValue> where TComponent : class
        {
            public static readonly Action<UniversalBinding> EventInitAction = self =>
            {
                var getter = (Func<TComponent, UnityEvent<TValue>>)self.CustomArg1;
                var unityEvent = getter(self.MountTarget.GetComponent<TComponent>());
                var handler = WrapEventHandler(self, (UnityAction<TValue>)self.CustomArg2);
                unityEvent.AddListener(handler);

                self.CustomArg0 = unityEvent;
                self.CustomArg2 = handler;
            };

            public static readonly Action<UniversalBinding> EventDestroyAction = self =>
            {
                var unityEvent = (UnityEvent<TValue>)self.CustomArg0;
                var handler = (UnityAction<TValue>)self.CustomArg2;
                unityEvent.RemoveListener(handler);
            };

            public static readonly Action<AnimatedUniversalBinding<TValue>, TValue> ApplyAction = (self, value) =>
            {
                var component = (TComponent)self.CustomArg0;
                var setter = (Action<TComponent, TValue>)self.CustomArg2;
                setter(component, value);
            };

            public static readonly Action<UniversalBinding, IAnimationUpdater> RenderAction = (self, animationUpdater) =>
            {
                var component = (TComponent)self.CustomArg0;
                var setter = (Action<TComponent, TValue>)self.CustomArg2;
                var value = (Func<TValue>)self.CustomArg3;

                if (self is AnimatedUniversalBinding<TValue> animatedSelf)
                {
                    var getter = (Func<TComponent, TValue>)self.CustomArg1;
                    animatedSelf.StartTransition(getter(component), value(), animationUpdater);
                }
                else
                {
                    setter(component, value());
                }
            };
        }

        public static void BindComponentValue<TComponent, TValue>(
            GameObject mountTarget,
            Func<TComponent, TValue> getter,
            Action<TComponent, TValue> setter,
            Func<TValue> value) where TComponent : class
        {
            BaseBinding
                .Allocate<UniversalBinding>()
                .Initialize(mountTarget,
                    initAction: DelegateCache<TComponent>.ValueInitAction,
                    renderAction: DelegateCache<TComponent, TValue>.RenderAction,
                    customArg1: getter,
                    customArg2: setter,
                    customArg3: value);
        }

        public static void BindComponentValue<TComponent, TValue>(
            GameObject mountTarget,
            Func<TComponent, TValue> getter,
            Action<TComponent, TValue> setter,
            Func<TValue> value,
            in TransitionConfig transitionConfig) where TComponent : class
        {
            var binding = BaseBinding.Allocate<AnimatedUniversalBinding<TValue>>();

            binding.Initialize(mountTarget,
                initAction: DelegateCache<TComponent>.ValueInitAction,
                renderAction: DelegateCache<TComponent, TValue>.RenderAction,
                customArg1: getter,
                customArg2: setter,
                customArg3: value);

            binding.InitializeTransitionConfig(
                DelegateCache<TComponent, TValue>.ApplyAction,
                transitionConfig);
        }

        public static void BindComponentValue<TComponent, TValue>(
            GameObject mountTarget,
            Func<TComponent, TValue> getter,
            Action<TComponent, TValue> setter,
            Func<TValue> value,
            SharedValue<TransitionConfig> transitionConfig) where TComponent : class
        {
            var binding = BaseBinding.Allocate<AnimatedUniversalBinding<TValue>>();

            binding.Initialize(mountTarget,
                initAction: DelegateCache<TComponent>.ValueInitAction,
                renderAction: DelegateCache<TComponent, TValue>.RenderAction,
                customArg1: getter,
                customArg2: setter,
                customArg3: value);

            binding.InitializeTransitionConfig(
                DelegateCache<TComponent, TValue>.ApplyAction,
                transitionConfig);
        }

        public static void BindComponentEvent<TComponent>(
            GameObject mountTarget,
            Func<TComponent, UnityEvent> getter,
            UnityAction handler) where TComponent : class
        {
            BaseBinding
                .Allocate<UniversalBinding>()
                .Initialize(mountTarget,
                    initAction: DelegateCache<TComponent>.EventInitAction,
                    destroyAction: DelegateCache<TComponent>.EventDestroyAction,
                    customArg1: getter,
                    customArg2: handler);
        }

        public static void BindComponentEvent<TComponent, TValue>(
            GameObject mountTarget,
            Func<TComponent, UnityEvent<TValue>> getter,
            UnityAction<TValue> handler) where TComponent : class
        {
            BaseBinding
                .Allocate<UniversalBinding>()
                .Initialize(mountTarget,
                    initAction: DelegateCache<TComponent, TValue>.EventInitAction,
                    destroyAction: DelegateCache<TComponent, TValue>.EventDestroyAction,
                    customArg1: getter,
                    customArg2: handler);
        }

        public static UnityAction WrapEventHandler(UniversalBinding self, UnityAction handler)
        {
            if (handler == null)
            {
                return null;
            }

            return () =>
            {
                // 如果有监听者，很可能是某个 binding 在 render 时导致事件被触发，这时候没必要再调用回调
                if (ChangeUtility.CurrentObserver is not null)
                {
                    return;
                }

                self.IsPassivelyObservingChanges = true;
                ChangeUtility.BeginObservedRegion(self);

                try
                {
                    handler();
                }
                finally
                {
                    ChangeUtility.EndObservedRegion(self);
                    self.IsPassivelyObservingChanges = false;
                }
            };
        }

        public static UnityAction<T> WrapEventHandler<T>(UniversalBinding self, UnityAction<T> handler)
        {
            if (handler == null)
            {
                return null;
            }

            return (T value) =>
            {
                // 如果有监听者，很可能是某个 binding 在 render 时导致事件被触发，这时候没必要再调用回调
                if (ChangeUtility.CurrentObserver is not null)
                {
                    return;
                }

                self.IsPassivelyObservingChanges = true;
                ChangeUtility.BeginObservedRegion(self);

                try
                {
                    handler(value);
                }
                finally
                {
                    ChangeUtility.EndObservedRegion(self);
                    self.IsPassivelyObservingChanges = false;
                }
            };
        }
    }
}