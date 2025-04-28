
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace DrawBattle
{

    public class PickWord : UdonSharpBehaviour
    {
        [SerializeField]
        private UdonBehaviour[] targets;

        [SerializeField]
        private Text stringHolder;

        [SerializeField] private TextMeshProUGUI textQuiz1Field;
        [SerializeField] private TextMeshProUGUI textQuiz2Field;
        [SerializeField] private TextMeshProUGUI textQuiz3Field;


        [UdonSynced, FieldChangeCallback(nameof(Word1))]
        private string _word1 = "";
        public string Word1
        {
            get => _word1;
            set
            {
                _word1 = value;
                if (textQuiz1Field != null)
                {
                    textQuiz1Field.text = value;
                }
            }
        }

        [UdonSynced, FieldChangeCallback(nameof(Word2))]
        private string _word2 = "";
        public string Word2
        {
            get => _word2;
            set
            {
                _word2 = value;
                if (textQuiz2Field != null)
                {
                    textQuiz2Field.text = value;
                }
            }
        }

        [UdonSynced, FieldChangeCallback(nameof(Word3))]
        private string _word3 = "";
        public string Word3
        {
            get => _word3;
            set
            {
                _word3 = value;
                if (textQuiz3Field != null)
                {
                    textQuiz3Field.text = value;
                }
            }
        }

        [NonSerialized]
        public string pickedWord = "";

        public void OnPickWord()
        {
            string index = stringHolder.text;
            pickedWord = index;

            foreach (var target in targets)
            {
                target.SetProgramVariable(nameof(pickedWord), pickedWord);
                target.SendCustomEvent(nameof(OnPickWord));
            }

            pickedWord = "";
        }

        public void SetWords(string q1, string q2, string q3)
        {
            Debug.Log($"SetWords: {q1}, {q2}, {q3}");
            TakeOwner();
            Word1 = q1;
            Word2 = q2;
            Word3 = q3;
            RequestSerialization();
        }

        public void TakeOwner()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }
    }

}