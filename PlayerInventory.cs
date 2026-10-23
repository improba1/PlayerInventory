using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private InventoryData inventoryData;

    [Header("State")]
    [SerializeField] private int selectedIndex = 0;

    public event Action<PlayerInventory> OnInventoryUpdated;
    public event Action<int> OnSelectionChanged;

    public int SelectedIndex => selectedIndex;

    public ItemData SelectedItem
    {
        get
        {
            if (inventoryData == null || inventoryData.slots.Count == 0) return null;
            if (selectedIndex < 0 || selectedIndex >= inventoryData.slots.Count) return null;

            var slot = inventoryData.slots[selectedIndex];
            return (slot != null && !slot.IsEmpty) ? slot.itemData : null;
        }
    }

    private void Awake()
    {
        inventoryData?.Initialize();
    }

    public void SelectNext()
    {
        ChangeSelection((selectedIndex + 1) % InventoryData.MaxSlots);
    }

    public void SelectPrevious()
    {
        ChangeSelection((selectedIndex - 1 + InventoryData.MaxSlots) % InventoryData.MaxSlots);
    }

    public void ChangeSelection(int newIndex)
    {
        if (newIndex < 0 || newIndex >= InventoryData.MaxSlots) return;
        if (selectedIndex == newIndex) return;

        selectedIndex = newIndex;
        OnSelectionChanged?.Invoke(selectedIndex);
        OnInventoryUpdated?.Invoke(this);
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || inventoryData == null) return false;

        int amountLeft = amount;

        if (item.isStackable)
        {
            foreach (var slot in inventoryData.slots)
            {
                if (!slot.IsEmpty && slot.itemData == item && slot.stackSize < item.maxStackSize)
                {
                    int spaceInSlot = item.maxStackSize - slot.stackSize;
                    int amountToAdd = Mathf.Min(amountLeft, spaceInSlot);

                    slot.AddToStack(amountToAdd);
                    amountLeft -= amountToAdd;

                    if (amountLeft <= 0) break;
                }
            }
        }

        if (amountLeft > 0)
        {
            foreach (var slot in inventoryData.slots)
            {
                if (slot.IsEmpty)
                {
                    int amountToAdd = Mathf.Min(amountLeft, item.maxStackSize);

                    slot.itemData = item;
                    slot.stackSize = amountToAdd;
                    amountLeft -= amountToAdd;

                    if (amountLeft <= 0) break;
                }
            }
        }

        if (amountLeft < amount)
        {
            OnInventoryUpdated?.Invoke(this);
            GameEvents.TriggerItemPickedUp(item);
            return amountLeft == 0;
        }

        return false;
    }

    public ItemData DropSelectedItem()
    {
        if (inventoryData == null || selectedIndex >= inventoryData.slots.Count) return null;

        var slot = inventoryData.slots[selectedIndex];
        if (!slot.IsEmpty)
        {
            ItemData itemToDrop = slot.itemData;
            RemoveItemFromSlot(selectedIndex, 1);
            return itemToDrop;
        }

        return null;
    }

    public int GetItemCount(ItemData item)
    {
        if (inventoryData == null || item == null) return 0;

        int totalCount = 0;
        foreach (var slot in inventoryData.slots)
        {
            if (!slot.IsEmpty && slot.itemData == item)
            {
                totalCount += slot.stackSize;
            }
        }
        return totalCount;
    }

    public void RemoveItem(ItemData item, int amount)
    {
        if (inventoryData == null || item == null || amount <= 0) return;

        for (int i = 0; i < inventoryData.slots.Count; i++)
        {
            var slot = inventoryData.slots[i];
            if (!slot.IsEmpty && slot.itemData == item)
            {
                int amountToRemove = Mathf.Min(amount, slot.stackSize);
                RemoveItemFromSlot(i, amountToRemove);

                amount -= amountToRemove;
                if (amount <= 0) break;
            }
        }
    }

    public void RemoveItemFromSelectedSlot(int amount)
    {
        if (inventoryData == null || selectedIndex >= inventoryData.slots.Count) return;

        var slot = inventoryData.slots[selectedIndex];
        if (!slot.IsEmpty && slot.stackSize >= amount)
        {
            RemoveItemFromSlot(selectedIndex, amount);
        }
    }

    private void RemoveItemFromSlot(int index, int amount)
    {
        var slot = inventoryData.slots[index];
        slot.RemoveFromStack(amount);

        if (slot.stackSize <= 0)
        {
            slot.Clear();
        }

        OnInventoryUpdated?.Invoke(this);
    }
}