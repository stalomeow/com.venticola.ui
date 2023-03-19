using System;
using UnityEngine;
using VentiCola.UI;

namespace VentiColaTests.UI
{
    public static class ExampleItemQuery
    {
        public static string GetItemName(int id)
        {
            return $"Item {id}";
        }

        public static int GetItemRarity(int id)
        {
            // Rarity:
            // - 0: 普通物品
            // - 1: 珍贵物品 
            // - 2: 稀有物品
            return id % 3;
        }
    }

    public class TestItemModel : ReactiveModel
    {
        /// <summary>
        /// 玩家拥有的数量。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 物品 ID。
        /// </summary>
        public int ItemID { get; set; }

        /// <summary>
        /// 物品的名称。
        /// </summary>
        public string Name
        {
            get => ExampleItemQuery.GetItemName(ItemID);
        }

        /// <summary>
        /// 物品背景的颜色。
        /// </summary>
        public Color BackgroundColor
        {
            get => ExampleItemQuery.GetItemRarity(ItemID) switch
            {
                0 => Color.white,
                1 => Color.blue,
                2 => Color.yellow,
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// 调试信息，这个属性里面调用了非常多其他属性。
        /// </summary>
        public string DebugInfo
        {
            get
            {
                // 测试一下 try-catch-finally
                try
                {
                    // 测试一下 local variable
                    string id = ItemID.ToString();
                    string color = BackgroundColor.ToString();
                    return $"{id}-{Name}-{color}";
                }
                catch
                {
                    throw;
                }
                finally
                {
                    Debug.Log("Finally Block of DebugInfo");
                }
            }
        }

        public int DiscardedProp
        {
            get => 11451419;
        }
    }
}