using System;
using UnityEngine;

namespace JugaadInc
{
    public enum ItemCategory
    {
        Material,
        Tool,
        Fastener,
        Structure,
        Mechanism,
        Container,
        Scrap,
        Special
    }

    [CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Information")]
        public string ItemId = "item_id";
        public string DisplayName = "New Item";
        [TextArea(2, 4)]
        public string Description = "";
        public Color DisplayColor = Color.white;
        public Sprite Icon;
        public ItemCategory Category = ItemCategory.Material;

        [Header("Scoring Properties")]
        public int EstimatedCost = 10;
        [Range(0, 100)]
        public int ResourcefulnessRating = 50;
        [Range(0, 100)]
        public int Versatility = 50;

        public static ItemData CreateRuntimeInstance(string id, string name, string desc, ItemCategory category, Color color, int cost = 10, int junkRating = 50)
        {
            var item = CreateInstance<ItemData>();
            item.ItemId = id;
            item.DisplayName = name;
            item.Description = desc;
            item.DisplayColor = color;
            item.Category = category;
            item.EstimatedCost = cost;
            item.ResourcefulnessRating = junkRating;
            return item;
        }
    }

    [Serializable]
    public class InventoryItem
    {
        public ItemData Data;
        public int Quantity = 1;
        public string UniqueInstanceId;

        public InventoryItem(ItemData data, int quantity = 1)
        {
            Data = data;
            Quantity = quantity;
            UniqueInstanceId = Guid.NewGuid().ToString("N");
        }

        public InventoryItem Clone()
        {
            return new InventoryItem(Data, Quantity) { UniqueInstanceId = UniqueInstanceId };
        }
    }
}
