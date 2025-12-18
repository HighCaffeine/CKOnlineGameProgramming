using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public enum InventoryType
    {
        Main,           // 내 메인 인벤토리
        Trade_My,       // 거래창 : 내 쪽
        Trade_Partner   // 거래창 : 상대방 쪽 (잠김)
    }

    public static Inventory Instance;

    [Tooltip("True : Main Inventory, False : Trade/Other")]
    public InventoryType inventoryType;

    public InventorySlot[] slots;

    private const int InventorySize = 5;

    public void Awake()
    {
        if (inventoryType == InventoryType.Main)
        {
            Instance = this;
        }
    }

    public void OnSlotClick(int slotIndex, int itemID)
    {
        if (inventoryType == InventoryType.Trade_Partner)
        {
            return;
        }

        if (inventoryType == InventoryType.Trade_My)
        {
            if (itemID != 0)
            {
                TradeManager.Instance.OnRegisterItem(slotIndex, 0);
                Debug.Log($"[Trade] {slotIndex}item cancel");
            }
        }

        if (inventoryType == InventoryType.Main)
        {
            if (TradeManager.Instance.tradeWindowPanel.activeSelf)
            {
                if (itemID == 0) return;

                int emptySlot = TradeManager.Instance.GetEmptyMyTradeSlot();

                if (emptySlot != -1)
                {
                    TradeManager.Instance.OnRegisterItem(slotIndex, itemID);

                    Debug.Log($"[Trade] {itemID} move to trade {emptySlot}slot");
                }
                else
                {
                    Debug.Log("[Trade] trade inventory is full");
                }
            }
        }
    }

    public int GetEmptySlot()
    {
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].currentItemID == 0)
                {
                    return i;
                }
            }
        }

        return -1;
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

        Debug.Log($"[Inventory] UI Updated)");
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