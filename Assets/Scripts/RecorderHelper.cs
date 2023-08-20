#define UNITY_EDITOR
#if UNITY_EDITOR

using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

public class RecorderHelper : MonoBehaviour
{
    RecorderController m_RecorderController;

    void OnEnable()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        var mediaOutputFolder = Path.Combine(Application.dataPath, "..", "SampleRecordings");
        // animation output is an asset that must be created in Assets folder
        var animationOutputFolder = Path.Combine(Application.dataPath, "SampleRecordings");

        // Video
        var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        videoRecorder.name = "My Video Recorder";
        videoRecorder.Enabled = true;

        // 设置格式，除了 MP4 还支持 WebM、MOV
        videoRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        // 比特率，主要影响录制出来的视频质量，如果想要好点可以设置为 Medium 或者 High
        videoRecorder.VideoBitRateMode = VideoBitrateMode.Low;

        // 设置游戏分辨率
        videoRecorder.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        // 设置保留音频
        videoRecorder.AudioInputSettings.PreserveAudio = true;
        // 设置输出的路径、文件名
        videoRecorder.OutputFile = Path.Combine(mediaOutputFolder, "video_v") + DefaultWildcard.Take;

        // Setup Recording
        // 传入前面创建的设置
        controllerSettings.AddRecorderSettings(videoRecorder);

        // 变成受代码、手动操作控制，另外的模式分别是 SingleFrame、
        // FrameInterval、TimeInterval，和面板里面的参数是对应的
        // 根据自己需要来选即可
        controllerSettings.SetRecordModeToManual();
        // 录制时候游戏运行的目标帧率
        controllerSettings.FrameRate = 60.0f;

        // 这个可以忽略，如果设置为 true 那么会打出更详细的 log
        RecorderOptions.VerboseMode = false;
        // 准备录制需要的内部数据，具体做啥可以先不理解
        // 只需要记得在开始录制之前先执行这个指令
        // 其实就是读取 Setting 中传入的一堆 RecorderSetting
        // 根据 Setting 创建对应的 Recorder Session
        m_RecorderController.PrepareRecording();
    }


    public void StartRecording()
    {
        m_RecorderController.StartRecording();
    }
    void OnDisable()
    {
        // 停止录制
        m_RecorderController.StopRecording();
    }
}

#endif
