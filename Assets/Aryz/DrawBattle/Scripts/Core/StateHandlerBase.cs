
using DrawBattle;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public abstract class StateHandlerBase : BaseSyncModule
{
    public virtual void OnStateEnter() { }
    public virtual void OnStateExit() { }
    public virtual void OnStateUpdate(GameState prevState, GameState nextState) { }
}
