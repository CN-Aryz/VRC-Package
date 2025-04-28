using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SyncCountdown : UdonSharpBehaviour
{
  [UdonSynced]
  private float endTime = 0f;

  [UdonSynced]
  private bool isCounting = false;

  [Header("UI References")]
  [SerializeField] private TextMeshProUGUI countdownText;

  [Header("Events")]
  [SerializeField] private UdonBehaviour onCountdownEnd;

  [Header("Settings")]
  [SerializeField] private float updateInterval = 0.1f; // UI更新间隔
  [SerializeField] private string displayFormat = "F0"; // 时间格式化字符串

  private float lastUpdateTime;
  private float cachedRemainingTime;

  public void StartCountdown(float duration)
  {
    if (duration <= 0f)
    {
      Debug.LogWarning("[SyncCountdown] Invalid duration value");
      return;
    }

    Debug.Log("[SyncCountdown] Start countdown: " + duration);

    TakeOwner();
    SetCountdownState((float)Networking.GetServerTimeInSeconds() + duration, true);
    UpdateUI(true); // 强制更新UI
  }

  private void Update()
  {
    if (!isCounting) return;

    float currentTime = Time.time;
    if (currentTime - lastUpdateTime >= updateInterval)
    {
      cachedRemainingTime = GetRemainingTime();
      UpdateUI(false);
      lastUpdateTime = currentTime;

      if (cachedRemainingTime <= 0f)
      {
        SetCountdownState(0f, false);
        OnCountdownEnd();
      }
    }
  }

  public float GetCurrentTime()
  {
    return isCounting ? cachedRemainingTime : GetRemainingTime();
  }

  public bool IsCounting()
  {
    return isCounting;
  }

  public override void OnDeserialization()
  {
    cachedRemainingTime = GetRemainingTime();
    UpdateUI(true);
  }

  private void UpdateUI(bool force)
  {
    if (countdownText == null) return;

    if (!isCounting)
    {
      countdownText.text = "0";
      return;
    }

    float timeToDisplay = force ? GetRemainingTime() : cachedRemainingTime;

    countdownText.text = timeToDisplay.ToString(displayFormat);
  }

  public void ResetCountdown()
  {
    TakeOwner();
    SetCountdownState(0f, false);
    cachedRemainingTime = 0f;
    UpdateUI(true);
  }

  private void OnCountdownEnd()
  {
    if (onCountdownEnd != null)
    {
      onCountdownEnd.SendCustomEvent("OnCountdownEnd");
    }
  }

  private void TakeOwner()
  {
    if (!Networking.IsOwner(gameObject))
    {
      Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }
  }

  private void SetCountdownState(float endTime, bool counting)
  {
    this.endTime = endTime;
    isCounting = counting;
    RequestSerialization();
  }

  private float GetRemainingTime()
  {
    return Mathf.Max(0f, endTime - (float)Networking.GetServerTimeInSeconds());
  }
}