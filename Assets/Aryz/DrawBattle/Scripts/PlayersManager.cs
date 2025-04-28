
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DrawBattle
{
    public class PlayersManager : BaseSyncModule
    {
        public VRCPlayerApi localPlayer;
        [SerializeField] private TextMeshProUGUI TeamAPlayerNameText;
        [SerializeField] private TextMeshProUGUI TeamBPlayerNameText;

        // 队伍A和队伍B的玩家
        [UdonSynced]
        private int[] teamA = new int[12];
        [UdonSynced]
        private int[] teamB = new int[12];

        void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }

        public bool AreTeamsReady()
        {
            int teamACount = 0;
            int teamBCount = 0;

            for (int i = 0; i < teamA.Length; i++)
            {
                if (teamA[i] != 0)
                {
                    teamACount++;
                }
            }

            for (int i = 0; i < teamB.Length; i++)
            {
                if (teamB[i] != 0)
                {
                    teamBCount++;
                }
            }

            // 每边至少两位玩家
            return teamACount >= 1 && teamBCount >= 1;
            // return teamACount >= 2 && teamBCount >= 2;
        }

        public void JoinTeamA()
        {
            if (localPlayer == null) return;
            // 如果玩家已经在队伍B中，先从队伍B中移除
            if (IsPlayerInTeamB(localPlayer.playerId))
            {
                RemovePlayerFromTeamB(localPlayer.playerId);
            }

            // 如果玩家已经在队伍A中，直接返回
            if (IsPlayerInTeamA(localPlayer.playerId)) return;

            // 将玩家添加到队伍A中
            for (int i = 0; i < teamA.Length; i++)
            {
                if (teamA[i] == 0)
                {
                    teamA[i] = localPlayer.playerId;
                    break;
                }
            }

            TakeOwner();
            RequestSerialization();

            // 更新UI
            UpdatePlayerNameText();
        }

        public void JoinTeamB()
        {
            if (localPlayer == null) return;
            // 如果玩家已经在队伍A中，先从队伍A中移除
            if (IsPlayerInTeamA(localPlayer.playerId))
            {
                RemovePlayerFromTeamA(localPlayer.playerId);
            }

            // 如果玩家已经在队伍B中，直接返回
            if (IsPlayerInTeamB(localPlayer.playerId)) return;

            // 将玩家添加到队伍B中
            for (int i = 0; i < teamB.Length; i++)
            {
                if (teamB[i] == 0)
                {
                    teamB[i] = localPlayer.playerId;
                    break;
                }
            }

            TakeOwner();
            RequestSerialization();

            // 更新UI
            UpdatePlayerNameText();
        }

        public void LeaveTeam()
        {
            if (localPlayer == null) return;

            if (IsPlayerInTeamA(localPlayer.playerId))
            {
                RemovePlayerFromTeamA(localPlayer.playerId);
            }
            else if (IsPlayerInTeamB(localPlayer.playerId))
            {
                RemovePlayerFromTeamB(localPlayer.playerId);
            }

            UpdatePlayerNameText();
        }


        private void RemovePlayerFromTeamA(int playerId)
        {
            for (int i = 0; i < teamA.Length; i++)
            {
                if (teamA[i] == playerId)
                {
                    teamA[i] = 0;
                    break;
                }
            }
        }


        private void RemovePlayerFromTeamB(int playerId)
        {
            for (int i = 0; i < teamB.Length; i++)
            {
                if (teamB[i] == playerId)
                {
                    teamB[i] = 0;
                    break;
                }
            }
        }

        public bool IsPlayerInTeamA(int playerId)
        {
            for (int i = 0; i < teamA.Length; i++)
            {
                if (teamA[i] == playerId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPlayerInTeamB(int playerId)
        {
            for (int i = 0; i < teamB.Length; i++)
            {
                if (teamB[i] == playerId)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdatePlayerNameText()
        {
            string teamAPlayerNames = "";
            for (int i = 0; i < teamA.Length; i++)
            {
                if (teamA[i] != 0)
                {
                    VRCPlayerApi player = VRCPlayerApi.GetPlayerById(teamA[i]);
                    if (player != null)
                    {
                        teamAPlayerNames += player.displayName + "\n";
                    }
                }
            }

            TeamAPlayerNameText.text = teamAPlayerNames;

            string teamBPlayerNames = "";
            for (int i = 0; i < teamB.Length; i++)
            {
                if (teamB[i] != 0)
                {
                    VRCPlayerApi player = VRCPlayerApi.GetPlayerById(teamB[i]);
                    if (player != null)
                    {
                        teamBPlayerNames += player.displayName + "\n";
                    }
                }
            }

            TeamBPlayerNameText.text = teamBPlayerNames;
        }

        public int[] GetTeamAPlayers()
        {
            int count = 0;

            // 先计算队伍A中有多少玩家
            for (int i = 0; i < teamA.Length; i++)
            {
                if (teamA[i] != 0)
                {
                    count++;
                }
            }

            // 创建一个新的数组来存储玩家ID
            int[] players = new int[count];
            int index = 0;

            // 将玩家ID复制到新数组中
            for (int i = 0; i < teamA.Length; i++)
            {
                if (teamA[i] != 0)
                {
                    players[index++] = teamA[i];
                }
            }

            return players;
        }

        public int[] GetTeamBPlayers()
        {
            int count = 0;

            // 先计算队伍B中有多少玩家
            for (int i = 0; i < teamB.Length; i++)
            {
                if (teamB[i] != 0)
                {
                    count++;
                }
            }

            // 创建一个新的数组来存储玩家ID
            int[] players = new int[count];
            int index = 0;

            // 将玩家ID复制到新数组中
            for (int i = 0; i < teamB.Length; i++)
            {
                if (teamB[i] != 0)
                {
                    players[index++] = teamB[i];
                }
            }

            return players;
        }

        // 新增加的方法
        public void ResetAllPlayers()
        {
            TakeOwner();
            for (int i = 0; i < teamA.Length; i++)
            {
                teamA[i] = 0;
            }
            for (int i = 0; i < teamB.Length; i++)
            {
                teamB[i] = 0;
            }
            RequestSerialization();
            Debug.Log("[玩家管理] 已重置所有队伍数据");
        }

        public override void OnDeserialization()
        {
            UpdatePlayerNameText();
        }
    }
}
