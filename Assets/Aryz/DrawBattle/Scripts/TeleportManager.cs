
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DrawBattle
{

    public class TeleportManager : StateHandlerBase
    {
        public GameManager gameManager;
        public PlayersManager playersManager;

        public Transform TeamADrawing;
        public Transform TeamBDrawing;
        public Transform TeamAWaiting;
        public Transform TeamBWaiting;

        private VRCPlayerApi localPlayer;

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }

        public void TeleportToDrawingAreaA()
        {
            if (localPlayer != null && TeamADrawing != null)
            {
                localPlayer.TeleportTo(TeamADrawing.position, TeamADrawing.rotation);
            }
        }

        public void TeleportToWaitingAreaA()
        {
            if (localPlayer != null && TeamBWaiting != null)
            {
                localPlayer.TeleportTo(TeamBWaiting.position, TeamBWaiting.rotation);
            }
        }


        public void TeleportToDrawingAreaB()
        {
            if (localPlayer != null && TeamBDrawing != null)
            {
                localPlayer.TeleportTo(TeamBDrawing.position, TeamBDrawing.rotation);
            }
        }

        public void TeleportToWaitingAreaB()
        {
            if (localPlayer != null && TeamAWaiting != null)
            {
                localPlayer.TeleportTo(TeamAWaiting.position, TeamAWaiting.rotation);
            }
        }

        public override void OnStateUpdate(GameState prevState, GameState nextState)
        {
            if (gameManager == null) return;
            if (playersManager == null) return;

            Debug.Log("[TeleportManager] OnStateUpdate");
            if (nextState == GameState.CHOOSE_WORD)
            {
                if (gameManager.IsCurrentPlayerDrawing())
                {
                    if (playersManager.IsPlayerInTeamA(localPlayer.playerId))
                    {
                        Debug.Log("[TeleportManager] TeleportToDrawingAreaA");
                        TeleportToDrawingAreaA();
                    }
                    else if (playersManager.IsPlayerInTeamB(localPlayer.playerId))
                    {
                        Debug.Log("[TeleportManager] TeleportToDrawingAreaB");
                        TeleportToDrawingAreaB();
                    }
                }
            }
            else if (nextState == GameState.END_DRAW)
            {
                if (playersManager.IsPlayerInTeamA(localPlayer.playerId))
                {
                    TeleportToWaitingAreaA();
                }
                else if (playersManager.IsPlayerInTeamB(localPlayer.playerId))
                {
                    TeleportToWaitingAreaB();
                }
            }
        }
    }

}