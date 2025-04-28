
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BaseSyncModule : UdonSharpBehaviour
{
    protected int _syncVersion;

    public virtual void RequestSync()
    {
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        _syncVersion++;
        RequestSerialization();
    }

    public virtual void TakeOwner()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }
}
