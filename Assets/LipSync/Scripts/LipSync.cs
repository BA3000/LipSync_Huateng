using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LipSync
{
    public class LipSync : MonoBehaviour
    {
        public static string[] vowelsJP = { "a", "i", "u", "e", "o" };
        public static string[] vowelsCN = { "a", "e", "i", "o", "u", "v" };

        protected const int MAX_BLEND_VALUE_COUNT = 6;
        public const string recdPat = "Assets/LipSync/Editor/recd.txt";


        public ERecognizerLanguage recognizerLanguage;
        public Animator targetAnimator;
        public string[] visemePropNames = new string[MAX_BLEND_VALUE_COUNT];
        public float propertyMinValue = 0.0f;
        public float propertyMaxValue = 100.0f;

        public int windowSize = 1024;
        public float amplitudeThreshold = 0.01f;
        public float moveTowardsSpeed = 8;

        protected LipSyncRuntimeRecognizer runtimeRecognizer;
        protected string[] currentVowels;
        protected Dictionary<string, int> vowelToIndexDict = new Dictionary<string, int>();
        protected string[] propertyNames = new string[MAX_BLEND_VALUE_COUNT];

        protected string recognizeResult;
        protected string lastRecognizeResult;
        protected float[] targetBlendValues = new float[MAX_BLEND_VALUE_COUNT];
        protected float[] currentBlendValues = new float[MAX_BLEND_VALUE_COUNT];


        public void InitializeRecognizer()
        {
            switch (recognizerLanguage)
            {
                case ERecognizerLanguage.Japanese:
                    currentVowels = vowelsJP;
                    break;
                case ERecognizerLanguage.Chinese:
                    currentVowels = vowelsCN;
                    break;
            }
            for (int i = 0; i < currentVowels.Length; ++i)
            {
                vowelToIndexDict[currentVowels[i]] = i;
                propertyNames[i] = visemePropNames[i];
            }
            runtimeRecognizer = new LipSyncRuntimeRecognizer(recognizerLanguage, windowSize, amplitudeThreshold);
        }

        void OnValidate()
        {
            windowSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(windowSize, 32, 8192));
            amplitudeThreshold = Mathf.Max(0, amplitudeThreshold);
            moveTowardsSpeed = Mathf.Clamp(moveTowardsSpeed, 5, 25);
        }

        protected void UpdateForward()
        {
            if (recognizeResult != null)
            {
                targetAnimator.CrossFadeInFixedTime(propertyNames[vowelToIndexDict[recognizeResult]], 0.05f, 1);
                Debug.Log(propertyNames[vowelToIndexDict[recognizeResult]]);
            }
            else
            {
                Debug.Log(recognizeResult);
                if (lastRecognizeResult == recognizeResult)
                {
                    targetAnimator.CrossFadeInFixedTime("Default", 0f, 1);
                }
                targetAnimator.CrossFadeInFixedTime("Default", 0.05f, 1);
            }
            lastRecognizeResult = recognizeResult;
        }
    }

}