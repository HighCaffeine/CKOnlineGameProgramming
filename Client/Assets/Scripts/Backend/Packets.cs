using System.Runtime.InteropServices;
using UnityEngine;

public enum E_PACKET
{
    LOGIN_REQUEST = 201, // LOGIN_REQ = 201,
    LOGIN_RESPONSE = 202, // LOGIN_RES = 202,

    // Enter
    ROOM_ENTER_REQUEST = 206, // PLAYER_JOINED
    ROOM_ENTER_RESPONSE = 207,
    ROOM_NEW_USER_NTF = 208, // PLAYER_JOINED (일단 자기 자신에도 전송하고 사용된다)
    ROOM_USER_INFO_NTF = 209, // CREATE_MATCH_PLAYER,Zone 안에 있는 유저 정보

    // Leave
    ROOM_LEAVE_REQUEST = 215,
    ROOM_LEAVE_RESPONSE = 216,
    ROOM_LEAVE_USER_NTF = 217, // PLAYER_LEFT


    // Move
    PLAYER_MOVEMENT,
    UPDATE_PLAYER_MOVEMENT,

    // Chat
    ROOM_CHAT_REQUEST = 221, // SEND_CHAT_MESSAGE
    ROOM_CHAT_RESPONSE = 222,
    ROOM_CHAT_NOTIFY = 223, // RECEIVE_CHAT_MESSAGE

    // Path
    MOVE_PATH_REQUEST = 225,
    MOVE_PATH_RESPONSE = 226,
    MOVE_PATH_NOTIFY = 227,
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
struct P_PlayerName
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string userName;
}

[StructLayout(LayoutKind.Sequential, Size = 66)]
struct P_LoginReq
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userPW;
}


[StructLayout(LayoutKind.Sequential, Size = 2)]
struct P_LoginRes
{
    // UInt16 Result;
    [MarshalAs(UnmanagedType.U2)]
    public ushort result;

}

[StructLayout(LayoutKind.Sequential, Size = 24)]
struct P_PlayerJoined
{
    [MarshalAs(UnmanagedType.I8)]
    public long id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string userName;
}

[StructLayout(LayoutKind.Sequential, Size = 4)]
struct P_RoomEnterRequest
{
    [MarshalAs(UnmanagedType.I4)]
    public int roomNumber;
}

[StructLayout(LayoutKind.Sequential, Size = 2)]
struct P_RoomEnterResponse
{
    [MarshalAs(UnmanagedType.I2)]
    public short result;
}

[StructLayout(LayoutKind.Sequential, Size = 41)]
struct P_RoomNewUserNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userName;
}

[StructLayout(LayoutKind.Sequential, Size = 56)]
struct P_CreateMatchPlayer
{
    [MarshalAs(UnmanagedType.I8)]
    public long id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string userName;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 position;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Size = 69)]
struct P_RoomUserInfoNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userName;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 position;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Size = 41)]
struct P_RoomLeaveUserNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userName;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct P_PlayerMovement
{
    [MarshalAs(UnmanagedType.I8)]
    public long player_id;

    [MarshalAs(UnmanagedType.R4)]
    public float dx;

    [MarshalAs(UnmanagedType.R4)]
    public float dy;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Size = 36)]
struct P_UpdatePlayerMovement
{
    [MarshalAs(UnmanagedType.I8)]
    public long player_id;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 motion;

}


[StructLayout(LayoutKind.Sequential, Size = 257)]
struct P_RoomChatRequest
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
    public string message;
}

[StructLayout(LayoutKind.Sequential, Size = 290)]
struct P_RoomChatNotify
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
    public string message;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct P_MovePathRequest
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 startPos;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 endPos;
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct P_PacketVector3
{
    [MarshalAs(UnmanagedType.R4)]
    public float x;

    [MarshalAs(UnmanagedType.R4)]
    public float y;

    [MarshalAs(UnmanagedType.R4)]
    public float z;

    public void Set(Vector3 v)
    {
        x = v.x; y = v.y; z = v.z;
    }
}

[StructLayout(LayoutKind.Sequential, Size = 130)]
struct P_MovePathResponse
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public P_PacketVector3[] path;

    [MarshalAs(UnmanagedType.I2)]
    public short path_count;
}