using System;
using System.Net;
using UnityEngine;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.Video;

class ClientState
{
    public Socket socket;
    public byte[] readBuff = new byte[2048];
    public int buffCount = 0;
}

public class TeacherLipsync : MonoBehaviour
{
    public float moveTowardsSpeed = 8;
    public float propertyMinValue = 0.0f;
    public float propertyMaxValue = 100.0f;
    
    [SerializeField]
    private SkinnedMeshRenderer targetBlendShapeObject;
    [SerializeField]
    private string[] propertyNames = new string[MAXBlendValueCount];

    [SerializeField]
    private VideoPlayer videoPlayer;
    
    private Socket listenfd;

    private const int MAXBlendValueCount = 16;

    private float[] targetBlendValues = new float[MAXBlendValueCount];
    private float[] currentBlendValues = new float[MAXBlendValueCount];
    private Dictionary<string, int> vowelToIndexDict = new Dictionary<string, int>();
    private string recognizeResult;
    private int[] propertyIndexs = new int[MAXBlendValueCount];
    private float[] bsValues = new float[58];
    private bool bIsConnectionEstablished = false;

    private Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

    private Dictionary<string, int> femailTeacherBSIdx = new Dictionary<string, int>() {
        { "jawOpen", 0 },
        { "mouthFunnel", 1 },
        { "mouthPucker", 2 },
        { "mouthShrugUpper", 3 },
        { "mouthShrugLower", 4 },
        { "mouthUpperUpLeft", 5 },
        { "mouthUpperUpRight", 6 },
        { "mouthStretchLeft", 7 },
        { "mouthStretchRight", 8 },
        { "mouthRollLower", 9 },
        { "mouthRollUpper", 10 },
        { "mouthSmileLeft", 11 },
        { "mouthSmileRight", 12 },
        { "eyeClose", 13 },
        { "smile", 14 }
    };
    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("Test Started!");
        for (int i = 0; i < 58; ++i)
        {
            bsValues[i] = 0.0f;
        }
        SartServer();
    }

    // Update is called once per frame
    private void Update()
    {
        // Dont need UpdateForward yet, we shall update bs with values read from JSON directly
        // UpdateForward();
        if (bIsConnectionEstablished && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
        UpdateFace();
    }

    private void UpdateFace()
    {
        // jawOpen
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["jawOpen"], bsValues[33] * 1.1f);
        // mouthFunnel
        // targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthFunnel"], bsValues[31]);
        // mouthPucker
        // targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthPucker"], bsValues[28]);
        // mouthShrugUpper
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthShrugUpper"], bsValues[27]);
        // mouthShrugLower
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthShrugLower"], bsValues[26]);
        // mouthRollLower
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthRollLower"], bsValues[34]);
        // mouthRollUpper
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthRollUpper"], bsValues[35]);
        // mouthUpperUpLeft
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthUpperUpLeft"], bsValues[14]);
        // mouthUpperUpRight
        // targetBlendShapeObject.SetBlendShapeWeight(9, bsValues[15]);
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthUpperUpRight"], bsValues[14]);
        // mouthStretchLeft
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthStretchLeft"], bsValues[29] * 0.8f);
        // mouthStretchRight
        // targetBlendShapeObject.SetBlendShapeWeight(11, bsValues[30]);
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthStretchRight"], bsValues[29] * 0.8f);
        // mouthSmileLeft
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthSmileLeft"], bsValues[18]);
        // mouthSmileRight
        // targetBlendShapeObject.SetBlendShapeWeight(13, bsValues[19]);
        targetBlendShapeObject.SetBlendShapeWeight(femailTeacherBSIdx["mouthSmileRight"], bsValues[18]);
    }

    private void DeserializeExpressList(ref string jsonTxt)
    {
        JObject jsonObj = (JObject) JsonConvert.DeserializeObject(jsonTxt);
        JArray expressList = (JArray) jsonObj["ExpressList"];

        for (int i = 0; i < 58; ++i)
        {
            float tmp = (float)expressList[i]["v"] * 3;
            tmp = tmp > 100.0f ? 100.0f : tmp;
            tmp = tmp < 0.0f ? 0.0f : tmp;
            bsValues[i] = tmp;
        }
        
    }

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
            Socket listenfd1 = (Socket) ar.AsyncState;
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
            ClientState state2 = (ClientState) ar.AsyncState;
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
