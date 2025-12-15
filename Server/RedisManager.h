#pragma once

#include "RedisTaskDefine.h"
//#include "ErrorCode.h"

//#include "../thirdparty/CRedisConn.h"
#include "CRedisConnEx.h"
#include <vector>
#include <deque>
#include <thread>
#include <mutex>
#include <random>
#include <iostream>
#include <algorithm>

class RedisManager
{
public:
	RedisManager() = default;
	~RedisManager() = default;

	bool Run(std::string ip_, UINT16 port_, const UINT32 threadCount_)
	{
		if (Connect(ip_, port_) == false)
		{
			printf("RedisManager::Run() Redis 접속 실패\n");
			return false;
		}

		mIsTaskRun = true;

		for (UINT32 i = 0; i < threadCount_; i++)
		{
			mTaskThreads.emplace_back([this]() { TaskProcessThread(); });
		}

		// Redis Sub 용 Thread
		mTaskThreads.emplace_back([this]() { SubscribeThread(); });

		printf("RedisManager::Run() Redis 동작 중...\n");
		return true;
	}

	void End()
	{
		mIsTaskRun = false;

		mConnSub.disConnect();

		for (auto& thread : mTaskThreads)
		{
			if (thread.joinable())
			{
				thread.join();
			}
		}
	}

	void PushTask(RedisTask task_)
	{
		std::lock_guard<std::mutex> guard(mReqLock);
		mRequestTask.push_back(task_);
	}

	RedisTask TakeResponseTask()
	{
		std::lock_guard<std::mutex> guard(mResLock);

		if (mResponseTask.empty())
		{
			return RedisTask();
		}

		auto task = mResponseTask.front();
		mResponseTask.pop_front();

		return task;
	}


private:
	bool Connect(std::string ip_, UINT16 port_)
	{
		if (mConn.connect(ip_, port_) == false)
		{
			std::cout << "RedisManager::Connect() Redis connect error " << mConn.getErrorStr() << std::endl;
			return false;
		}
		else
		{
			std::cout << "RedisManager::Connect() Redis connect success !!!" << std::endl;
		}

		if (mConnSub.connect(ip_, port_) == false)
		{
			std::cout << "RedisManager::Connect() Redis(Sub) connect error " << mConn.getErrorStr() << std::endl;
			return false;
		}
		else
		{
			std::cout << "RedisManager::Connect() Redis(Sub) connect success !!!" << std::endl;
		}

		return true;
	}

	void TaskProcessThread()
	{
		printf("RedisManager::TaskProcessThread() Redis 스레드 시작...\n");

		while (mIsTaskRun)
		{
			bool isIdle = true;

			if (auto task = TakeRequestTask(); task.TaskID != RedisTaskID::INVALID)
			{
				isIdle = false;

				if (task.TaskID == RedisTaskID::REQUEST_LOGIN)
				{
					auto pRequest = (RedisLoginReq*)task.pData;
					
					RedisLoginRes bodyData;
					bodyData.Result = (UINT16)ERROR_CODE::LOGIN_USER_INVALID_PW;

					std::string value;
					if (mConn.get(pRequest->UserID, value))
					{
						bodyData.Result = (UINT16)ERROR_CODE::NONE;

						if (value.compare(pRequest->UserPW) == 0)
						{
							bodyData.Result = (UINT16)ERROR_CODE::NONE;
							CopyUserID(bodyData.UserID, pRequest->UserID);
						}
					}
					
					RedisTask resTask;
					resTask.UserIndex = task.UserIndex;
					resTask.TaskID = RedisTaskID::RESPONSE_LOGIN;
					resTask.DataSize = sizeof(RedisLoginRes);
					resTask.pData = new char[resTask.DataSize];
					CopyMemory(resTask.pData, (char*)&bodyData, resTask.DataSize);

					PushResponse(resTask);
				}
				else if (task.TaskID == RedisTaskID::REQUEST_NOTICE)
				{
					auto pRequest = (RedisNoticeReq*)task.pData;

					mConn.publish("ch_notice", pRequest->Message);
					
				}
				else if (task.TaskID == RedisTaskID::REQUEST_LOAD_INVENTORY)
				{
					auto pRequest = (RedisInvenReq*)task.pData;
					RedisInvenRes resData;

					resData.UserIndex = pRequest->UserIndex;
					memset(resData.ItemSlots, 0, sizeof(resData.ItemSlots));

					//레디스 해쉬 키값
					std::string id = "u:" + std::string(pRequest->UserID) + ":inven";
					std::map<std::string, std::string> inven;

					//getall로 가져오고, 안에 안비었으면 내부 처리
					if (mConn.hgetall(id, inven) && !inven.empty())
					{	
						//페어로 값 가져옴
						//내부에 0 100 1 200 2 300 같이 저장할거 (키를 인덱스로 바로 쓸 수 있도록)
						for (auto const& [key, value] : inven)
						{
							int index = std::stoi(key);
							if (index >= 0 && index < INVENTORY_SIZE)
							{
								//아이템 하나씩 세팅 (빈칸은 0)
								resData.ItemSlots[index] = std::stoi(value);
							}
						}

					}
					else //인벤이 없다면 (신규 유저라면)
					{
						//랜덤 아이템 ID 값 가져옴
						int item1 = 101 + (rand() % 5);
						int item2 = 101 + (rand() % 5);
						uint32_t ret;

						//랜덤 인덱스 얻기
						static std::random_device rd;
						static std::mt19937 gen(rd());
						int nums[] = {0, 1, 2, 3, 4};
						int first, firstIndex;
						int sec, secIndex;
						
						//랜덤 인덱스 1
						std::uniform_int_distribution<int> dis1(0, 4); firstIndex = dis1(gen);
						first = nums[first];

						//맨 뒤랑 교체
						std::swap(nums[firstIndex], nums[4]);

						//랜덤 인덱스 2
						std::uniform_int_distribution<int> dis2(0, 3); secIndex = dis2(gen);
						sec = nums[secIndex];

						//다 0으로 세팅
						for (int i = 0; i < INVENTORY_SIZE; i++)
						{
							mConn.hset(id, std::to_string(i), "0", ret);
						}

						mConn.hset(id, std::to_string(first), std::to_string(item1), ret);
						mConn.hset(id, std::to_string(sec), std::to_string(item2), ret);

						resData.ItemSlots[first] = item1;
						resData.ItemSlots[sec] = item2;
					}
				
					//결과 반환해줌
					RedisTask resTask;
					resTask.TaskID = RedisTaskID::RESPONSE_LOAD_INVENTORY;
					resTask.UserIndex = task.UserIndex;
					resTask.DataSize = sizeof(RedisInvenRes);
					resTask.pData = new char[resTask.DataSize];
					memcpy(resTask.pData, &resData, resTask.DataSize);

					PushResponse(resTask);
				}
				else if (task.TaskID == RedisTaskID::REQUEST_TRADE_EXCHANGE)
				{
					auto pRequest = (RedisTradeReq*)task.pData;
					RedisTradeRes resData;


					RedisTask resTask;
					resTask.TaskID = RedisTaskID::RESPONSE_TRADE_EXCHANGE;
					resTask.UserIndex = task.UserIndex;
					resTask.DataSize = sizeof(RedisTradeRes);
					resTask.pData = new char[resTask.DataSize];
					memcpy(resTask.pData, &resData, resTask.DataSize);

					PushResponse(resTask);
				}
				else if (task.TaskID == RedisTaskID::REQUEST_SHOP_UPDATE)
				{
					auto pRequest = (RedisShopReq*)task.pData;
					RedisShopRes resData;

					RedisTask resTask;
					resTask.TaskID = RedisTaskID::RESPONSE_SHOP_UPDATE;
					resTask.UserIndex = task.UserIndex;
					resTask.DataSize = sizeof(RedisShopRes);
					resTask.pData = new char[resTask.DataSize];
					memcpy(resTask.pData, &resData, resTask.DataSize);

					PushResponse(resTask);
				}

				task.Release();
			}

	
			if (isIdle)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(1));
			}
		}

		printf("Redis 스레드 종료\n");
	}

	void SubscribeThread()
	{
		printf("RedisManager::SubscribeThread() Redis(Sub) 스레드 시작...\n");

		auto result = mConnSub.initSubscribe("ch_notice");

		while (mIsTaskRun)
		{
			std::string message; /* output */
			mConnSub.subscribe(message);

			RedisNoticeRes bodyData;
			CopyUserID(bodyData.UserID, "[GM]");
			CopyMemory(bodyData.Message, message.c_str(), sizeof(bodyData.Message));

			RedisTask resTask;
			resTask.UserIndex = 0; // to all users
			resTask.TaskID = RedisTaskID::RESPONSE_NOTICE;
			resTask.DataSize = sizeof(RedisNoticeRes);
			resTask.pData = new char[resTask.DataSize];
			CopyMemory(resTask.pData, (char*)&bodyData, resTask.DataSize);

			PushResponse(resTask);
		}
	}

	RedisTask TakeRequestTask()
	{
		std::lock_guard<std::mutex> guard(mReqLock);

		if (mRequestTask.empty())
		{
			return RedisTask();
		}

		auto task = mRequestTask.front();
		mRequestTask.pop_front();

		return task;
	}

	void PushResponse(RedisTask task_)
	{
		std::lock_guard<std::mutex> guard(mResLock);
		mResponseTask.push_back(task_);
	}




	private:

	RedisCpp::CRedisConnEx mConn;
	RedisCpp::CRedisConnEx mConnSub; // Redis Subscribe용

	bool		mIsTaskRun = false;
	std::vector<std::thread> mTaskThreads;

	std::mutex mReqLock;
	std::deque<RedisTask> mRequestTask;

	std::mutex mResLock;
	std::deque<RedisTask> mResponseTask;
};