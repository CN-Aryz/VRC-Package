
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class MouseSimulator : UdonSharpBehaviour
{
    [Header("电脑屏幕上的鼠标指针")]
    public RectTransform cursor;  // 电脑屏幕上显示的指针
    [Header("控制器移动范围")]
    public float controllerXMin = -0.1f;
    public float controllerXMax = 0.1f;
    public float controllerZMin = -0.1f;
    public float controllerZMax = 0.1f;

    [Header("电脑屏幕上鼠标指针的显示范围（单位为 UI 空间坐标）")]
    public Vector2 pointerScreenMin = new Vector2(-260f, -200f);
    public Vector2 pointerScreenMax = new Vector2(260f, 200f);


    [Header("判定“贴桌”允许的高度误差")]
    [Tooltip("Mouse 相对 MousePosition 的 localY 必须落在 ±threshold 内才认为贴在桌面上")]
    public float contactThreshold = 0.02f;

    [Header("判定有效移动的距离阈值")]
    public float moveThreshold = 0.001f;   // ≈1 mm（按场景比例自行调节）
    public float jitterThreshold = 0.001f;  // 过滤手抖

    [Header("鼠标点击音效")]
    public AudioSource audioSource;
    public AudioClip clickDownSound;
    public AudioClip clickUpSound;
    [Range(0f, 1f)]
    public float clickVolume = 0.5f;

    [Header("UI 图标层级，仅检测此 Layer 上的 Collider")]
    public LayerMask uiIconLayer;

    [Header("当本地 Y ≥ 该值时，旋转达到 100%")]
    [Tooltip("通常设为 1，如果想调整过渡高度可自行修改")]
    public float rotationFullHeight = 1f;

    // ---------- 私有状态 ----------
    private bool isHeld = false;
    private bool _isContacting = false;      // 当前是否贴桌
    private Vector3 _lastContactPos;                // 上一帧贴桌的 localPosition
    private Vector2 _currentPointerPos;             // 指针在 UI 上的累积坐标
    private float _rangeX, _rangeZ;
    private float _screenRangeX, _screenRangeY;

    private float lastReleaseTime = 0f;        // 上次松开的时刻
    private int clickCount = 0;              // 连续完整点击次数

    void Start()
    {
        if (cursor != null)
            _currentPointerPos = cursor.anchoredPosition;

        _rangeX = controllerXMax - controllerXMin;
        _rangeZ = controllerZMax - controllerZMin;
        _screenRangeX = pointerScreenMax.x - pointerScreenMin.x;
        _screenRangeY = pointerScreenMax.y - pointerScreenMin.y;
    }
    public override void OnPickup()
    {
        isHeld = true;
    }

    public override void OnDrop()
    {
        isHeld = false;
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (!isHeld) return;
        if (value)
        {
            PlayClickSound();
        }
        else
        {
            PlayReleaseSound();

            // —— 以下才是真正的一次“点击”事件 —— 
            float now = Time.time;
            // 如果与上次松开时间差在阈值内，累加；否则重置为 1
            if (now - lastReleaseTime <= 0.3f)
            {
                clickCount++;
            }
            else
            {
                clickCount = 1;
            }
            lastReleaseTime = now;

            // 双击判定：两次松开（完整点击）且时间间隔足够小
            if (clickCount == 2)
            {
                TryClickIcon();    // 执行双击打开
                clickCount = 0;  // 重置，避免三击也触发
            }
        }
    }
    void Update()
    {
        Vector3 localPos = transform.localPosition;
        localPos.y = Mathf.Max(0f, localPos.y);
        transform.localPosition = localPos;

        // —— 2. 计算插值因子 t ∈ [0,1] —— 
        // 当 y=0 → t=0；y>=rotationFullHeight → t=1；中间平滑过渡
        float t = rotationFullHeight > 0f
            ? Mathf.Clamp01(localPos.y / rotationFullHeight)
            : 1f;

        // —— 3. 捕获当前（外部驱动的）旋转 —— 
        //    LateUpdate 在所有 Update/物理/动画之后执行，
        //    这里 rawRotation 就是“你希望在 y 足够高时恢复的那个旋转”
        Quaternion rawRotation = transform.localRotation;

        // —— 4. 从零旋转（Quaternion.identity）到 rawRotation 做球面插值 —— 
        //    当 t=0 → identity（无旋转）；t=1 → rawRotation（完全保留）
        Quaternion blended = Quaternion.Slerp(Quaternion.identity, rawRotation, t);

        // —— 5. 应用插值后的旋转 —— 
        transform.localRotation = blended;
    }

    void FixedUpdate()
    {
        if (!isHeld) return;
        Vector3 localPos = transform.localPosition;
        bool isNowContacting = Mathf.Abs(localPos.y) <= contactThreshold;

        //------------------------------------------------------------
        // 1. 状态机：检测“贴桌”进入/退出
        //------------------------------------------------------------
        if (isNowContacting && !_isContacting)
        {
            // → 刚刚放到桌面：初始化锚点
            _lastContactPos = localPos;
            _isContacting = true;
            return;                    // 首帧不移动指针
        }
        else if (!isNowContacting && _isContacting)
        {
            // → 刚刚抬离桌面
            _isContacting = false;
            return;
        }

        if (!_isContacting) return;    // 空中移动时忽略

        //------------------------------------------------------------
        // 2. 计算相对位移（贴桌状态下）
        //------------------------------------------------------------
        Vector3 delta = localPos - _lastContactPos;

        // 手抖过滤
        if (delta.sqrMagnitude < jitterThreshold * jitterThreshold) return;

        // 更新锚点
        _lastContactPos = localPos;

        //------------------------------------------------------------
        // 3. 将 delta(XZ) 归一化后映射到屏幕增量
        //------------------------------------------------------------
        float normDX = delta.x / _rangeX;   // -1..1 → -1..1 ，占整幅比例
        float normDZ = delta.z / _rangeZ;

        Vector2 pointerDelta = new Vector2(
            normDX * _screenRangeX,
            normDZ * _screenRangeY
        );

        _currentPointerPos += pointerDelta;

        // 4. 限制指针不出屏幕
        _currentPointerPos.x = Mathf.Clamp(_currentPointerPos.x, pointerScreenMin.x, pointerScreenMax.x);
        _currentPointerPos.y = Mathf.Clamp(_currentPointerPos.y, pointerScreenMin.y, pointerScreenMax.y);

        cursor.anchoredPosition = _currentPointerPos;
    }

    // 播放点击按下音效
    private void PlayClickSound()
    {
        if (audioSource != null && clickDownSound != null)
        {
            audioSource.clip = clickDownSound;
            audioSource.volume = clickVolume;
            audioSource.Play();
        }
    }

    // 播放点击释放音效
    private void PlayReleaseSound()
    {
        if (audioSource != null && clickUpSound != null)
        {
            audioSource.clip = clickUpSound;
            audioSource.volume = clickVolume;
            audioSource.Play();
        }
    }

    private void TryClickIcon()
    {
        // 从鼠标指针世界位置沿其前方向发射射线，适合 VR 模式
        Vector3 origin = cursor.transform.position;
        Vector3 direction = cursor.transform.forward;
        Ray ray = new Ray(origin, direction);
        // —— 加入这一行即可在 Scene 视图里看到射线 —— 
        // Debug.DrawRay(origin, direction * 0.01f, Color.green, 2f);1

        if (Physics.Raycast(ray, out var hit, 0.01f, uiIconLayer))
        {
            var udonComp = hit.collider.gameObject.GetComponent<UdonSharpBehaviour>();
            if (udonComp != null)
            {
                udonComp.SendCustomEvent("OnIconClick");
            }
        }
    }
}
