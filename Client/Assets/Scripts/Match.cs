using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public unsafe class Match : MonoBehaviour, IPacketReceiver
{
    public static Match Current;
    public Dictionary<long, Player> Players;

    void Awake()
    {
        Debug.Log("Match started");
        Current = this;
        Players = new Dictionary<long, Player>();
        Client.TCP.AddPacketReceiver(this);
        Client.UDP.AddPacketReceiver(this);
    }

    void Start()
    {
        AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name);
        P_RoomEnterRequest request = new P_RoomEnterRequest()
        {
            roomNumber = 0
        };
        Client.TCP.SendPacket2(E_PACKET.ROOM_ENTER_REQUEST, request);
    }

    private void OnGUI()
    {
        foreach (Player player in Players.Values)
        {
            if (player.ID != LocalPlayerInfo.ID)
            {
                Vector3 scpos = GameObject.Find("Player Camera").GetComponent<Camera>().WorldToScreenPoint(player.transform.position);
                if (scpos.z > 0)
                {
                    GUI.contentColor = Color.cyan;
                    GUI.Label(new Rect(scpos.x, Screen.height - scpos.y, 100, 25), player.Name);
                }
            }
        }
    }

    void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
        Client.UDP.RemovePacketReceiver(this);
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;
        switch ((E_PACKET)packetId)
        {
            case E_PACKET.ROOM_ENTER_RESPONSE:
                P_RoomEnterResponse roomEnterResponse = UnsafeCode.ByteArrayToStructure<P_RoomEnterResponse>(packet.data);
                Debug.Log($"ROOM_ENTER_RESPONSE result={roomEnterResponse.result}");
                break;

            case E_PACKET.ROOM_NEW_USER_NTF:
                P_RoomNewUserNotify roomNewUserNotify = UnsafeCode.ByteArrayToStructure<P_RoomNewUserNotify>(packet.data);
                AddPlayer(roomNewUserNotify.userUUID, roomNewUserNotify.userName);
                Debug.Log($"Player {roomNewUserNotify.userName} has joined");
                break;

            case E_PACKET.ROOM_USER_INFO_NTF:
                P_RoomUserInfoNotify roomUserInfoNotify = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);
                Player newPlayer = AddPlayer(roomUserInfoNotify.userUUID, roomUserInfoNotify.userName);
                if (newPlayer != null)
                {
                    newPlayer.transform.position = roomUserInfoNotify.position;
                    newPlayer.transform.rotation = roomUserInfoNotify.rotation;
                }
                break;

            case E_PACKET.ROOM_LEAVE_USER_NTF:
                P_RoomLeaveUserNotify roomLeaveUserNotify = UnsafeCode.ByteArrayToStructure<P_RoomLeaveUserNotify>(packet.data);
                RemovePlayer(roomLeaveUserNotify.userUUID);
                break;

            case E_PACKET.UPDATE_PLAYER_MOVEMENT:
                P_UpdatePlayerMovement updateMovement = UnsafeCode.ByteArrayToStructure<P_UpdatePlayerMovement>(packet.data);
                if (Players.TryGetValue(updateMovement.player_id, out Player player) && player != null)
                {
                    player.Movement.Move(updateMovement.motion);
                }
                break;

            case E_PACKET.MOVE_PATH_RESPONSE:
                P_MovePathResponse movePath = UnsafeCode.ByteArrayToStructure<P_MovePathResponse>(packet.data);

                if (PathVisualizer.Instance != null)
                {
                    PathVisualizer.Instance.OnReceivePathPacket(movePath.path_count, movePath.path);
                    Debug.Log($"MOVE_PATH_RESPONSE pathCount={movePath.path_count}");
                    for (int i = 0; i < movePath.path_count; i++)
                    {
                        Debug.Log($"MOVE_PATH_RESPONSE path[{i}]=({movePath.path[i]})");
                    }
                }
                break;
            case E_PACKET.INVENTORY_INFO:
                break;
            case E_PACKET.SHOP_INFO:
                break;
            case E_PACKET.TRADE_REQUEST:
                break;
            case E_PACKET.TRADE_REQUEST_NTF:
                break;
            case E_PACKET.TRADE_RESPONSE:
                break;
            case E_PACKET.TRADE_START_NTF:
                break;
            case E_PACKET.TRADE_ITEM_UPDATE:
                break;
            case E_PACKET.TRADE_ITEM_NTF:
                break;
            case E_PACKET.TRADE_LOCK:
                break;
            case E_PACKET.TRADE_LOCK_NTF:
                break;
            case E_PACKET.TRADE_CONFIRM:
                break;
            case E_PACKET.TRADE_RESULT:
                break;

            default:
                break;

        }
    }

    private Player AddPlayer(long id, string playerName)
    {
        if (Players == null || Players.ContainsKey(id))
            return null;

        bool local = LocalPlayerInfo.ID == id;
        GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        playerObj.name = playerName;
        if (local)
        {
            GameObject cameraObject = new GameObject($"Player Camera");
            Camera playerCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<MouseLook>();
            playerCamera.transform.parent = playerObj.transform;

            // test code
            /*
            float randomX = Random.Range(-25, 26);
            float randomZ = Random.Range(-25, 26);
            Vector3 pos = new Vector3(randomX, 0, randomZ);
            playerObj.transform.position = pos;
            //*/
        }
        playerObj.transform.position.Set(5.0f, 2.0f, 5.0f);
        PlayerMovement playerMovement = playerObj.AddComponent<PlayerMovement>();
        playerMovement.Controller = playerObj.AddComponent<CharacterController>();
        Player player = playerObj.AddComponent<Player>();
        player.ID = id;
        player.Name = playerName;
        player.Movement = playerMovement;
        player.IsLocal = local;
        Players.Add(id, player);
        return player;
    }

    private void RemovePlayer(long id)
    {
        if (Players != null && Players.TryGetValue(id, out Player player) && player != null)
        {
            Destroy(player.gameObject);
            Players.Remove(id);
        }
    }
}
