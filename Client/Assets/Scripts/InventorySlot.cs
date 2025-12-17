using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image iconImage; // 아이콘을 표시할 UI Image
    public int slotIndex;   // 0~4번 인덱스

    private int currentItemID = 0;

    public void UpdateSlot(int itemID)
    {
        currentItemID = itemID;

        if (itemID == 0) // 0번은 빈 아이템
        {
            iconImage.sprite = null;
            iconImage.enabled = false; // 이미지 끄기
        }
        else
        {
            Sprite itemSprite = ItemDataBase.Instance.GetItemSprite(itemID);

            if (itemSprite != null)
            {
                iconImage.sprite = itemSprite;
                iconImage.enabled = true; // 이미지 켜기
            }
        }
    }
}