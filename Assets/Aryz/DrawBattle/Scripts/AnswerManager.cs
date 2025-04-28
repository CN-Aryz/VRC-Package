
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DrawBattle
{
    public class AnswerManager : StateHandlerBase
    {
        public GameManager gameManager;
        public PickWordManager pickWordManager;
        public PlayersManager playersManager;
        private AKeyboard keyboard;

        private VRCPlayerApi localPlayer;

        // 回答是否已经被处理
        [UdonSynced]
        private bool teamAAnswered = false;
        [UdonSynced]
        private bool teamBAnswered = false;

        // 回答是否已经被处理
        [UdonSynced]
        public bool teamAWin = false;
        [UdonSynced]
        public bool teamBWin = false;


        [UdonSynced]
        private float teamATime = -1f;
        [UdonSynced]
        private float teamBTime = -1f;

        // 玩家发起回答
        public void OnEndEdit()
        {
            Debug.Log($"OnEndEdit {keyboard.text} {pickWordManager.word}");

            string correctAnswer = pickWordManager.word.Split('|')[1];
            if (!CanAnswer())
            {
                Debug.Log("Can't Answer");
                return;
            }
            if (string.Equals(correctAnswer, keyboard.text, StringComparison.OrdinalIgnoreCase))
            {
                if (playersManager.IsPlayerInTeamA(localPlayer.playerId))
                {
                    TeamACorrect();
                }
                else if (playersManager.IsPlayerInTeamB(localPlayer.playerId))
                {
                    TeamBCorrect();
                }
            }
            else
            {
                Debug.Log("Wrong");
            }
        }


        // 判断当前能否回答
        public bool CanAnswer()
        {
            // bool isPainter = gameManager.IsCurrentPlayerDrawing();
            // bool drawing = gameManager.GetGameState() == GameState.DRAWING;
            // bool playerInGame = playersManager.IsPlayerInTeamA(localPlayer.playerId) || playersManager.IsPlayerInTeamB(localPlayer.playerId);
            // bool playerTeamAnswered = playersManager.IsPlayerInTeamA(localPlayer.playerId) ? teamAAnswered : teamBAnswered;
            // return !isPainter && drawing && playerInGame && !playerTeamAnswered;
            return true;
        }

        public void TeamACorrect()
        {
            teamAAnswered = true;
            if (!teamBAnswered)
            {
                teamAWin = true;
            }
            teamATime = Networking.GetServerTimeInMilliseconds();
            TakeOwner();
            RequestSerialization();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OnTeamACorrect");
        }

        public void TeamBCorrect()
        {
            teamBAnswered = true;
            if (!teamAAnswered)
            {
                teamBWin = true;
            }
            teamBTime = Networking.GetServerTimeInMilliseconds();
            TakeOwner();
            RequestSerialization();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OnTeamBCorrect");
        }
        public void OnTeamACorrect()
        {
            Debug.Log("Team A Correct");
            if (Networking.IsOwner(gameManager.gameObject))
            {
                if (!teamBAnswered)
                {
                    gameManager.IncreaseTeamAScore(200);
                }
                else
                {
                    gameManager.IncreaseTeamAScore(100);
                    gameManager.EndDrawing();
                }
            }
        }

        public void OnTeamBCorrect()
        {
            Debug.Log("Team B Correct");
            if (Networking.IsOwner(gameManager.gameObject))
            {
                if (!teamAAnswered)
                {
                    gameManager.IncreaseTeamBScore(200);
                }
                else
                {
                    gameManager.IncreaseTeamBScore(100);
                    gameManager.EndDrawing();
                }
            }
        }


        // 在游戏Manager开始新题目时调用此方法重置状态
        public void ResetAnswerState()
        {
            teamAAnswered = false;
            teamBAnswered = false;
            teamATime = -1f;
            teamBTime = -1f;
            teamAWin = false;
            teamBWin = false;
            TakeOwner();
            RequestSerialization();
        }

        public override void OnStateUpdate(GameState prevState, GameState nextState)
        {
            if (nextState != GameState.END_DRAW)
            {
                ResetAnswerState();
            }
        }

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }
    }
}
