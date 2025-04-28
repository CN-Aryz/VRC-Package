
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket5.Circle2539.Timer
{
    public class SyncSecSlider : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.Linear)]
        float _seconds = 0;

        public GameObject _timerSystem;

        public Text _timerText;

        public Slider _secondsSlider;


        void LateUpdate()
        {
            var timer = (UdonBehaviour)_timerSystem.GetComponent(typeof(UdonBehaviour));
            var isTimerActive = (bool)timer.GetProgramVariable("isTimerActive");

            if (isTimerActive)
            {
                return;
            }

            _secondsSlider.value = _seconds;

            var sec = (int)_seconds;
            var minStr = (_timerText.text.Split(':'))[0];
            var secStr = sec.ToString("D2");

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
            _seconds = _secondsSlider.value;
        }
    }
}
