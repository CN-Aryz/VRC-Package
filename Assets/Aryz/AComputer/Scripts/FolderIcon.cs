﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FolderIcon : UdonSharpBehaviour
{
    public GameObject target;

    public void OnIconClick()
    {
        target.SetActive(!target.activeSelf);
    }
}
