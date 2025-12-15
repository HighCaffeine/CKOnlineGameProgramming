#pragma once

#include <iostream>
#include <vector>
#include <fstream>
#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"
#include "DetourCommon.h"
#include "unity.h"

// -----------------------------------------------------------
// RecastDemo 파일 포맷에 맞춘 헤더 구조체 (필수)
// -----------------------------------------------------------
struct NavMeshSetHeader
{
    int magic;
    int version;
    int numTiles;
    dtNavMeshParams params;
};

struct NavMeshTileHeader
{
    dtTileRef tileRef;
    int dataSize;
};

class NavMeshManager {
private:
    dtNavMesh* m_navMesh = nullptr;
    dtNavMeshQuery* m_navQuery = nullptr;
    dtQueryFilter m_filter; // 이동 비용 등 설정

public:
    NavMeshManager() {}
    ~NavMeshManager() {
        dtFreeNavMeshQuery(m_navQuery);
        dtFreeNavMesh(m_navMesh);
    }

    // 1. NavMesh 데이터 파일 로드 (.bin 파일)
    bool LoadNavMesh(const char* path) {
        FILE* fp = nullptr;
        fopen_s(&fp, path, "rb"); // fopen_s 사용
        if (!fp) {
            std::cout << "File not found: " << path << std::endl;
            return false;
        }

        NavMeshSetHeader header;
        if (fread(&header, sizeof(NavMeshSetHeader), 1, fp) != 1) {
            fclose(fp);
            return false;
        }

        // 매직 넘버 체크 ('MSET')
        if (header.magic != ((int)'M' << 24 | (int)'S' << 16 | (int)'E' << 8 | (int)'T')) {
            std::cout << "Invalid Magic Number" << std::endl;
            // 디버깅용: 실제 들어있는 값 확인
            std::cout << "Expected: " << ((int)'M' << 24 | (int)'S' << 16 | (int)'E' << 8 | (int)'T') << std::endl;
            std::cout << "Actual: " << header.magic << std::endl;

            fclose(fp);
            return false;
        }

        m_navMesh = dtAllocNavMesh();
        if (!m_navMesh || dtStatusFailed(m_navMesh->init(&header.params))) {
            fclose(fp);
            return false;
        }

        // 타일 데이터 읽기
        for (int i = 0; i < header.numTiles; ++i) {
            NavMeshTileHeader tileHeader;
            fread(&tileHeader, sizeof(NavMeshTileHeader), 1, fp);

            if (!tileHeader.tileRef || !tileHeader.dataSize) break;

            unsigned char* data = (unsigned char*)dtAlloc(tileHeader.dataSize, DT_ALLOC_PERM);
            fread(data, tileHeader.dataSize, 1, fp);

            // 중요: DT_TILE_FREE_DATA 옵션으로 메모리 관리 위임
            m_navMesh->addTile(data, tileHeader.dataSize, DT_TILE_FREE_DATA, tileHeader.tileRef, 0);
        }
        fclose(fp);

        // 쿼리 객체 생성
        m_navQuery = dtAllocNavMeshQuery();
        if (dtStatusFailed(m_navQuery->init(m_navMesh, 2048))) {
            return false;
        }

        return true;
    }

    // 3. 경로 찾기 (Start -> End)
    std::vector<Vector3> FindPath(Vector3 startPos, Vector3 endPos) {
        std::vector<Vector3> pathPoints;
        if (!m_navQuery) return pathPoints;

        float startPt[3] = { startPos.x, startPos.y, startPos.z };
        float endPt[3] = { endPos.x, endPos.y, endPos.z };
        float polyPickExt[3] = { 2.0f, 4.0f, 2.0f };

        dtPolyRef startRef, endRef;
        float startPtOnPoly[3], endPtOnPoly[3];

        // 1. 시작점/끝점과 가장 가까운 폴리곤 찾기
        m_navQuery->findNearestPoly(startPt, polyPickExt, &m_filter, &startRef, startPtOnPoly);
        m_navQuery->findNearestPoly(endPt, polyPickExt, &m_filter, &endRef, endPtOnPoly);

        if (!startRef || !endRef) return pathPoints;

        // 2. 경로 폴리곤 탐색
        dtPolyRef pathPolys[256];
        int pathCount = 0;
        m_navQuery->findPath(startRef, endRef, startPtOnPoly, endPtOnPoly, &m_filter, pathPolys, &pathCount, 256);

        // 3. 직선 경로 추출 (Straight Path)
        float straightPath[256 * 3];
        unsigned char straightPathFlags[256];
        dtPolyRef straightPathRefs[256];
        int straightPathCount = 0;

        m_navQuery->findStraightPath(startPtOnPoly, endPtOnPoly, pathPolys, pathCount,
            straightPath, straightPathFlags, straightPathRefs,
            &straightPathCount, 256);

        for (int i = 0; i < straightPathCount; ++i) {
            pathPoints.push_back({ straightPath[i * 3], straightPath[i * 3 + 1], straightPath[i * 3 + 2] });
        }

        return pathPoints;
    }
};