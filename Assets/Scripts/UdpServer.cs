using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpServer : MonoBehaviour {
    public event SimpleDelegate UdpServerInitialized;

    public int Port;

    public List<GcTypes.ObjectInfo> ois = new List<GcTypes.ObjectInfo>();
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
    private static UdpServer instance;
    public static UdpServer Instance
    {
        // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
        // otherwise we assign instance to a new component and return that
        get { return instance ?? (instance = GameObject.Find("UdpRcv").GetComponent<UdpServer>()); }

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

        UdpServerDebugText.GetComponent<UnityEngine.UI.Text>().text = "last pkt: " + Util.FmtFloat(TimeSinceLastPosePacket);

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

                GameObject go = null; //Main.Instance.DictDrones[] GameObject.Find("d" + objIdx);
                GameObject to = null; //GameObject.Find("dt" + objIdx);
                DroneObjectBhv dobhv = null;
                DroneTargetBhv tobhv = null;

                if (Main.Instance.DictDrones.ContainsKey("d"+objIdx))
                {
                    go = Main.Instance.DictDrones["d"+objIdx];
                }

                if (Main.Instance.DictDroneTargets.ContainsKey("dt"+objIdx))
                {
                    to = Main.Instance.DictDroneTargets["dt"+objIdx];
                }

                

                if (go == null)
                {
                    go = Instantiate(DronePrefab);
                    go.name = "d" + objIdx;
                    dobhv = go.GetComponent<DroneObjectBhv>();
                    dobhv.droneId = objIdx;
                    Main.Instance.DictDrones[go.name] = go;
                }

                if (to == null)
                {
                    to = Instantiate(DroneTargetPrefab);
                    to.name = "dt" + objIdx;
                    tobhv = to.GetComponent<DroneTargetBhv>();
                    tobhv.droneObj = dobhv;
                    Main.Instance.DictDroneTargets[to.name] = to;
                }

                if (dobhv == null)
                {
                    dobhv = go.GetComponent<DroneObjectBhv>();
                }

                if (ois.Count <= 0)
                {
                    // happens if not connected
                    return;
                }
                dobhv.objectInfo = ois[i];

            }
        }
    }

    public string IdleString()
    {
        //var txt = "";
        string fullTxt = "";
        for (int i = 0; i < Main.Instance.NumDrones; i++)
        {
            string txt = ""+ i;
            UInt32 flags ;
            lock (this)
            {
                flags = ois[i].flags;
            }
            bool idle = (flags & (uint) GcTypes.ObjectInfoFlags.FLAG_IDLE) != 0;
            if (idle)
            {
                txt+= " idle";
            }
            else
            {
                txt+= " NOT idle";
            }

            if (idle)
            {
                string line = "<color=#00ff00>"+txt+"</color>\n";
                fullTxt += line;
            }
            else
            {
                string line = "<color=#ddaa00>"+txt+"</color>\n";
                fullTxt += line;
            }


        }

        return fullTxt;
    }

    public bool Idle(HashSet<int> ids)
    {
        lock (this)
        {
            if (ids.Count == 1 && ids.Contains(-1))
            {
                for (int id = 0; id < Main.Instance.NumDrones; id++)
                {
                    if ((ois[id].flags & (uint) GcTypes.ObjectInfoFlags.FLAG_IDLE) != 1)
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (int id in ids)
                {
                    if (id >= ois.Count)
                    {
                        continue;
                    }
                    if ((ois[id].flags & (uint) GcTypes.ObjectInfoFlags.FLAG_IDLE) != 1)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void Init()
    {

        Debug.Log("UDP SRV INIT");
        //Port = int.Parse(_portUdp);
        _rcvThread = new Thread(new ThreadStart(ReceiveData));
        _rcvThread.IsBackground = true;
        _rcvThread.Start();

        if (UdpServerInitialized != null)
        {
            UdpServerInitialized();
        }
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

        Debug.Log("rcv buf len " + _client.Client.ReceiveBufferSize);

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

                        GcTypes.ObjectInfo oi = new GcTypes.ObjectInfo();

                        oi.id = NextUInt16(rcvBuf, ref byteIdx);
                        //Debug.Log("ID " + oi.id); 
                        oi.type = NextUInt8(rcvBuf, ref byteIdx);
                        oi.liveness = NextUInt8(rcvBuf, ref byteIdx);

                        oi.trackingX = NextFloat(rcvBuf, ref byteIdx);
                        oi.trackingY = NextFloat(rcvBuf, ref byteIdx);
                        oi.trackingZ = NextFloat(rcvBuf, ref byteIdx);
                        oi.trackingOrientation = NextFloat(rcvBuf, ref byteIdx);

                        oi.targetX = NextFloat(rcvBuf, ref byteIdx);
                        oi.targetY = NextFloat(rcvBuf, ref byteIdx);
                        oi.targetZ = NextFloat(rcvBuf, ref byteIdx);
                        oi.targetOrientation = NextFloat(rcvBuf, ref byteIdx);

                        oi.colorR = NextFloat(rcvBuf, ref byteIdx);
                        oi.colorG = NextFloat(rcvBuf, ref byteIdx);
                        oi.colorB = NextFloat(rcvBuf, ref byteIdx);

                        oi.timestamp = NextFloat(rcvBuf, ref byteIdx);
                        oi.trackingOrientationX = NextFloat(rcvBuf, ref byteIdx);
                        oi.trackingOrientationY = NextFloat(rcvBuf, ref byteIdx);
                        oi.trackingOrientationZ = NextFloat(rcvBuf, ref byteIdx);
                        oi.trackingOrientationW = NextFloat(rcvBuf, ref byteIdx);

                        oi.frameCount = NextUInt32(rcvBuf, ref byteIdx);
                        oi.currentState = (GcTypes.DroneState) NextInt(rcvBuf, ref byteIdx);

                        oi.flags = NextUInt32(rcvBuf, ref byteIdx);
                        oi.relativeRotation = NextFloat(rcvBuf, ref byteIdx);

                        objIdx = oi.id;// byteIdx / 72 - 1;

                        lock (this)
                        {
                            TimeSinceLastPosePacket = 0;

                            while (ois.Count < objIdx + 1)
                            {
                                Main.Instance.NumDrones = objIdx + 1;
                                ois.Add(new GcTypes.ObjectInfo());
                            }
                            
                            ois[objIdx] = oi;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("UDP EXC " + e.ToString());
            }
        }
    }

    public void OnApplicationQuit()
    {
        Debug.Log("Destroying UdpServer");
        _running = false;
        if (_rcvThread.IsAlive)
        {
            _rcvThread.Abort();
        }
        _client.Close();
    }
}