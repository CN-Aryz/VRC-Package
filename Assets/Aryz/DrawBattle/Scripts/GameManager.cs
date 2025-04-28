
using System;
using System.Collections.Generic;
using BestHTTP.Extensions;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DrawBattle
{
    // 枚举游戏状态
    public enum GameState
    {
        NOT_START,
        START,
        CHOOSE_WORD,
        DRAWING,
        END_DRAW,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GameManager : BaseSyncModule
    {
        [Header("Manager References")]
        [SerializeField]
        private AnswerManager answerManager;
        [SerializeField]
        private PlayersManager playersManager;
        [SerializeField]
        public WordsetManager wordsetManager;
        [SerializeField]
        private StateHandlerBase[] stateHandlers;
        [Header("Manager References")]
        [SerializeField]
        private TextMeshProUGUI stateText;
        [SerializeField]
        private TextMeshProUGUI ownerText;
        [SerializeField]
        private TextMeshProUGUI teamAStateText;
        [SerializeField]
        private TextMeshProUGUI teamAScoreText;
        [SerializeField]
        private TextMeshProUGUI teamBStateText;
        [SerializeField]
        private TextMeshProUGUI teamBScoreText;
        [SerializeField]
        private TextMeshProUGUI currentRoundText;
        [SerializeField]
        private SyncCountdown countdown;


        [UdonSynced]
        private GameState gameState = GameState.NOT_START;

        [UdonSynced]
        private int teamAScore = 0;

        [UdonSynced]
        private int teamBScore = 0;

        // 当前绘画的队伍A玩家索引
        [UdonSynced]
        private int currentTeamAPlayerIndex = 0;
        // 当前绘画的队伍B玩家索引
        [UdonSynced]
        private int currentTeamBPlayerIndex = 0;

        [UdonSynced]
        private int currentRound = -1;

        // 当前对词语有选择权的队伍 0是A队 1是B队
        [UdonSynced]
        private int currentPickWordTeam = 0;

        private GameState lastGameState; // 用于记录上一次的游戏状态

        private VRCPlayerApi localPlayer;

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            ownerText.text = $"Owner: {localPlayer.displayName} | {localPlayer.playerId}";
        }

        // 开始游戏初始化
        public void StartGame()
        {
            // 检查玩家数量是否足够
            if (!playersManager.AreTeamsReady())
            {
                UpdateStateText("玩家不足，无法开始游戏");
                return;
            }

            Debug.Log("StartGame");
            SetGameState(GameState.START);

            SetCurrentRound(0);
            SetTeamAScore(0);
            SetTeamBScore(0);

            TakeOwner();
            RequestSerialization();
            // 开始倒计时
            UpdateStateText("正在准备开始游戏");
            UpdateScoreText();
            countdown.StartCountdown(3f);
        }

        // 开始一个新的回合
        private void StartNewRound()
        {
            Debug.Log("StartNewRound");
            TakeOwner();

            SetCurrentRound(currentRound + 1);

            if (answerManager.teamAWin)
            {
                currentPickWordTeam = 1;
                GetNextTeamBPlayer();
            }
            else if (answerManager.teamBWin)
            {
                currentPickWordTeam = 0;
                GetNextTeamAPlayer();
            }

            RequestSerialization();

            answerManager.ResetAnswerState();
            // 开始选择词语倒计时

            StartPickWord();
        }

        private void StartPickWord()
        {
            Debug.Log("StartPickWord");
            TakeOwner();
            SetGameState(GameState.CHOOSE_WORD);
            countdown.StartCountdown(10);
        }

        public void OnPickWord()
        {
            Debug.Log("GameManager OnPickWord");
            if (Networking.IsOwner(gameObject))
            {
                StartDrawing();
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StartDrawing");
            }
        }

        // 选择词汇后开始绘画倒计时
        public void StartDrawing()
        {
            Debug.Log("StartDrawing");
            SetGameState(GameState.DRAWING);
            countdown.StartCountdown(60);
        }

        public void IncreaseTeamAScore(int score)
        {
            TakeOwner();
            SetTeamAScore(teamAScore + score);
            RequestSerialization();
        }

        public void IncreaseTeamBScore(int score)
        {
            TakeOwner();
            SetTeamBScore(teamBScore + score);
            RequestSerialization();
        }

        // 绘画结束
        public void EndDrawing()
        {
            Debug.Log("EndDrawing");
            SetGameState(GameState.END_DRAW);
            // LastWord = CurrentWord;
            countdown.StartCountdown(5);
        }

        public void SetGameState(GameState state)
        {
            gameState = state;
            OnGameStateChange();
            RequestSerialization();
        }

        public void OnGameStateChange()
        {
            NotifyStateChange(lastGameState, gameState);
            lastGameState = gameState;
        }

        private void NotifyStateChange(GameState prevState, GameState newState)
        {
            foreach (var handler in stateHandlers)
            {
                if (handler != null)
                {
                    handler.OnStateUpdate(prevState, newState);
                }
            }
        }

        private void SetCurrentRound(int round)
        {
            currentRound = round;
            UpdateCurrentRoundText();
        }

        private void SetTeamAScore(int score)
        {
            teamAScore = score;
            UpdateScoreText();
        }

        private void SetTeamBScore(int score)
        {
            teamBScore = score;
            UpdateScoreText();
        }

        public void OnCountdownEnd()
        {
            Debug.Log($"[GameManager] CountdownEnd {gameState}");
            if (Networking.IsOwner(gameObject))
            {
                switch (gameState)
                {
                    case GameState.START:
                        StartNewRound();
                        break;
                    case GameState.CHOOSE_WORD:
                        OnPickWord();
                        break;
                    case GameState.DRAWING:
                        EndDrawing();
                        break;
                    case GameState.END_DRAW:
                        StartNewRound();
                        break;
                }
            }
        }

        public void GetNextTeamAPlayer()
        {
            int[] teamAPlayers = playersManager.GetTeamAPlayers();
            if (teamAPlayers.Length == 0)
            {
                return; // 没有玩家
            }
            currentTeamAPlayerIndex = (currentTeamAPlayerIndex + 1) % teamAPlayers.Length;
        }

        public void GetNextTeamBPlayer()
        {
            int[] teamBPlayers = playersManager.GetTeamBPlayers();
            if (teamBPlayers.Length == 0)
            {
                return; // 没有玩家
            }
            currentTeamBPlayerIndex = (currentTeamBPlayerIndex + 1) % teamBPlayers.Length;
        }

        public bool IsCurrentPlayerDrawing()
        {
            int currentPlayerId = localPlayer.playerId;
            int[] teamAPlayers = playersManager.GetTeamAPlayers();
            int[] teamBPlayers = playersManager.GetTeamBPlayers();

            if (Array.IndexOf(teamAPlayers, currentPlayerId) == currentTeamAPlayerIndex)
            {
                return true;
            }

            if (Array.IndexOf(teamBPlayers, currentPlayerId) == currentTeamBPlayerIndex)
            {
                return true;
            }

            return false;
        }

        private void UpdateStateText(string status)
        {
            if (stateText != null)
            {
                stateText.text = status;
            }
        }

        private void UpdateScoreText()
        {
            if (teamAScoreText != null)
            {
                teamAScoreText.text = teamAScore.ToString();
            }

            if (teamBScoreText != null)
            {
                teamBScoreText.text = teamBScore.ToString();
            }
        }

        private void UpdateCurrentRoundText()
        {
            if (currentRoundText != null)
            {
                currentRoundText.text = currentRound.ToString();
            }
        }

        // 获取当前游戏状态
        public GameState GetGameState()
        {
            return gameState;
        }

        public override void OnDeserialization()
        {
            if (lastGameState != gameState)
            {
                OnGameStateChange();
            }

            UpdateScoreText();
            UpdateCurrentRoundText();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            Debug.Log($"[GameManager] OnOwnershipTransferred {player.displayName}");
            ownerText.text = $"Owner: {player.displayName} | {player.playerId}";
        }
    }
}
