using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System;
using System.Net;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine.Video;

namespace TeachLipSync
{
    public enum ELsMode
    {
        None = 0,
        Offline = 1,
        Online = 2
    }

    class ClientState
    {
        public Socket socket;
        public byte[] readBuff = new byte[2048];
        public int buffCount = 0;
    }

    /// <summary>
    /// 用于选择类型，是使用实时 FFT 解析口型还是远程链接深度学习模型
    /// </summary>
    enum LipSyncType
    {
        DeepL,
        FFT
    }

    public class TeacherLipsync : MonoBehaviour
    {
#region configs
        public float moveTowardsSpeed = 8;
        /// <summary>
        /// BS 可以取的最小值
        /// </summary>
        public float propertyMinValue = 0.0f;
        /// <summary>
        /// BS 最大值
        /// </summary>
        public float propertyMaxValue = 100.0f;

        /// <summary>
        /// 是否读取本地离线数据
        /// </summary>
        [SerializeField]
        private ELsMode lsMode = ELsMode.Offline;

        /// <summary>
        /// 数据的FPS
        /// </summary>
        private float dataFrameInterval = 1.0f / 30f;
#endregion

        /// <summary>
        /// 要更新的对象
        /// </summary>
        [SerializeField]
        private SkinnedMeshRenderer targetBlendShapeObject;

        [SerializeField]
        private string[] propertyNames = new string[MAXBlendValueCount];

        [SerializeField]
        private float[] propertyScales = new float[MAXBlendValueCount];

        [SerializeField]
        private float[] propertyThres = new float[MAXBlendValueCount];

        /// <summary>
        /// 视频播放器
        /// </summary>
        [SerializeField]
        private VideoPlayer videoPlayer;

        /// <summary>
        /// 更新类型，可以选择是FFT实时还是深度学习离线模型
        /// </summary>
        [SerializeField]
        private LipSyncType lsType;

        /// <summary>
        /// 连接远程深度学习模型用的 Socket
        /// </summary>
        private Socket listenfd;

        /// <summary>
        /// BS数量上限，需要手动根据模型 BS 数量进行设置
        /// </summary>
        private const int MAXBlendValueCount = 16;

        private float[] targetBlendValues = new float[MAXBlendValueCount];
        private float[] currentBlendValues = new float[MAXBlendValueCount];
        private Dictionary<string, int> vowelToIndexDict = new Dictionary<string, int>();
        private string recognizeResult;
        private int[] propertyIndexs = new int[MAXBlendValueCount];
        private float[] bsValues = new float[58];
        private bool bIsConnectionEstablished = false;

        private Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

        // bs idx
        private Dictionary<string, int> femaleTeacherBSIdxDict = new Dictionary<string, int>();

        // bs scale
        private Dictionary<string, float> femaleTeacherBSScaleDict = new Dictionary<string, float>();
        private Dictionary<string, float> femaleTeacherBSThresDict = new Dictionary<string, float>();

        public int[] arrBSDataIdx = { 33, 31, 28, 27, 26, 14, 14, 29, 29, 35, 34, 18, 18 };

        private float timeElapsed = 0.0f;

        // Start is called before the first frame update
        private void Start()
        {
            Debug.Log("Test Started!");
            // set bs idx by string array
            for (int i = 0; i < propertyNames.Length; ++i)
            {
                femaleTeacherBSIdxDict[propertyNames[i]] = i;
                femaleTeacherBSScaleDict[propertyNames[i]] = propertyScales[i];
                femaleTeacherBSThresDict[propertyNames[i]] = propertyThres[i];
            }

            // set initial bs values
            for (int i = 0; i < 58; ++i)
            {
                bsValues[i] = 0.0f;
            }

            if (lsMode == ELsMode.Online){
                SartServer();
            }
            else if(lsMode == ELsMode.Offline) {
                LoadOfflineData();
            }
            else {
                Debug.LogError("INVALID ELsMode!");
            }
        }

        // Update is called once per frame
        private void Update()
        {
            // Dont need UpdateForward yet, we shall update bs with values read from JSON directly
            // UpdateForward();

            if (lsMode == ELsMode.Offline) {
                UpdateOffline();
            }
            else if (lsMode == ELsMode.Online) {
                if (bIsConnectionEstablished && !videoPlayer.isPlaying)
                {
                    videoPlayer.Play();
                }
                if (lsType == LipSyncType.DeepL)
                {
                    UpdateFace();
                }
                else
                {
                    UpdateFaceFFT();
                }
            }
        }

        private void UpdateOffline() {
            timeElapsed += Time.deltaTime;
            if (timeElapsed > dataFrameInterval) {
                var jmpFrames = Mathf. FloorToInt(timeElapsed / dataFrameInterval);

                // TODO 更新面部数据

                timeElapsed -= jmpFrames * dataFrameInterval;
            }
            UpdateFace();
        }

        private void LoadOfflineData() {
            // TODO: 加载JSON数据
        }

        private float BSCalcWithUpperBound(float rawBSValue, float scale, float thres, float min, float max)
        {
            float bsVal;
            if (rawBSValue > thres)
            {
                bsVal = 0.0f;
            }
            else
            {
                bsVal = rawBSValue;
            }
            return Mathf.Clamp(bsVal * scale, min, max);
        }

        /// <summary>
        /// 通过 FFT 来实时分辨口型
        /// </summary>
        private void UpdateFaceFFT()
        {
        }

        /// <summary>
        /// 对 BS 数值进行处理，先进行最大值处理，然后按照倍率放大，最后再次按照给定的最小值与最大值进行 Clamp
        /// </summary>
        /// <param name="rawBSValue">初始 BS 值</param>
        /// <param name="scale">放大倍率</param>
        /// <param name="thres">原始数据的最大值</param>
        /// <param name="min">clamp 用的最小值</param>
        /// <param name="max">clamp 用的最大值</param>
        /// <returns></returns>
        private float BSCalc(float rawBSValue, float scale, float thres, float min, float max)
        {
            return Mathf.Clamp(Mathf.Max(rawBSValue, thres) * scale, min, max);
        }

        private void UpdateFace()
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (propertyNames[i] == "jawOpen")
                {
                    string tmp = propertyNames[i];
                    int idx = femaleTeacherBSIdxDict[tmp];
                    float scale = femaleTeacherBSScaleDict[tmp];
                    float thres = femaleTeacherBSThresDict[tmp];
                    targetBlendShapeObject.SetBlendShapeWeight(idx, BSCalcWithUpperBound(bsValues[arrBSDataIdx[i]], scale, thres, 0, 100));
                }
                else
                {
                    string tmp = propertyNames[i];
                    int idx = femaleTeacherBSIdxDict[tmp];
                    float scale = femaleTeacherBSScaleDict[tmp];
                    float thres = femaleTeacherBSThresDict[tmp];
                    targetBlendShapeObject.SetBlendShapeWeight(idx, BSCalc(bsValues[arrBSDataIdx[i]], scale, thres, 0, 100));
                }
            }
        }

        /// <summary>
        /// 解析 JSON 字符串用
        /// </summary>
        /// <param name="jsonTxt">原始 JSON 字符串</param>
        private void DeserializeExpressList(ref string jsonTxt)
        {
            JObject jsonObj = (JObject)JsonConvert.DeserializeObject(jsonTxt);
            JArray expressList = (JArray)jsonObj["ExpressList"];

            for (int i = 0; i < 58; ++i)
            {
                float tmp = (float)expressList[i]["v"] * 3;
                tmp = tmp > 100.0f ? 100.0f : tmp;
                tmp = tmp < 0.0f ? 0.0f : tmp;
                bsValues[i] = tmp;
            }
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        private void SartServer()
        {
            Debug.Log("Server starting");
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 12345);
            listenfd.Bind(ipEp);
            listenfd.Listen(0);
            Debug.Log("Server Started!");
            listenfd.BeginAccept(AcceptCallback, listenfd);
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listenfd1 = (Socket)ar.AsyncState;
                Socket clientfd1 = listenfd1.EndAccept(ar);
                ClientState state = new ClientState();
                state.socket = clientfd1;
                clients.Add(clientfd1, state);
                bIsConnectionEstablished = true;
                clientfd1.BeginReceive(state.readBuff, state.buffCount, 2048 - state.buffCount, 0, ReceiveCallback, state);
                listenfd1.BeginAccept(AcceptCallback, listenfd1);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Console.WriteLine(e);
                throw;
            }
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                ClientState state2 = (ClientState)ar.AsyncState;
                Socket clientfd2 = state2.socket;
                int count = clientfd2.EndReceive(ar);
                state2.buffCount += count;
                if (count == 0)
                {
                    clientfd2.Close();
                    clients.Remove(clientfd2);
                    Debug.Log("socket closed");
                    return;
                }

                OnReceiveData(ref state2);
                clientfd2.BeginReceive(state2.readBuff, state2.buffCount, 2048 - state2.buffCount, 0, ReceiveCallback, state2);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Console.WriteLine(e);
                throw;
            }
        }

        private void OnReceiveData(ref ClientState state)
        {
            if (state.buffCount <= 2)
            {
                return;
            }
            Int16 bodyLength = BitConverter.ToInt16(state.readBuff, 0);
            // Debug.Log("got msg length: " + bodyLength);
            if (state.buffCount < 2 + bodyLength)
            {
                return;
            }
            // Debug.Log("trying to read buff from 2 to " + (bodyLength));
            string recvStr = System.Text.Encoding.UTF8.GetString(state.readBuff, 2, bodyLength);
            // Debug.Log("received str: " + recvStr);
            DeserializeExpressList(ref recvStr);
            int start = 2 + bodyLength;
            int count = state.buffCount - start;
            Array.Copy(state.readBuff, start, state.readBuff, 0, count);
            state.buffCount -= start;
            // Debug.Log("new buffCnt: " + state.buffCount);
        }

        /// <summary>
        /// 更新面部 BS
        /// </summary>
        protected void UpdateForward()
        {
            for (int i = 0; i < targetBlendValues.Length; ++i)
            {
                targetBlendValues[i] = 0.0f;
            }
            if (recognizeResult != null)
            {
                targetBlendValues[vowelToIndexDict[recognizeResult]] = 1.0f;
            }
            for (int k = 0; k < currentBlendValues.Length; ++k)
            {
                if (propertyIndexs[k] != -1)
                {
                    currentBlendValues[k] = Mathf.MoveTowards(currentBlendValues[k], targetBlendValues[k], moveTowardsSpeed * Time.deltaTime);
                    targetBlendShapeObject.SetBlendShapeWeight(propertyIndexs[k], Mathf.Lerp(propertyMinValue, propertyMaxValue, currentBlendValues[k]));
                }
            }
        }
    }

}
