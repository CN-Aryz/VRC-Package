
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket5.Circle2539.Timer
{
    public class SyncMinSlider : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.Linear)]
        float _minutes = 0;

        public GameObject _timerSystem;

        public Text _timerText;

        public Slider _minutesSlider;


        void LateUpdate()
        {
            var timer = (UdonBehaviour)_timerSystem.GetComponent(typeof(UdonBehaviour));
            var isTimerActive = (bool)timer.GetProgramVariable("isTimerActive");

            if (isTimerActive)
            {
                return;
            }

            _minutesSlider.value = _minutes;

            var min = (int)_minutes;
            var minStr = min.ToString("D2");
            var secStr = (_timerText.text.Split(':'))[1];

            _timerText.text = minStr + ":" + secStr;
        }

        public void OnChangeOwner()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            }
        }

        public void GetSliderValue()
        {
            _minutes = _minutesSlider.value;
        }
    }
}