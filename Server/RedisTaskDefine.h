#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "ErrorCode.h"

#include "unity.h"

enum class RedisTaskID : UINT16
{
	INVALID = 0,

	REQUEST_LOGIN = 1001,
	RESPONSE_LOGIN = 1002,
	REQUEST_NOTICE = 1003,
	RESPONSE_NOTICE = 1004,

	//인벤
	REQUEST_LOAD_INVENTORY = 11001,
	RESPONSE_LOAD_INVENTORY = 11002,

	//상점
	REQUEST_SHOP_UPDATE = 12001,
	RESPONSE_SHOP_UPDATE = 12002,

	//거래
	REQUEST_TRADE_EXCHANGE = 13001,
	RESPONSE_TRADE_EXCHANGE = 13002,
	
};



struct RedisTask
{
	UINT32 UserIndex = 0;
	RedisTaskID TaskID = RedisTaskID::INVALID;
	UINT16 DataSize = 0;
	char* pData = nullptr;	

	void Release()
	{
		if (pData != nullptr)
		{
			delete[] pData;
		}
	}
};




#pragma pack(push,1)

struct RedisLoginReq
{
	char UserID[MAX_USER_ID_LEN + 1];
	char UserPW[MAX_USER_PW_LEN + 1];
};

struct RedisLoginRes
{
	char UserID[MAX_USER_ID_LEN + 1];
	UINT16 Result = (UINT16)ERROR_CODE::NONE;
};

struct RedisNoticeReq
{
	char UserID[MAX_USER_ID_LEN + 1];
	char Message[MAX_CHAT_MSG_SIZE + 1];
};

struct RedisNoticeRes
{
	char UserID[MAX_USER_ID_LEN + 1];
	char Message[MAX_CHAT_MSG_SIZE + 1];
};



//인벤토리 결과물
struct RedisInvenReq
{
	int UserIndex;
	int UserID[MAX_USER_ID_LEN + 1];
};

struct RedisInvenRes
{
	int UserIndex;
	int ItemSlots[INVENTORY_SIZE];
};

struct RedisTradeReq
{
	int UserA, UserB;				//유저 인덱스 정보
	int CountA, CountB;				//각자 몇 개 보내는지
	
	//A 거래 데이터
	int ItemsASlots[INVENTORY_SIZE];
	int ItemsAIDs[INVENTORY_SIZE];
	
	//B 거래 데이터
	int ItemsBSlots[INVENTORY_SIZE];
	int ITemsBIDs[INVENTORY_SIZE];
};
#pragma pack(pop) //위에 설정된 패킹설정이 사라짐