using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace DrawBattle
{
    public enum WordsetState
    {
        NOT_LOADED,
        LOADING,
        LOADED,
        ERROR
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WordsetManager : UdonSharpBehaviour
    {
        [SerializeField]
        private VRCUrl url;

        [SerializeField]
        private TextMeshProUGUI stateText; // 添加一个文本框引用
        private WordsetState state; // 添加一个文本框引用

        private string[] originWords;

        void Start()
        {
            UpdateState(WordsetState.NOT_LOADED);
            Debug.Log($"正在初始化题目");
            LoadWord(url);
        }

        public void LoadWord(VRCUrl url)
        {
            UpdateState(WordsetState.LOADING);
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            string resultAsUTF8 = result.Result;
            // 调用函数解析并存储题目和答案
            ParseAndStoreQuestions(resultAsUTF8);
            UpdateState(WordsetState.LOADED);
        }

        private void ParseAndStoreQuestions(string data)
        {
            // 假设字符串格式为 "题目|答案\n题目|答案"
            originWords = data.Split('\n');
        }
        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.LogError($"Error loading string: {result.ErrorCode} - {result.Error}");
            UpdateState(WordsetState.ERROR);
        }

        private void UpdateState(WordsetState state)
        {
            switch (state)
            {
                case WordsetState.NOT_LOADED:
                    UpdateStatusText("题库未加载");
                    break;
                case WordsetState.LOADING:
                    UpdateStatusText("题库加载中");
                    break;
                case WordsetState.LOADED:
                    UpdateStatusText($"题库加载成功 数量：{originWords.Length}");
                    break;
                case WordsetState.ERROR:
                    UpdateStatusText("题库加载失败");
                    break;
            }
            this.state = state;
        }

        // 封装更新文本框状态的函数
        private void UpdateStatusText(string status)
        {
            if (stateText != null)
            {
                stateText.text = status;
            }
        }

        // 获取n个随机的单词，并返回它们的索引
        public string[] GetRandomWords(int num)
        {
            if (originWords == null || originWords.Length == 0)
            {
                Debug.LogError("题库未加载或为空");
                return null;
            }

            if (num <= 0)
            {
                Debug.LogError("请求的单词数量必须大于0");
                return null;
            }

            string[] resultWords;

            // 如果请求的单词数量大于题库中的单词数量，返回所有单词的索引
            if (num >= originWords.Length)
            {
                resultWords = new string[originWords.Length];
                for (int i = 0; i < originWords.Length; i++)
                {
                    resultWords[i] = originWords[i];
                }
                return resultWords;
            }

            // 创建一个索引数组
            int[] indices = new int[originWords.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            // 使用Fisher-Yates洗牌算法对索引数组进行随机排序
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                int temp = indices[i];
                indices[i] = indices[randomIndex];
                indices[randomIndex] = temp;
            }

            // 取前num个索引
            resultWords = new string[num];
            for (int i = 0; i < num; i++)
            {
                resultWords[i] = originWords[indices[i]];
            }

            return resultWords;
        }

        // 根据索引获取单词
        public string GetWordByIndex(int index)
        {
            if (index < 0 || index >= originWords.Length)
            {
                Debug.LogError("索引超出范围");
                return null;
            }
            return originWords[index];
        }

        public string[] splitWordAndAnswer(string originWord)
        {
            return originWord.Split('|');
            // string[] parts = lines[i].Split('|');
            // if (parts.Length == 2)
            // {
            //     originWords[i] = parts[0];
            //     answers[i] = parts[1];
            // }
            // else
            // {
            //     Debug.LogError($"Invalid format in line {i}: {lines[i]}");
            // }
        }
    }
}