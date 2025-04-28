
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket5.Circle2539.Timer
{
    public class TimeSetter : UdonSharpBehaviour
    {
        float minutes = 0;

        float seconds = 0;

        public GameObject timerSystem;

        public Text timerText;

        public Slider minutesSlider;

        public Slider secondsSlider;

        public override void Interact()
        {
            minutes = minutesSlider.value;
            seconds = secondsSlider.value;
        }

        void Update()
        {
            var timer = (UdonBehaviour)timerSystem.GetComponent(typeof(UdonBehaviour));
            var isTimerActive = (bool)timer.GetProgramVariable("isTimerActive");

            if (isTimerActive)
            {
                return;
            }

            minutesSlider.value = minutes;
            secondsSlider.value = seconds;

            var min = (int)minutes;
            var sec = (int)seconds;
            var minStr = "";
            var secStr = "";

            if (min < 10)
            {
                minStr = "0" + min.ToString();
            }
            else
            {
                minStr = min.ToString();
            }

            if (sec < 10)
            {
                secStr = "0" + sec.ToString();
            }
            else
            {
                secStr = sec.ToString();
            }

            timerText.text = minStr + ":" + secStr;
        }
    }
}

