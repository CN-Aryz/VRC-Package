
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FolderIcon : UdonSharpBehaviour
{
    public GameObject[] targets;

    private GameObject currentOpen = null;
    private int currentIndex = 0;

    public void OnIconClick()
    {
        // 已有面板打开，先关闭
        if (currentOpen != null)
        {
            currentOpen.SetActive(false);
            currentOpen = null;
            return;
        }

        // 未打开任何图片，则从列表里随机选一个打开
        if (targets == null || targets.Length == 0) return;

        currentIndex = (currentIndex + 1) % targets.Length;
        currentOpen = targets[currentIndex];
        currentOpen.SetActive(true);
    }
}
