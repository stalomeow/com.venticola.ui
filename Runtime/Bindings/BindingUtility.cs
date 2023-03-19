using System;
using System.Collections.Generic;
using UnityEngine;
using VentiCola.UI.Internals;

namespace VentiCola.UI.Bindings
{
    public static class BindingUtility
    {
        public static void SetDirty(IBinding binding)
        {
            if (binding.IsSelfDirty)
            {
                return;
            }

            binding.IsSelfDirty = true;

            // 向上通知所有的父级
            ILayoutBinding parent = binding.ParentBinding;

            while (parent != null)
            {
                if (!parent.DirtyChildBindings.Add(binding))
                {
                    break;
                }

                binding = parent;
                parent = binding.ParentBinding;
            }
        }

        public static void RenderIfDirty(IBinding binding)
        {
            // 要先渲染自己，再渲染子 Binding！

            if (binding.IsSelfDirty)
            {
                RenderSelfSafe(binding);
            }

            if (binding is ILayoutBinding layoutBinding && layoutBinding.EnableChildBindingRendering)
            {
                RenderChildBindings(layoutBinding);
            }
        }

        private static void RenderSelfSafe(IBinding binding)
        {
            ChangeUtility.BeginObservedRegion(binding);

            try
            {
                // 在渲染页面之前把各种值都计算出来，这样可以知道数据是不是真的发生了变化。
                // e.g. Computed 是惰性求值的，有可能 Computed 依赖的属性发生了变化，但是最终计算出的值没变。
                binding.CalculateValues(out bool changed);

                // 只在必要时重新渲染。
                if (changed || binding.IsFirstRendering)
                {
                    binding.RenderSelf();
                }

                // 设置状态。如果上面的代码抛出了异常，那么会跳过这部分代码。
                // *注：即使上面没有真的重新渲染，也要设置这些状态！否则 Dirty 标记清不掉，会导致 bug。
                binding.IsSelfDirty = false;
                binding.IsFirstRendering = false;

                if (!binding.IsPassive)
                {
                    // 如果代码覆盖率已经达到 100%，那么已经没必要再主动监听新的变化了。
                    // 这时候只会被动监听所有依赖的变化。这样可以减少一部分时间上的开销。
                    binding.SetIsPassive(binding.HasCoveredAllBranchesSinceFirstRendering());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to render binding({binding.GetType().FullName})!", binding.GetOwnerGameObject());
            }
            finally
            {
                ChangeUtility.EndObservedRegion(binding);
            }
        }

        private static void RenderChildBindings(ILayoutBinding layoutBinding)
        {
            HashSet<IBinding> dirtyChildren = layoutBinding.DirtyChildBindings;

            if (dirtyChildren.Count > 0)
            {
                foreach (var dirtyChild in dirtyChildren)
                {
                    RenderIfDirty(dirtyChild);
                }

                dirtyChildren.Clear();
            }
        }
    }
}
