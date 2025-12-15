#pragma once

#include "Packet.h"

#include <string>

class Actor
{
public:
	enum class DOMAIN_STATE 
	{
		NONE = 0,
		LOGIN = 1,
		ROOM = 2
	};


	Actor() = default;
	~Actor() = default;

	void Init(const INT32 index)
	{
		mIndex = index;
		position.x = 5.0f;
		position.y = 2.0f;
		position.z = 5.0f;
	}

	void Clear()
	{
		mRoomIndex = -1;
		mUserID = "";
		mIsConfirm = false;
		mCurDomainState = DOMAIN_STATE::NONE;
	}

	int SetLogin(const char* userID_)
	{
		mCurDomainState = DOMAIN_STATE::LOGIN;
		mUserID = userID_;

		return 0;
	}
		
	void EnterRoom(INT32 roomIndex_)
	{
		mRoomIndex = roomIndex_;
		mCurDomainState = DOMAIN_STATE::ROOM;
	}
		
	void SetDomainState(DOMAIN_STATE value_) { mCurDomainState = value_; }

	INT32 GetCurrentRoom() 
	{
		return mRoomIndex;
	}

	INT32 GetNetConnIdx() 
	{
		return mIndex;
	}

	std::string GetUserId() const
	{
		return  mUserID;
	}

	DOMAIN_STATE GetDomainState() 
	{
		return mCurDomainState;
	}


	const Vector3& GetPosition() const { return position;	}
	const Quaternion& GetRotation() const { return rotation; }

	Vector3 UpdateMovement(float dx, float dy, Quaternion& rotation_)
	{
		const float SPEED = 20.0f;

		dx *= (dx <= 1.0f);
		dy *= (dy <= 1.0f);

		// same as the client-sided calculation
		Vector3 right = Quaternion_Multiply(rotation_, Vector3_right());
		Vector3 forward = Quaternion_Multiply(rotation_, Vector3_forward());
		Vector3 mx = Vector3_Multiply(right, dx);
		Vector3 my = Vector3_Multiply(forward, dy);
		Vector3 motion = Vector3_Addition(mx, my);
		motion = Vector3_Multiply(motion, FIXED_DELTA_TIME * SPEED);

		
		position = Vector3_Addition(position, motion);
		rotation = rotation_;

		return motion;
	}

private:
	INT32 mIndex = -1;

	INT32 mRoomIndex = -1;

	std::string mUserID;
	bool mIsConfirm = false;
	std::string mAuthToken;
	
	DOMAIN_STATE mCurDomainState = DOMAIN_STATE::NONE;		

	// position of the player
	Vector3 position;

	// rotation of the player
	Quaternion rotation;
};

