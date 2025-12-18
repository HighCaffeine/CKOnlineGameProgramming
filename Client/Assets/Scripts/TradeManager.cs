using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Runtime.InteropServices;

public enum TradeState
{
    Enter = 0,   // 거래 입장
    Locked = 1   // 잠금 완료
}

public class TradeManager : MonoBehaviour
{
    public static TradeManager Instance;

    [Header("Trade UI - Popup")]
    public GameObject tradeReqPanel;
    public TMPro.TextMeshProUGUI requesterName;

    [Header("Trade UI - Window")]
    public GameObject tradeWindowPanel;
    public TMPro.TextMeshProUGUI myName;
    public TMPro.TextMeshProUGUI partnerName;
    public Button myLockButton;
    public Button myConfirmButton;

    [Header("Trade UI - Status Icons")]
    public Image myStatusImage;
    public Image partnerStatusImage;

    public Sprite[] statusSprites;

    [Header("Trade UI - Inventory")]
    public Inventory myTradeInventory;
    public Inventory partnerTradeInventory;

    [Header("UI Blockers")]
    public GameObject myInventoryBlocker;
    public GameObject partnerInventoryBlocker;


    [Header("Player Inventory")]
    public Inventory myInventory;

    private string currentPartnerName;
    private long currentPartnerUUID;
    private long requestSenderUUID;

    public bool IsMyLocked => isMyLocked;

    private bool isMyLocked = false;
    private bool isPartnerLocked = false;

    private void Awake()
    {
        Instance = this;
        if (tradeReqPanel) tradeReqPanel.SetActive(false);
        if (tradeWindowPanel) tradeWindowPanel.SetActive(false);

        if (myInventoryBlocker) myInventoryBlocker.SetActive(false);
        if (partnerInventoryBlocker) partnerInventoryBlocker.SetActive(false);
    }

    public void SendTradeRequest(long targetUUID)
    {
        P_TradeRequest pkt = new P_TradeRequest();
        pkt.targetUUID = targetUUID;
        Client.TCP.SendPacket2(E_PACKET.TRADE_REQUEST, pkt);
    }

    public int GetEmptyMyTradeSlot()
    {
        if (myTradeInventory != null && myTradeInventory.slots != null)
        {
            for (int i = 0; i < myTradeInventory.slots.Length; i++)
            {
                if (myTradeInventory.slots[i].currentItemID == 0)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public void OnClickAccept()
    {
        SendResponse(true);
        tradeReqPanel.SetActive(false);
    }

    public void OnClickReject()
    {
        SendResponse(false);
        tradeReqPanel.SetActive(false);
    }

    public void OnRegisterItem(int myInvenSlotIndex, int itemID)
    {
        if (!tradeWindowPanel.activeSelf) return;
        if (isMyLocked) return;
        if (itemID == 0) return;

        int targetSlot = GetEmptyMyTradeSlot();

        if (targetSlot == -1)
        {
            return;
        }

        P_TradeItemUpdate pkt = new P_TradeItemUpdate();
        pkt.invenSlot = myInvenSlotIndex;
        pkt.tradeSlot = targetSlot;
        pkt.itemID = itemID;
        Client.TCP.SendPacket2(E_PACKET.TRADE_ITEM_UPDATE, pkt);


        if (myTradeInventory != null)
        {
            myTradeInventory.SetInventoryByIndex(targetSlot, itemID);
        }

        if (myInventory != null)
        {
            myInventory.SetInventoryByIndex(myInvenSlotIndex, 0);
        }
    }

    public void ReturnItemsToInventory()
    {
        if (myTradeInventory == null || myTradeInventory.slots == null) return;

        for (int i = 0; i < myTradeInventory.slots.Length; i++)
        {
            int itemID = myTradeInventory.slots[i].currentItemID;

            if (itemID != 0)
            {
                int emptySlot = Inventory.Instance.GetEmptySlot();

                if (emptySlot != -1)
                {
                    Inventory.Instance.SetInventoryByIndex(emptySlot, itemID);
                    myTradeInventory.SetInventoryByIndex(i, 0);
                }
            }
        }
    }

    public void OnClickLock()
    {
        isMyLocked = true;
        if (myLockButton) myLockButton.interactable = false;

        if (myInventoryBlocker != null) myInventoryBlocker.SetActive(true);

        SetPlayerStatusUI(true, TradeState.Locked);

        P_TradeLock pkt = new P_TradeLock();
        pkt.isLocked = true;

        Client.TCP.SendPacket2(E_PACKET.TRADE_LOCK, pkt);
        CheckConfirmState();
    }

    public void OnClickConfirm()
    {
        P_TradeConfirm pkt = new P_TradeConfirm();
        pkt.isConfirmed = true;

        Client.TCP.SendPacket2(E_PACKET.TRADE_CONFIRM, pkt);
        if (myConfirmButton) myConfirmButton.interactable = false;
    }

    public void SendResponse(bool isAccept)
    {
        P_TradeResponse pkt = new P_TradeResponse();
        pkt.requesterUUID = requestSenderUUID;
        pkt.isAccept = isAccept;

        Client.TCP.SendPacket2(E_PACKET.TRADE_RESPONSE, pkt);
    }

    public void ShowRequestPopup(string name, long uuid)
    {
        requestSenderUUID = uuid;
        if (requesterName) requesterName.text = $"Trade Req From : {name}";
        currentPartnerName = name;
        tradeReqPanel.SetActive(true);
    }

    // 거래창이 열릴 때 (Start 패킷 수신)
    public void OpenTradeWindow(long partnerUUID, string partnerNameStr)
    {
        currentPartnerUUID = partnerUUID;
        currentPartnerName = partnerNameStr;

        tradeWindowPanel.SetActive(true);
        tradeReqPanel.SetActive(false);

        if (myName) myName.text = LocalPlayerInfo.Name;

        // 갱신된 이름으로 UI 설정
        if (partnerName) partnerName.text = currentPartnerName;

        isMyLocked = false;
        isPartnerLocked = false;

        if (myLockButton) myLockButton.interactable = true;
        if (myConfirmButton) myConfirmButton.interactable = false;

        if (myInventoryBlocker != null) myInventoryBlocker.SetActive(false);
        if (partnerInventoryBlocker != null) partnerInventoryBlocker.SetActive(false);

        SetPlayerStatusUI(true, TradeState.Enter);
        SetPlayerStatusUI(false, TradeState.Enter);

        int[] emptySlots = new int[9];
        if (myTradeInventory != null) myTradeInventory.SetInventory(emptySlots);
        if (partnerTradeInventory != null) partnerTradeInventory.SetInventory(emptySlots);
    }

    public void CloseTradeWindow(string msg, bool isSuccess = false)
    {
        Debug.Log(msg);
        tradeWindowPanel.SetActive(false);

        if (!isSuccess)
        {
            ReturnItemsToInventory();
        }

        if (myInventoryBlocker != null) myInventoryBlocker.SetActive(false);
        if (partnerInventoryBlocker != null) partnerInventoryBlocker.SetActive(false);

        isMyLocked = false;
        isPartnerLocked = false;

        int[] emptySlots = new int[9];
        if (myTradeInventory != null) myTradeInventory.SetInventory(emptySlots);
        if (partnerTradeInventory != null) partnerTradeInventory.SetInventory(emptySlots);
    }

    public void CheckConfirmState()
    {
        if (isMyLocked && isPartnerLocked)
        {
            if (myConfirmButton) myConfirmButton.interactable = true;
        }
        else
        {
            if (myConfirmButton) myConfirmButton.interactable = false;
        }
    }

    public void SetPartnerLockState(bool isLock)
    {
        isPartnerLocked = isLock;

        if (partnerInventoryBlocker != null)
        {
            partnerInventoryBlocker.SetActive(isLock);
        }

        SetPlayerStatusUI(false, isLock ? TradeState.Locked : TradeState.Enter);
        CheckConfirmState();
    }

    public void SetPartnerConfirmState(bool isConfirmed)
    {
        if (partnerName)
        {
            partnerName.text = currentPartnerName + (isConfirmed ? " <color=yellow>(Confirmed)</color>" : "");
        }
    }

    public void SetMyConfirmState(bool isConfirmed)
    {
        if (myName)
        {
            myName.text = LocalPlayerInfo.Name + (isConfirmed ? " <color=yellow>(Confirmed)</color>" : "");
        }
    }

    public void SetPartnerItem(int index, int itemID)
    {
        if (partnerTradeInventory != null)
        {
            partnerTradeInventory.SetInventoryByIndex(index, itemID);
        }
    }

    private void SetPlayerStatusUI(bool isMine, TradeState state)
    {
        int spriteIndex = (int)state;

        if (statusSprites == null || spriteIndex < 0 || spriteIndex >= statusSprites.Length) return;

        if (isMine && myStatusImage != null)
        {
            myStatusImage.sprite = statusSprites[spriteIndex];
        }
        else if (!isMine && partnerStatusImage != null)
        {
            partnerStatusImage.sprite = statusSprites[spriteIndex];
        }
    }
}