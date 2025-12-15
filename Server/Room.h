#pragma once

#include "Npc.h"
#include "UserManager.h"
#include "Packet.h"
#include "NavMeshManager.h"

#include <functional>

void CopyUserID(char* userID, const Actor& user);
void CopyUserID(char* userID, const std::string& userID_);
void CopyUserID(char* userID, const char* userID_);

class Room 
{
public:
	Room() = default;
	~Room() = default;

	INT32 GetMaxUserCount() { return mMaxUserCount; }

	INT32 GetCurrentUserCount() { return mCurrentUserCount; }

	INT32 GetRoomNumber() { return mRoomNum; }


	void Init(const INT32 roomNum_, const INT32 maxUserCount_, const std::string& navMeshFileName)
	{
		mRoomNum = roomNum_;
		mMaxUserCount = maxUserCount_;
		InitNavMesh(navMeshFileName);
	}

	void InitNavMesh(const std::string& navMeshFileName)
	{
		if (navMeshManager.LoadNavMesh(navMeshFileName.c_str())) {
			std::cout << "NavMesh Loaded!" << std::endl;

			// Unity에서 보낸 좌표라고 가정
			Vector3 start = { 10.5f, 0.0f, -5.2f };
			Vector3 end = { 42.1f, 0.0f, 15.8f };

			std::vector<Vector3> path = navMeshManager.FindPath(start, end);

			std::cout << "Path Found: " << path.size() << " points." << std::endl;
			for (const auto& p : path) {
				std::cout << "(" << p.x << ", " << p.y << ", " << p.z << ")" << std::endl;
			}
		}
		else {
			std::cout << "Failed to load NavMesh." << std::endl;
		}
	}

	std::vector<Vector3> FindPath(const Vector3& start, const Vector3& end)
	{
		return navMeshManager.FindPath(start, end);
	}

	UINT16 EnterUser(User* user_)
	{
		if (mCurrentUserCount >= mMaxUserCount)
		{
			return (UINT16)ERROR_CODE::ENTER_ROOM_FULL_USER;
		}

		mUserList.push_back(user_);
		++mCurrentUserCount;

		user_->EnterRoom(mRoomNum);

		// 입장하는 유저에게, Zone 내 유저 수 만큼 정보 송신
		for (auto pRoomUser : mUserList)
		{
			if (pRoomUser == nullptr || pRoomUser == user_) {
				continue;
			}

			ROOM_USER_INFO_NTF_PACKET roomUserInfoNtf;
			roomUserInfoNtf.userUUID = pRoomUser->GetNetConnIdx();
			CopyUserID(roomUserInfoNtf.userID, *pRoomUser);
			roomUserInfoNtf.position = pRoomUser->GetPosition();
			roomUserInfoNtf.rotation = pRoomUser->GetRotation();
			SendPacketFunc(user_->GetNetConnIdx(), roomUserInfoNtf.PacketLength, (char*)&roomUserInfoNtf);
		}

		// 입장하는 유저에게, Zone 내 Npc 수 만큼 정보 송신
		for (auto pRoomNpc : mNpcList)
		{
			if (pRoomNpc == nullptr) {
				continue;
			}

			ROOM_USER_INFO_NTF_PACKET roomUserInfoNtf;
			roomUserInfoNtf.userUUID = pRoomNpc->GetNetConnIdx();
			CopyUserID(roomUserInfoNtf.userID, *pRoomNpc);
			roomUserInfoNtf.position = pRoomNpc->GetPosition();
			roomUserInfoNtf.rotation = pRoomNpc->GetRotation();
			SendPacketFunc(user_->GetNetConnIdx(), roomUserInfoNtf.PacketLength, (char*)&roomUserInfoNtf);
		}


		return (UINT16)ERROR_CODE::NONE;
	}

	Npc* CreateNpc()

	{

		INT32 uuid = 10000 + mNpcList.size();

		auto npmID = std::to_string(uuid);

		Npc* npc = new Npc();

		npc->Init(uuid);

		npc->SetLogin(npmID.c_str());



		mNpcList.push_back(npc);

		return npc;

	}

	UINT16 EnterNpc()
	{
		Npc* newNpc = CreateNpc();

		newNpc->EnterRoom(mRoomNum);

		NotifyUserEnter(newNpc->GetNetConnIdx(), newNpc->GetUserId());

		return (UINT16)ERROR_CODE::NONE;
	}

		
	void LeaveUser(User* leaveUser_)
	{
		mUserList.remove_if([leaveUserId = leaveUser_->GetUserId()](User* pUser) {
			return leaveUserId == pUser->GetUserId();
		});

		--mCurrentUserCount;

		ROOM_LEAVE_USER_NTF_PACKET notifyPkt;
		notifyPkt.userUUID = leaveUser_->GetNetConnIdx();
		CopyUserID(notifyPkt.userID, *leaveUser_);
		bool EXCEPT_ME = true; // 퇴장하는 유저에겐 보내지 않음
		SendToAllUser(notifyPkt.PacketLength, (char*)&notifyPkt, notifyPkt.userUUID, EXCEPT_ME);
	}
						
	void NotifyChat(INT32 clientIndex_, const char* userID_, const char* msg_)
	{
		ROOM_CHAT_NOTIFY_PACKET roomChatNtfyPkt;
		CopyMemory(roomChatNtfyPkt.Msg, msg_, sizeof(roomChatNtfyPkt.Msg));
		CopyUserID(roomChatNtfyPkt.userID, userID_);
		SendToAllUser(sizeof(roomChatNtfyPkt), (char*)&roomChatNtfyPkt, clientIndex_, false);
	}

	void NotifyUserEnter(INT32 clientIndex_, const std::string& userID)
	{
		ROOM_NEW_USER_NTF_PACKET roomNewUserNtfPkt;
		roomNewUserNtfPkt.userUUID = clientIndex_;
		CopyUserID(roomNewUserNtfPkt.userID, userID);
		bool EXCEPT_ME = false; // 입장하는 유저도 본 패킷이 로직에 필요함
		SendToAllUser(roomNewUserNtfPkt.PacketLength, (char*)&roomNewUserNtfPkt, clientIndex_, EXCEPT_ME);
	}

		
	std::function<void(UINT32, UINT32, char*)> SendPacketFunc;


	void SendToAllUser(const UINT16 dataSize_, char* data_, const INT32 passUserIndex_, bool exceptMe)
	{
		for (auto pUser : mUserList)
		{
			if (pUser == nullptr) {
				continue;
			}

			if (exceptMe && pUser->GetNetConnIdx() == passUserIndex_) {
				continue;
			}

			SendPacketFunc((UINT32)pUser->GetNetConnIdx(), (UINT32)dataSize_, data_);
		}
	}

private:
	NavMeshManager navMeshManager;

	INT32 mRoomNum = -1;

	std::list<User*> mUserList;
	std::list<Npc*> mNpcList;

		
	INT32 mMaxUserCount = 0;

	UINT16 mCurrentUserCount = 0;
};


void CopyUserID(char* userID, const Actor& user)
{
	CopyUserID(userID, user.GetUserId());
}

void CopyUserID(char* userID, const std::string& userID_)
{
	CopyMemory(userID, userID_.c_str(), sizeof(userID));
}

void CopyUserID(char* userID, const char* userID_)
{
	CopyMemory(userID, userID_, (MAX_USER_ID_LEN + 1));
}