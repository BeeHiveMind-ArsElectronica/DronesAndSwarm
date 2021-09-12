using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpServerVideo : MonoBehaviour {

    public int Port;

    public List<GcTypes.VideoInfo> videoInfos = new List<GcTypes.VideoInfo>();
    //public int numDrones = 0;

    public GameObject DronePrefab;
    public GameObject DroneTargetPrefab;

    Thread _rcvThread;
    UdpClient _client;
    private bool _running;
    private bool _setupComplete = false;
    private string _ipMcast;

    public float TimeSinceLastPosePacket = 9999;

    public GameObject UdpServerDebugText;
    private static UdpServerVideo instance;
    public static UdpServerVideo Instance
    {
        // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
        // otherwise we assign instance to a new component and return that
        get { return instance ?? (instance = GameObject.Find("UdpRcvVideo").GetComponent<UdpServerVideo>()); }

    }


    // Use this for initialization
    void Start () {
        // Init();

        //ois = new GcTypes.ObjectInfo[Main.Instance.NumDrones];
    }

    // Update is called once per frame
    void Update () 
    {
        if (!_setupComplete)
        {
            _setupComplete = true;
            //WallSetup.Instance.instanceCamera.orthographic = true;
            Init();
        }

        UdpServerDebugText.GetComponent<UnityEngine.UI.Text>().text = "v last pkt: " + Util.FmtFloat(TimeSinceLastPosePacket);

        if (TimeSinceLastPosePacket > 0.5f)
        {
            UdpServerDebugText.GetComponent<UnityEngine.UI.Text>().color = Color.red;
        }
        else
        {
            UdpServerDebugText.GetComponent<UnityEngine.UI.Text>().color = Color.white;
        }

        lock (this)
        {
            TimeSinceLastPosePacket += Time.deltaTime;

            for (int i = 0; i < Main.Instance.NumDrones; i++)
            {
                int objIdx = i;

                lock(this)
                {

                    GameObject go = null;

                    if (Main.Instance.DictDrones.ContainsKey("d"+objIdx))
                    {
                        go = Main.Instance.DictDrones["d"+objIdx];
                    }

                    if (go != null)
                    {
                        DroneObjectBhv dobhv = go.GetComponent<DroneObjectBhv>();

                        if (dobhv != null)
                        {
                            dobhv.videoInfo = videoInfos[i];
                        }
                    }
                }

            }
        }
    }

    

    private void Init()
    {

        Debug.Log("UDP SRV VIDEO INIT");
        //Port = int.Parse(_portUdp);
        _rcvThread = new Thread(new ThreadStart(ReceiveData));
        _rcvThread.IsBackground = true;
        _rcvThread.Start();
    }


    float NextFloat(byte[] b, ref int byteIdx)
    {
        float f = BitConverter.ToSingle(b, byteIdx);
        byteIdx += 4;
        return f;
    }

    byte NextUInt8(byte[] b, ref int byteIdx)
    {
        byte i = b[byteIdx];
        byteIdx += 1;
        return i;
    }

    UInt32 NextUInt32(byte[] b, ref int byteIdx)
    {
        UInt32 i = BitConverter.ToUInt32(b, byteIdx);
        byteIdx += 4;
        return i;
    }

    UInt16 NextUInt16(byte[] b, ref int byteIdx)
    {
        UInt16 i = BitConverter.ToUInt16(b, byteIdx);
        byteIdx += 2;
        return i;
    }

    int NextInt(byte[] b, ref int byteIdx)
    {
        int i = BitConverter.ToInt32(b, byteIdx);
        byteIdx += 4;
        return i;
    }

    private void ReceiveData()
    {
        _client = new UdpClient(Port);

        IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
        
        _running = true;

        while (_running)
        {
            try
            {
                byte[] rcvBuf = _client.Receive(ref ip);


                if (rcvBuf != null && rcvBuf.Length > 0)
                {
                    

                    int objIdx = 0;
                    const int STEP_BYTES = 16;

                    for (int byteIdx = 0; byteIdx < rcvBuf.Length; )
                    {

                        GcTypes.VideoInfo vi = new GcTypes.VideoInfo();

                        vi.type = NextUInt8(rcvBuf, ref byteIdx);
                        //Debug.Log("ID " + oi.id); 
                        vi.droneId = NextUInt16(rcvBuf, ref byteIdx);
                        vi.videoId = NextUInt16(rcvBuf, ref byteIdx);
                        vi.side = NextUInt8(rcvBuf, ref byteIdx);

                        vi.startVideoFrame = NextUInt16(rcvBuf, ref byteIdx);
                        vi.startAnimFrame = NextUInt16(rcvBuf, ref byteIdx);
                        vi.stopAnimFrame = NextUInt16(rcvBuf, ref byteIdx);

                        vi.videoEffect = NextUInt8(rcvBuf, ref byteIdx);
                        vi.brightness = NextUInt16(rcvBuf, ref byteIdx);
                        vi.videoProjectionMode = NextUInt8(rcvBuf, ref byteIdx);

                        vi.x0 = NextFloat(rcvBuf, ref byteIdx);
                        vi.y0 = NextFloat(rcvBuf, ref byteIdx);
                        vi.x1 = NextFloat(rcvBuf, ref byteIdx);
                        vi.y1 = NextFloat(rcvBuf, ref byteIdx);

                        objIdx = vi.droneId;// byteIdx / 72 - 1;

                        lock (this)
                        {
                            TimeSinceLastPosePacket = 0;

                            while (videoInfos.Count < objIdx + 1)
                            {
                                //Main.Instance.NumDrones = objIdx + 1;
                                videoInfos.Add(new GcTypes.VideoInfo());
                            }
                            
                            videoInfos[objIdx] = vi;
                        }

                        if (objIdx == 0)
                        {
                            //Debug.Log(objIdx + ": " + oi.trackingX + " / " + oi.trackingY + " / " + oi.trackingZ);
//                            Debug.Log(objIdx + " MAP " + vi.x0 + " " + vi.y0 + " " + vi.x1 + " " + vi.y1);
                        }



                        //    Debug.Log(objIdx + ": " + oi.trackingX + " / " + oi.trackingY + " / " + oi.trackingZ);
                    }

                    
                }
            }
            catch (Exception e)
            {
                Debug.LogError("UDP EXC " + e.ToString());
            }
        }
    }
    public void OnDestroy()
    {


    }
    public void OnApplicationQuit()
    {
        Debug.Log("Destroying UdpServer");
        _running = false;
        if (_rcvThread != null)
        {
            if (_rcvThread.IsAlive)
            {
                _rcvThread.Abort();
            }

            _client.Close();
        }
    }
}