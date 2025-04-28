
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AKeyboardDemo : UdonSharpBehaviour
{
    public AKeyboard keyboard;
    void Start()
    {

    }

    public void OnEndEdit()
    {
        Debug.Log("OnEndEdit");
        Debug.Log(keyboard.text);
    }
}
