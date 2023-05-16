using UnityEngine;

namespace LipSync
{
    public enum ELipSyncMethod { Runtime, Baked }

    public class FFTAnimLipSync : LipSync
    {
        public ELipSyncMethod lipSyncMethod;
        public AudioSource audioSource;
        public FFTWindow fftWindow = FFTWindow.Hamming;

        void Start()
        {
            InitializeRecognizer();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                audioSource.Play();
            }
            recognizeResult = runtimeRecognizer.RecognizeByAudioSource(audioSource, fftWindow);
            UpdateForward();
        }

    }

}
