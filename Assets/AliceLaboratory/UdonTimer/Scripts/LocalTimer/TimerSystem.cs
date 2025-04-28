
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket5.Circle2539.Timer
{
    public class TimerSystem : UdonSharpBehaviour
    {
        float totalTime;

        float oldSec;

        int minutes;

        float seconds;

        bool isTimerPause;

        public bool isTimerActive;

        public Slider minutesSlider;

        public Slider secondsSlider;

        public Text timerText;

        public Text timeUpText;

        public Button startButton;

        public Button stopButton;

        public Button resetButton;

        public AudioSource alarm;

        void Start()
        {
            totalTime = minutes * 60 + seconds;
            oldSec = 0;
            stopButton.interactable = false;
            resetButton.interactable = false;
        }


        ///<summary>
        ///Startボタンクリックイベント
        ///</summary>
        public void OnStartClicked()
        {
            var minAndSec = timerText.text.Split(':');
            minutes = int.Parse(minAndSec[0]);
            seconds = float.Parse(minAndSec[1]);
            Debug.Log(minutes);
            Debug.Log(seconds);

            if (minutes <= 0 && seconds <= 0.01)
            {
                return;
            }

            if (isTimerPause) // 一時停止状態から復帰した場合
            {
                isTimerPause = false;
            }

            totalTime = minutes * 60 + seconds;
            oldSec = 0;

            isTimerActive = true;

            // UI更新
            if (timeUpText != null)
            {
                timeUpText.text = "";
            }
            startButton.interactable = false;
            stopButton.interactable = true;
            resetButton.interactable = true;
            minutesSlider.interactable = false;
            secondsSlider.interactable = false;
        }


        ///<summary>
        ///Stopボタンクリックイベント
        ///</summary>
        public void OnStopClicked()
        {
            isTimerPause = true;
            startButton.interactable = true;
            stopButton.interactable = false;
        }


        ///<summary>
        ///Resetボタンクリックイベント
        ///</summary>
        public void OnResetClicked()
        {
            isTimerActive = false;
            startButton.interactable = true;
            stopButton.interactable = false;
            resetButton.interactable = false;
            minutesSlider.interactable = true;
            secondsSlider.interactable = true;
        }


        void Update()
        {
            if (totalTime <= 0 || !isTimerActive || isTimerPause)
            {
                return;
            }

            totalTime = minutes * 60 + seconds;
            totalTime -= Time.deltaTime;

            minutes = (int)totalTime / 60;
            seconds = totalTime - minutes * 60;

            if ((int)seconds != (int)oldSec)
            {
                timerText.text = minutes.ToString("00") + ":" + ((int)seconds).ToString("00");
            }
            oldSec = seconds;

            if (totalTime <= 0f)
            {
                isTimerActive = false;

                // アラームがセットされていれば再生
                if (alarm != null)
                {
                    alarm.Play();
                }

                // UIを更新
                if (timeUpText != null) // TimeUpTestがセットされていればTime Up表示
                {
                    timeUpText.text = "Time Up";
                }
                startButton.interactable = true;
                stopButton.interactable = false;
                resetButton.interactable = false;
                minutesSlider.interactable = true;
                secondsSlider.interactable = true;
            }
        }
    }
}

