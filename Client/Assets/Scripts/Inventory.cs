using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Tooltip("True : Main Inventory, False : Trade/Other")]
    public bool isInventory;

    public InventorySlot[] slots;

    private const int InventorySize = 5;

    public void Awake()
    {
        if (isInventory)
        {
            Instance = this;
        }
    }

    public void SetInventory(int[] items)
    {
        if (items == null) return;

        for (int i = 0; i < InventorySize; i++)
        {
            if (i >= slots.Length) break;

            int itemID = (i < items.Length) ? items[i] : 0;

            slots[i].UpdateSlot(itemID);
        }

        Debug.Log($"[Inventory] UI Updated. (IsMain: {isInventory})");
    }

    public void SetInventoryByIndex(int index, int itemID)
    {
        if (index < 0 || index >= InventorySize)
        {
            Debug.LogWarning($"[Inventory] Invalid Slot Index: {index}");
            return;
        }

        if (slots == null || index >= slots.Length)
        {
            Debug.LogError("[Inventory] Slots array is not set up correctly");
            return;
        }

        slots[index].UpdateSlot(itemID);

        Debug.Log($"[Inventory] Slot {index} updated to Item {itemID}");
    }
}