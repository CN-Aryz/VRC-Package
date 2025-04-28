using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace DrawBattle
{
    public class SceneStateManager : StateHandlerBase
    {

        [SerializeField]
        private GameManager gameManager; // 添加一个文本框引用
        [SerializeField]
        private TextMeshProUGUI stateText; // 添加一个文本框引用

        public GameObject[] onlyShowOnChooseWord;
        public GameObject[] onlyShowOnPainterChooseWord;
        public GameObject[] onlyShowOnPainterDrawing;

        public override void OnStateUpdate(GameState prevState, GameState nextState)
        {
            foreach (var obj in onlyShowOnChooseWord)
            {
                obj.SetActive(nextState == GameState.CHOOSE_WORD);
            }

            bool isPainter = gameManager.IsCurrentPlayerDrawing();
            foreach (var obj in onlyShowOnPainterChooseWord)
            {
                obj.SetActive(nextState == GameState.CHOOSE_WORD && isPainter);
            }
            foreach (var obj in onlyShowOnPainterDrawing)
            {
                obj.SetActive(nextState == GameState.DRAWING && isPainter);
            }

            UpdateStateText(nextState);
            // // 重置所有笔
            // foreach (var penManager in penSettings.penManagers)
            //     if (penManager)
            //         penManager.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(QvPen_PenManager.Respawn));
            // // 重置所有橡皮
            // foreach (var eraserManager in penSettings.eraserManagers)
            //     if (eraserManager)
            //         eraserManager.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(QvPen_EraserManager.ResetEraser));

        }


        private void UpdateStateText(GameState gameState)
        {
            if (stateText != null)
            {
                switch (gameState)
                {
                    case GameState.NOT_START:
                        stateText.text = "游戏未开始";
                        break;
                    case GameState.START:
                        stateText.text = "游戏开始";
                        break;
                    case GameState.CHOOSE_WORD:
                        stateText.text = "选择词语";
                        break;
                    case GameState.DRAWING:
                        stateText.text = "绘画中";
                        break;
                    case GameState.END_DRAW:
                        stateText.text = "绘画结束";
                        break;
                }
            }
        }
    }
}