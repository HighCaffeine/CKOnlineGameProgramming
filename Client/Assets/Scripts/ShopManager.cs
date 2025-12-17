using UnityEngine;
using System;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    [Header("Shop UI")]
    public TMPro.TextMeshProUGUI shopItemNameText;  // 현재 판매 아이템 이름/ID
    public TMPro.TextMeshProUGUI shopTimerText;     // 남은 시간 (00:00:00)
    private long targetShopTime;   // 서버에서 받은 갱신 시간 (Unix Timestamp)

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (targetShopTime > 0)
        {
            // 현재 유닉스 시간
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remain = targetShopTime - now;

            if (remain > 0)
            {
                // 시간을 시:분:초 형식으로 변환
                TimeSpan t = TimeSpan.FromSeconds(remain);
                if (shopTimerText)
                {
                    shopTimerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
                }
            }
            else
            {
                if (shopTimerText) shopTimerText.text = "00:00:00";
            }
        }
    }

    public void SetTargetShopTime(long targetShopTime, int itemID)
    {
        this.targetShopTime = targetShopTime;
        if (shopItemNameText) shopItemNameText.text = "Item ID: " + itemID;
    }
}