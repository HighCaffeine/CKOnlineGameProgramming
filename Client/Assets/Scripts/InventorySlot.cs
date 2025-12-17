using UnityEngine;
using UnityEngine.EventSystems; // 클릭 이벤트용 네임스페이스
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image iconImage;
    public int slotIndex;

    public Button slotButton;

    public int currentItemID { get; private set; }
    private Inventory parentInventory;

    private void Awake()
    {
        parentInventory = GetComponentInParent<Inventory>();
    }

    public void UpdateSlot(int itemID)
    {
        currentItemID = itemID;

        if (itemID == 0)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        else
        {
            iconImage.sprite = ItemDataBase.Instance.GetItemSprite(itemID);
            iconImage.enabled = true;
        }
    }

    public void SetSlotButtonInteractable(bool isAllow)
    {
        if (slotButton) slotButton.interactable = isAllow;
    }

    public void OnClick()
    {
        if (parentInventory != null)
        {
            parentInventory.OnSlotClick(slotIndex, currentItemID);
        }
    }
}