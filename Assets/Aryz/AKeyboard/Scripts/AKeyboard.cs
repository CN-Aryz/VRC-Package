
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class AKeyboard : UdonSharpBehaviour
{
    [SerializeField]
    private Text
    _stringHolder;

    [SerializeField]
    private TextMeshProUGUI
    showText;

    [SerializeField]
    private GameObject
    uiPanel; // 引用UI面板

    [SerializeField]
    private float
    uiDistance = 0.5f; // UI面板与玩家头部的距离，可在Unity编辑器中配置


    [SerializeField]
    private AudioClip keyPressSound; // 普通按键音效

    [SerializeField]
    private AudioClip enterSound; // Enter 键音效

    [SerializeField]
    private AudioSource audioSource; // 用于播放音效的 AudioSource

    public float
    hideDistance = 1f;

    [SerializeField]
    private UdonSharpBehaviour[]
    _targets;

    [NonSerialized]
    public AKeyboard
        keyboard;

    [SerializeField]
    private int
    characterLimit = 10;

    public string
    text = string.Empty;

    private bool hasCheckedAxis = false; // 新增：用于跟踪是否已经检查过轴

    [HideInInspector]
    public readonly string
    appname = nameof(AKeyboard);

    [SerializeField]
    private Color
    C_APP = new Color(0xf2, 0x7d, 0x4a, 0xff) / 0xff,
    C_LOG = new Color(0x00, 0x8b, 0xca, 0xff) / 0xff,
    C_WAR = new Color(0xfe, 0xeb, 0x5b, 0xff) / 0xff,
    C_ERR = new Color(0xe0, 0x30, 0x5a, 0xff) / 0xff;

    private readonly string
    CTagEnd = "</color>";

    void Start()
    {
        keyboard = this;
        if (uiPanel != null)
        {
            uiPanel.SetActive(false); // 初始时隐藏UI面板
        }
    }


    void Update()
    {
        // 检查当前玩家是否按下了T键
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleUIPanel(); // 显示UI面板
        }

        // 检查 Oculus_CrossPlatform_SecondaryThumbstickVertical 是否为 -1
        if (!hasCheckedAxis && Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical") == -1)
        {
            ToggleUIPanel(); // 显示UI面板
            hasCheckedAxis = true; // 标记为已检查
        }
        else if (hasCheckedAxis && Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical") != -1)
        {
            hasCheckedAxis = false; // 重置标记
        }

        // 如果UI面板是激活状态，检查玩家与UI面板的距离
        if (uiPanel != null && uiPanel.activeSelf)
        {
            CheckPlayerDistance();
        }
    }


    private void CheckPlayerDistance()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            P_ERR("Local player is null.");
            return;
        }

        var headTrackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 headPosition = headTrackingData.position; // 头部位置
        // 计算玩家与UI面板之间的距离
        float distance = Vector3.Distance(headPosition, uiPanel.transform.position);

        // 如果距离超过3米，隐藏UI面板
        if (distance > hideDistance)
        {
            uiPanel.SetActive(false);
        }
    }

    private void ToggleUIPanel()
    {
        if (uiPanel == null)
        {
            P_ERR("UI Panel is not assigned.");
            return;
        }

        // 获取玩家的头部追踪数据
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            P_ERR("Local player is null.");
            return;
        }

        // 如果UI面板已经激活，则关闭它
        if (uiPanel.activeSelf)
        {
            uiPanel.SetActive(false);
            P("UI Panel closed.");
            return;
        }

        P("UI Panel opened.");

        // 获取玩家头部的位置和旋转
        var headTrackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 headPosition = headTrackingData.position; // 头部位置
        Quaternion headRotation = headTrackingData.rotation; // 头部旋转

        // 计算头部的视角方向（包括俯仰角）
        Vector3 forwardDirection = headRotation * Vector3.forward;
        Vector3 upDirection = headRotation * Vector3.up;

        // 将UI面板放置在玩家头部前方2米处
        uiPanel.transform.position = headPosition + forwardDirection * uiDistance;

        // 确保UI面板始终面向玩家头部
        uiPanel.transform.rotation = Quaternion.LookRotation(forwardDirection, upDirection);

        // 激活UI面板
        uiPanel.SetActive(true);
    }

    public void OnEndEdit()
    {
        foreach (var target in _targets)
        {
            if (target)
            {
                target.SetProgramVariable(nameof(keyboard), keyboard);
                target.SendCustomEvent(nameof(OnEndEdit));
            }
            else
            {
                P_ERR("Target Udon Sharp Behaviour is null.");
            }
        }
        text = string.Empty;
        UpdateShowText();
    }

    public void UpdateShowText()
    {
        if (showText == null)
        {
            return;
        }
        showText.text = text;
    }

    public void OnBackSpace()
    {
        if (text.Length > 0)
        {
            text = text.Substring(0, text.Length - 1);
        }
        UpdateShowText();
    }

    public void OnKeyPress()
    {
        var input = _stringHolder.text;
        _stringHolder.text = string.Empty;

        switch (input)
        {
            case "Enter":
                PlaySound(enterSound);
                OnEndEdit();
                break;
            case "BackSpace":
                PlaySound(keyPressSound);
                OnBackSpace();
                break;
            case "Empty":
                PlaySound(keyPressSound);
                text = string.Empty;
                UpdateShowText();
                break;
            default:
                PlaySound(keyPressSound);
                if (text.Length < characterLimit)
                {
                    text += input;
                    UpdateShowText();
                }
                break;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void P(object o)
    {
        Debug.Log($"[{CTag(C_APP)}{appname}{CTagEnd}] {CTag(C_LOG)}{o}{CTagEnd}", this);
    }

    private void P_ERR(object o)
    {
        Debug.LogError($"[{CTag(C_APP)}{appname}{CTagEnd}] {CTag(C_ERR)}{o}{CTagEnd}", this);
    }

    private void P_LOG(object o)
    {
        Debug.Log($"[{CTag(C_APP)}{appname}{CTagEnd}] {CTag(C_LOG)}{o}{CTagEnd}", this);
    }

    private void P_WAR(object o)
    {
        Debug.LogWarning($"[{CTag(C_APP)}{appname}{CTagEnd}] {CTag(C_WAR)}{o}{CTagEnd}", this);
    }

    private string CTag(Color c)
    {
        return $"<color=\"#{ToHtmlStringRGB(c)}\">";
    }
    private string ToHtmlStringRGB(Color c)
    {
        c *= 0xff;
        return $"{Mathf.RoundToInt(c.r):x2}{Mathf.RoundToInt(c.g):x2}{Mathf.RoundToInt(c.b):x2}";
    }

}
