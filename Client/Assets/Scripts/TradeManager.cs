using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

public class TradeManager : MonoBehaviour
{
    public static TradeManager Instance;

    [Header("Trade UI - Popup")]
    public GameObject tradeReqPanel; // 요청 왔을 때 뜨는 창
    public TMPro.TextMeshProUGUI requesterName;

    [Header("Trade UI - Window")]
    public GameObject tradeWindowPanel;  // 실제 거래창
    public TMPro.TextMeshProUGUI myName;
    public TMPro.TextMeshProUGUI partnerName;
    public Button myLockButton;
    public Button myConfirmButton;

    [Header("Trade UI - Inventory")]
    public Inventory myTradeInventory;
    public Inventory partnerTradeInventory;

    // 내부 상태 변수
    private string currentPartnerName;
    private long currentPartnerUUID;
    private long requestSenderUUID;
    private bool isMyLocked = false;
    private bool isPartnerLocked = false;

    private void Awake()
    {
        Instance = this;
        if (tradeReqPanel) tradeReqPanel.SetActive(false);
        if (tradeWindowPanel) tradeWindowPanel.SetActive(false);
    }

    #region Send
    public void SendTradeRequest(long targetUUID)
    {
        P_TradeRequest pkt = new P_TradeRequest();
        pkt.targetUUID = targetUUID;

        Client.TCP.SendPacket(E_PACKET.TRADE_REQUEST, pkt);
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
    public void OnRegisterItem(int slotIndex, int itemID)
    {
        if (isMyLocked) return; // 잠금 상태면 못 바꿈

        P_TradeItemUpdate pkt = new P_TradeItemUpdate();
        pkt.slotIndex = slotIndex;
        pkt.itemID = itemID;

        Client.TCP.SendPacket(E_PACKET.TRADE_ITEM_UPDATE, pkt);

        if (myTradeInventory != null)
        {
            myTradeInventory.SetInventoryByIndex(slotIndex, itemID);
        }
    }
    public void OnClickLock()
    {
        isMyLocked = true;
        if (myLockButton) myLockButton.interactable = false;

        P_TradeLock pkt = new P_TradeLock();
        pkt.isLocked = true;

        Client.TCP.SendPacket(E_PACKET.TRADE_LOCK, pkt);
        CheckConfirmState();
    }

    public void OnClickConfirm()
    {
        P_TradeConfirm pkt = new P_TradeConfirm();
        pkt.isConfirmed = true;

        Client.TCP.SendPacket(E_PACKET.TRADE_CONFIRM, pkt);
        if (myConfirmButton) myConfirmButton.interactable = false;
    }
    public void SendResponse(bool isAccept)
    {
        P_TradeResponse pkt = new P_TradeResponse();
        pkt.requesterUUID = requestSenderUUID;
        pkt.isAccept = isAccept;

        Client.TCP.SendPacket(E_PACKET.TRADE_RESPONSE, pkt);
    }

    #endregion







    public void ShowRequestPopup(string name, long uuid)
    {
        requestSenderUUID = uuid;
        if (requesterName) requesterName.text = $"Trade Req From : {name}";
        currentPartnerName = name; // 이름 저장
        tradeReqPanel.SetActive(true);
    }


    public void OpenTradeWindow(long partnerUUID)
    {
        currentPartnerUUID = partnerUUID;
        tradeWindowPanel.SetActive(true);
        tradeReqPanel.SetActive(false);

        if (myName) myName.text = LocalPlayerInfo.Name;
        if (partnerName) partnerName.text = currentPartnerName;

        // 상태 초기화
        isMyLocked = false;
        isPartnerLocked = false;
        if (myLockButton) myLockButton.interactable = true;
        if (myConfirmButton) myConfirmButton.interactable = false;

        int[] emptySlots = new int[5];

        if (myTradeInventory != null) myTradeInventory.SetInventory(emptySlots);

        if (partnerTradeInventory != null) partnerTradeInventory.SetInventory(emptySlots);
    }

    public void CloseTradeWindow(string msg)
    {
        Debug.Log(msg);
        tradeWindowPanel.SetActive(false);

        int[] emptySlots = new int[5];
        if (myTradeInventory != null) myTradeInventory.SetInventory(emptySlots);
        if (partnerTradeInventory != null) partnerTradeInventory.SetInventory(emptySlots);
    }

    public void CheckConfirmState()
    {
        if (isMyLocked && isPartnerLocked)
        {
            if (myConfirmButton) myConfirmButton.interactable = true;
        }
    }

    public void SetPartnerLockState(bool isLock)
    {
        isPartnerLocked = isLock;
        CheckConfirmState();
    }

    public void SetPartnerItem(int index, int itemID)
    {
        if (partnerTradeInventory != null)
        {
            partnerTradeInventory.SetInventoryByIndex(index, itemID);
        }
    }
}