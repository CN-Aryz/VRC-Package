
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ClickCloseSelf : UdonSharpBehaviour
{

    public void OnIconClick()
    {
        Debug.Log("close self");
        gameObject.SetActive(false);
    }
}
