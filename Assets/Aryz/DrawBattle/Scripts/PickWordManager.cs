
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace DrawBattle
{
    public class PickWordManager : StateHandlerBase
    {
        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private PickWord[] pickWordUIs;

        [UdonSynced]
        private string[] randomWords = new string[3];

        public string pickedWord = "";

        [UdonSynced]
        public string word = "";
        // private string[] historyWords = new string[10];

        // 用于提示给队伍A和队伍B的画手当前要画的词语
        public TextMeshProUGUI teamAWordText;
        public TextMeshProUGUI teamBWordText;

        public void OnPickWord()
        {
            TakeOwner();
            word = randomWords[int.Parse(pickedWord) - 1];
            UpdateTeamWordText();
            RequestSerialization();
            gameManager.OnPickWord();
        }

        public override void OnStateUpdate(GameState prevState, GameState nextState)
        {
            if (Networking.IsOwner(gameManager.gameObject))
            {
                if (nextState == GameState.CHOOSE_WORD)
                {
                    randomWords = new string[3];
                    pickedWord = "";


                    // 随机从题库中抽取三个题目
                    TakeOwner();
                    randomWords = gameManager.wordsetManager.GetRandomWords(3);
                    RequestSerialization();

                    if (randomWords == null || randomWords.Length < 3)
                    {
                        Debug.LogError("题库中没有足够的题目");
                        return;
                    }

                    // 设置题目
                    for (int i = 0; i < pickWordUIs.Length; i++)
                    {
                        string word1 = gameManager.wordsetManager.splitWordAndAnswer(randomWords[0])[0];
                        string word2 = gameManager.wordsetManager.splitWordAndAnswer(randomWords[1])[0];
                        string word3 = gameManager.wordsetManager.splitWordAndAnswer(randomWords[2])[0];
                        pickWordUIs[i].SetWords(word1, word2, word3);
                    }
                }

                if (nextState == GameState.DRAWING && string.IsNullOrEmpty(pickedWord))
                {
                    // 随机选择词语
                    pickedWord = UnityEngine.Random.Range(1, 4).ToString();
                    OnPickWord();
                }
            }
        }


        public override void OnDeserialization()
        {
            UpdateTeamWordText();
        }


        private void UpdateTeamWordText()
        {
            string showName = word.Split('|')[0];
            if (teamAWordText != null)
            {
                teamAWordText.text = showName;
            }
            if (teamBWordText != null)
            {
                teamBWordText.text = showName;
            }
        }
    }

}