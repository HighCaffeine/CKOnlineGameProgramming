using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    public static PathVisualizer Instance;

    void Awake()
    {
        Instance = this;
    }

    // 서버 규격과 유사한 패킷 데이터 구조 (시뮬레이션 용)
    // 실제로는 네트워크 라이브러리의 수신 버퍼에서 파싱한 값을 사용하시면 됩니다.
    private Vector3[] receivedPath = new Vector3[10];
    private int receivedPathCount = 0;

    // 기즈모 색상 설정
    public Color pathColor = Color.green;
    public Color waypointColor = Color.red;
    public bool showOnlyWhenSelected = false;

    /// <summary>
    /// 네트워크 모듈에서 패킷을 받으면 이 함수를 호출하여 데이터를 갱신합니다.
    /// </summary>
    /// <param name="count">유효한 경로 점의 개수</param>
    /// <param name="pathArray">서버로부터 받은 좌표 배열</param>
    public void OnReceivePathPacket(int count, P_PacketVector3[] pathArray)
    {
        // 1. 데이터 유효성 검사
        if (count > 10) count = 10; // 서버 스펙(Max 10)에 따른 안전장치

        // 2. 데이터 복사 (네트워크 버퍼가 재사용될 수 있으므로 값 복사 권장)
        receivedPathCount = count;
        for (int i = 0; i < count; i++)
        {
            receivedPath[i].Set(pathArray[i].x, pathArray[i].y, pathArray[i].z);
        }
    }

    /// <summary>
    /// 에디터 씬 뷰에 그림을 그리는 유니티 콜백 함수입니다.
    /// </summary>
    void OnDrawGizmos()
    {
        if (showOnlyWhenSelected) return;
        DrawPath();
    }

    // 오브젝트가 선택되었을 때만 그리고 싶다면 이 함수 사용
    void OnDrawGizmosSelected()
    {
        if (showOnlyWhenSelected)
            DrawPath();
    }

    void DrawPath()
    {
        // 경로가 없거나 점이 2개 미만이면 그릴 수 없음
        if (receivedPathCount < 2) return;

        // 1. 경로 선 그리기
        Gizmos.color = pathColor;
        for (int i = 0; i < receivedPathCount - 1; i++)
        {
            Vector3 start = receivedPath[i];
            Vector3 end = receivedPath[i + 1];

            // (옵션) 바닥에 묻히지 않도록 살짝 띄워서 그리기
            // start.y += 0.1f; 
            // end.y += 0.1f;

            Gizmos.DrawLine(start, end);
        }

        // 2. 웨이포인트(꺾이는 점) 표시
        Gizmos.color = waypointColor;
        for (int i = 0; i < receivedPathCount; i++)
        {
            Gizmos.DrawSphere(receivedPath[i], 0.3f); // 반지름 0.3의 구
        }
    }

    // ---------------------------------------------------------
    // [테스트용] 에디터에서 우클릭으로 가상의 경로 데이터를 넣어보는 기능
    // ---------------------------------------------------------
    [ContextMenu("Test Simulate Receive Path")]
    public void TestReceive()
    {
        P_PacketVector3[] dummyPath = new P_PacketVector3[10];

        // 임의의 경로 데이터 생성 (현재 위치 기준)
        var pos = this.transform.position;
        dummyPath[0].Set(pos);
        dummyPath[1].Set(this.transform.position + new Vector3(20, 0, 20));
        dummyPath[2].Set(this.transform.position + new Vector3(50, 0, 50));
        dummyPath[3].Set(this.transform.position + new Vector3(80, 0, 20));

        // 데이터 수신 시뮬레이션 (점 4개)
        OnReceivePathPacket(4, dummyPath);

        Debug.Log("Simulated Path Received!");
    }
}
