
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PhotoCapture : UdonSharpBehaviour
{
    public Camera photoCamera; // 拍照用的相机
    public Camera sideCamera; // 拍照用的相机

    public GameObject Book; // 用于显示照片的GameObject
    public string CoverMaterialName = "Cover"; // 要替换的材质名称，默认为"Cover"
    public string SideMaterialName = "Side"; // 要替换的材质名称，默认为"Cover"

    public TextMeshProUGUI titleText; // 显示的标题

    private RenderTexture CoverRenderTexture; // 渲染纹理
    private RenderTexture SideRenderTexture; // 渲染纹理
    private Material[] originalMaterials; // 存储原始材质备份

    public string[] languageKeys = { "zh", "en", "ja" };
    public string[] happyTexts = {
        "如何讨 {0} 开心",
        "How to make {0} happy",
        "{0}を喜ばせる方法"
    };

    void Start()
    {
        // 创建一个RenderTexture
        CoverRenderTexture = new RenderTexture(731 * 2, 983 * 2, 24);
        SideRenderTexture = new RenderTexture(760 * 2, 108 * 2, 24);
        CoverRenderTexture.format = RenderTextureFormat.ARGB32;
        SideRenderTexture.format = RenderTextureFormat.ARGB32;

        // 备份原始材质
        if (Book != null)
        {
            Renderer renderer = Book.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterials = renderer.materials;
            }
        }
    }

    void TakePhoto()
    {
        if (Book == null)
        {
            Debug.LogError("PhotoDisplayObject is not assigned!");
            return;
        }

        // 设置相机的目标纹理
        photoCamera.targetTexture = CoverRenderTexture;

        // 渲染相机视图
        photoCamera.Render();

        // 重置相机的目标纹理,让它继续渲染到屏幕上
        photoCamera.targetTexture = null;

        // 获取目标GameObject的Renderer
        Renderer renderer = Book.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("No Renderer found on the PhotoDisplayObject!");
            return;
        }

        // 复制原始材质数组
        Material[] newMaterials = (Material[])originalMaterials.Clone();

        // 找到并替换目标材质
        bool materialFound = false;
        for (int i = 0; i < newMaterials.Length; i++)
        {
            if (newMaterials[i].name.Replace(" (Instance)", "") == CoverMaterialName)
            {
                newMaterials[i].mainTexture = CoverRenderTexture;
                materialFound = true;
                break;
            }
        }

        if (!materialFound)
        {
            Debug.LogError($"Material '{CoverMaterialName}' not found on the object!");
            return;
        }

        // 应用修改后的材质数组
        renderer.materials = newMaterials;
    }

    void TakeSidePhoto()
    {
        if (Book == null)
        {
            Debug.LogError("PhotoDisplayObject is not assigned!");
            return;
        }

        // 设置相机的目标纹理
        sideCamera.targetTexture = SideRenderTexture;

        // 渲染相机视图
        sideCamera.Render();

        // 重置相机的目标纹理,让它继续渲染到屏幕上
        sideCamera.targetTexture = null;

        // 获取目标GameObject的Renderer
        Renderer renderer = Book.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("No Renderer found on the PhotoDisplayObject!");
            return;
        }

        // 复制原始材质数组
        Material[] newMaterials = (Material[])originalMaterials.Clone();

        // 找到并替换目标材质
        bool materialFound = false;
        for (int i = 0; i < newMaterials.Length; i++)
        {
            if (newMaterials[i].name.Replace(" (Instance)", "") == SideMaterialName)
            {
                newMaterials[i].mainTexture = SideRenderTexture;
                materialFound = true;
                break;
            }
        }

        if (!materialFound)
        {
            Debug.LogError($"Material '{SideMaterialName}' not found on the object!");
            return;
        }

        // 应用修改后的材质数组
        renderer.materials = newMaterials;
    }
    // 也可以通过输入键触发
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         TakePhoto();
    //     }
    // }

    // 玩家角色加载好后获取头部位置，然后拍照
    public override void OnAvatarChanged(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            SendCustomEventDelayedSeconds(nameof(StartTakePhoto), 0.3f);
        }
    }

    public void StartTakePhoto()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        // 拍照
        Vector3 headPosition = player.GetBonePosition(HumanBodyBones.Head);

        float offsetDistance = 0.5f;

        Quaternion rotation = Quaternion.Euler(0, 30, 0);
        Vector3 offset = rotation * Vector3.forward * offsetDistance;

        photoCamera.transform.position = headPosition + offset;
        photoCamera.transform.LookAt(headPosition);

        TakePhoto();

        var displayName = player.displayName;
        switch (displayName)
        {
            case "TB_Xu":
                displayName = "TB";
                break;
            case "小鹿包DeerBun":
                displayName = "小鹿包";
                break;
            default:
                break;
        }

        string currentLanguage = VRCPlayerApi.GetCurrentLanguage();
        string formatString = GetLocalizedString(currentLanguage);
        titleText.text = string.Format(formatString, displayName);
        TakeSidePhoto();
    }
    private string GetLocalizedString(string language)
    {
        // 1. 尝试完全匹配
        for (int i = 0; i < languageKeys.Length; i++)
        {
            if (languageKeys[i] == language)
            {
                return happyTexts[i];
            }
        }

        // 2. 尝试匹配主语言部分 (如 zh-CN -> zh)
        string mainLanguage = language.Contains("-") ? language.Split('-')[0] : "";
        if (!string.IsNullOrEmpty(mainLanguage))
        {
            for (int i = 0; i < languageKeys.Length; i++)
            {
                if (languageKeys[i] == mainLanguage)
                {
                    return happyTexts[i];
                }
            }
        }

        // 3. 默认回退英语
        return happyTexts[0];
    }
}
