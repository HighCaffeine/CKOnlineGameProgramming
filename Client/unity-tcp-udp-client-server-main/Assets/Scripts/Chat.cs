using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chat : MonoBehaviour, IPacketReceiver
{
    private const float CHAT_WINDOW_HEIGHT = 250f;

    private bool bShowInput = false;
    private bool bShowMessages = false;
    private List<string> messages = new List<string>();
    private string currentMessage = string.Empty;
    private Vector3 chatScroll = new Vector2(0, CHAT_WINDOW_HEIGHT);
    private IEnumerator showMessagesCorutine;
    private GUIStyle chatMessageStyle;

    void Start()
    {
        Client.TCP.AddPacketReceiver(this);
    }

    void ScrollToBottom()
    {
        chatScroll.y = CHAT_WINDOW_HEIGHT;
    }

    void OnGUI()
    {
        bool enter = Event.current.type == EventType.KeyDown && (Event.current.character == '\n');
        bShowInput ^= enter;
        GUI.backgroundColor = Color.clear;
        GUI.Window(1337, new Rect(10, Screen.height - CHAT_WINDOW_HEIGHT, 400, CHAT_WINDOW_HEIGHT), ChatWindow, string.Empty);
        if (enter)
        {
            ScrollToBottom();
            if (!string.IsNullOrWhiteSpace(currentMessage) && !string.IsNullOrWhiteSpace(currentMessage))
            {
                P_RoomChatRequest roomChatRequest = default;
                roomChatRequest.message = currentMessage;
                currentMessage = string.Empty;
                Client.TCP.SendPacket2(E_PACKET.ROOM_CHAT_REQUEST, roomChatRequest);
            }
        }
    }

    void ChatWindow(int id)
    {
        // the scrollbar does not function properly
        // I will fix it in the future
        chatScroll = GUILayout.BeginScrollView(chatScroll);
        if (bShowMessages || bShowInput)
        {
            if (chatMessageStyle == null)
            {
                chatMessageStyle = new GUIStyle(GUI.skin.label);
                chatMessageStyle.fontStyle = FontStyle.Bold;
            }
            for (int i = 0; i < messages.Count; i++)
            {
                GUILayout.Label(messages[i], chatMessageStyle);
            }
        }
        GUILayout.EndScrollView();
        if (bShowInput)
        {
            GUI.SetNextControlName("Message Input");
            currentMessage = GUILayout.TextField(currentMessage, 64, Array.Empty<GUILayoutOption>());
            GUI.FocusControl("Message Input");
        }
    }

    IEnumerator ShowMessages()
    {
        bShowMessages = true;
        yield return new WaitForSeconds(5);
        bShowMessages = false;
    }

    void AddMessage(string message)
    {
        messages.Add(message);
        if (messages.Count > 64)
        {
            messages.RemoveAt(0);
        }
        if (bShowMessages && showMessagesCorutine != null)
        {
            StopCoroutine(showMessagesCorutine);
        }
        showMessagesCorutine = ShowMessages();
        StartCoroutine(showMessagesCorutine);
        ScrollToBottom();
    }

    public void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;
        switch ((E_PACKET)packetId)
        {
            case E_PACKET.ROOM_CHAT_NOTIFY:
                P_RoomChatNotify roomChatNotify = UnsafeCode.ByteArrayToStructure<P_RoomChatNotify>(packet.data);
                string color = LocalPlayerInfo.Name == roomChatNotify.userID ? "lime" : "red";
                AddMessage($"<color={color}>[{roomChatNotify.userID}] {roomChatNotify.message}</color>");

                // temp
                //if (PathVisualizer.Instance != null)
                //{
                //    PathVisualizer.Instance.TestReceive();
                //}
                break;

            case E_PACKET.ROOM_ENTER_RESPONSE:
                P_RoomEnterResponse roomEnterResponse = UnsafeCode.ByteArrayToStructure<P_RoomEnterResponse>(packet.data);
                AddMessage($"<color=blue>[Game] ROOM_ENTER_RESPONSE result = {roomEnterResponse.result}</color>");
                break;

            case E_PACKET.ROOM_NEW_USER_NTF:
                P_RoomNewUserNotify roomNewUserNotify = UnsafeCode.ByteArrayToStructure<P_RoomNewUserNotify>(packet.data);
                AddMessage($"<color=blue>[Game] {roomNewUserNotify.userName} has joined</color>");
                break;

            case E_PACKET.ROOM_USER_INFO_NTF:
                P_RoomUserInfoNotify roomUserListNotify = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);
                AddMessage($"<color=blue>[Game] {roomUserListNotify.userName} exists </color>");
                break;

            case E_PACKET.ROOM_LEAVE_USER_NTF:
                P_RoomLeaveUserNotify roomLeaveUserNotify = UnsafeCode.ByteArrayToStructure<P_RoomLeaveUserNotify>(packet.data);
                AddMessage($"<color=blue>[Game] {roomLeaveUserNotify.userName} has left</color>");
                break;

            default:
                break;
        }
    }

    void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
    }
}
