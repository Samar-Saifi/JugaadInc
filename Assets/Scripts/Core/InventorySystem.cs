using System;
using System.Collections.Generic;
using UnityEngine;

namespace JugaadInc
{
    public class InventorySystem : MonoBehaviour
    {
        [SerializeField] private int maxCapacity = 16;
        [SerializeField] private bool allowDuplicates = true;

        private readonly List<InventoryItem> items = new List<InventoryItem>();
        private int selectedIndex = -1;

        public event Action<InventoryItem> OnItemAdded;
        public event Action<InventoryItem> OnItemRemoved;
        public event Action OnInventoryChanged;
        public event Action<int, InventoryItem> OnItemSelected;

        public int Count => items.Count;
        public int MaxCapacity => maxCapacity;
        public bool IsFull => items.Count >= maxCapacity;
        public IReadOnlyList<InventoryItem> Items => items.AsReadOnly();
        public int SelectedIndex => selectedIndex;

        public InventoryItem SelectedItem
        {
            get
            {
                if (selectedIndex >= 0 && selectedIndex < items.Count)
                    return items[selectedIndex];
                return null;
            }
        }

        public bool AddItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null) return false;

            if (IsFull) return false;

            if (!allowDuplicates)
            {
                var existing = items.Find(x => x.Data.ItemId == itemData.ItemId);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            var newItem = new InventoryItem(itemData, quantity);
            items.Add(newItem);

            OnItemAdded?.Invoke(newItem);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(InventoryItem item)
        {
            if (item == null || !items.Contains(item)) return false;

            items.Remove(item);
            if (selectedIndex >= items.Count)
            {
                selectedIndex = items.Count - 1;
            }

            OnItemRemoved?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItemByInstanceId(string instanceId)
        {
            var item = items.Find(x => x.UniqueInstanceId == instanceId);
            return RemoveItem(item);
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                selectedIndex = -1;
            }
            else
            {
                selectedIndex = index;
            }
            OnItemSelected?.Invoke(selectedIndex, SelectedItem);
        }

        public void Clear()
        {
            items.Clear();
            selectedIndex = -1;
            OnInventoryChanged?.Invoke();
        }

        public bool ContainsItem(string itemId)
        {
            return items.Exists(x => x.Data != null && x.Data.ItemId == itemId);
        }
    }
}
