using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;


using GcTypes;

public class TcpMgr : MonoBehaviour
{
    public event IntIntFloatDelegate BaseRotationSentEvent;

    public bool UseLocalGroundControl = false;

    // Static singleton instance
    private static TcpMgr instance;

    // Static singleton property
    public static TcpMgr Instance
    {
        // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
        // otherwise we assign instance to a new component and return that
        get { return instance ?? (instance = GameObject.Find("TcpMgr").GetComponent<TcpMgr>()); }

    }

    // handlig tcp update frequencies
    class StreamRate
    {
        public float maxRateInS;
        public float elapsedTimeInS;
        public float lastUpdateInS;

        public StreamRate(float maxRateInS)
        {
            this.maxRateInS = maxRateInS;
            this.elapsedTimeInS = 0.0f;
            this.lastUpdateInS = 0.0f;
        }

        /// <summary>
        /// Accumulates elapesd time and checks if passed time is greater
        /// than max. rate
        /// </summary>
        /// <returns></returns>
        public bool TimeExceeded()
        {
            elapsedTimeInS += (Time.time - lastUpdateInS);
            lastUpdateInS = Time.time;

            if (elapsedTimeInS >= maxRateInS)
            {
                elapsedTimeInS = 0;
                return true;
            }

            return false;
        }
    }
    private float m_maxFollowWpRateInS = 0.04f; // 0.04 is 25 Hz
    private Dictionary<int, StreamRate> followWaypointStreamRates;

    class NetCmdRecord
    {
        public Int32 type;
        public Int32 context;
        public Int32 intParam1;
        public Int32 intParam2;
    }

    class NetCmdRecordFloat : NetCmdRecord
    {
        public float[] floatVecParam;

        public NetCmdRecordFloat()
        {
            floatVecParam = new float[4];
        }
    }

    class NetCmdRecordDouble : NetCmdRecord
    {
        public double[] doubleVecParam;

        public NetCmdRecordDouble()
        {
            doubleVecParam = new double[2];
        }
    }

    class NetCmdRecordLong : NetCmdRecord
    {
        public long[] longVecParam;

        public NetCmdRecordLong()
        {
            longVecParam = new long[2];
        }
    }

    public struct AckInfo
    {
        public bool isAck;
        public bool overrideCmds;
        public int retries;
        public int rateInMs;

        public AckInfo(bool isAck = false, bool overrideCmds = true,
            int retries = 3, int rateInMs = 1500)
        {
            this.isAck = isAck;
            this.overrideCmds = overrideCmds;
            this.retries = retries;
            this.rateInMs = rateInMs;
        }
    }

    public struct ReceivedIdleTuple
    {
        public bool received;
        public bool idle;
        public double timeReceived;
    }

    public struct ReceivedRotationCmd
    {
        public bool success;
        public bool processed;
    }

    public string ServerIp;
    public GameObject InfoText;

    TcpClient m_client;
    Thread m_tcpThread;
    bool m_setupComplete = false;
    bool m_running = false;

    const int PORT = 8000;

    private int m_pingSequence = 0;
    private double m_pingReqTimeInS = -1.0;
    private double m_lastPingLatencyInUs = -1.0;
    private double m_maxPingLatencyInUs = -1.0;
    private double m_pingAttemptTimeInS = 0.0;
    private double m_pingSuccessTimeInS = 0.0;

    private Stopwatch m_stopwatch = new Stopwatch();

    private double m_timeCache = 0.0;
    const double PING_TIMEOUT_IN_S = 1.0;
    const double PING_INTERVAL_IN_S = 2.0;
    const double CONNECTION_TIMEOUT_IN_S = 60.0;

    public const byte TIMECODE_SUPPRESS_WAYPOINT = (1 << 7);
    public const byte TIMECODE_SUPPRESS_LAYER0 =   (1 << 0);
    public const byte TIMECODE_SUPPRESS_LAYER1 =   (1 << 1);

    ConcurrentUtil.ConcurrentDict<int, ReceivedIdleTuple> m_idleDict = new ConcurrentUtil.ConcurrentDict<int, ReceivedIdleTuple>();
    public ConcurrentUtil.ConcurrentDict<int, int> rotationCmdDict = new ConcurrentUtil.ConcurrentDict<int, int>();

    public ConcurrentUtil.ConcurrentDict<int, ReceivedIdleTuple> IdleDict()
    {
        return m_idleDict;
    }

    public string IdleDictString()
    {

        string fullTxt = "";
        string stale = "";

        var dc = m_idleDict;



        List<int> keys = new List<int>(dc.Keys());
        foreach (var k in keys)
        {
            // var set = TcpMgr.BitmaskToIdSet(k);
            var state = dc[k].idle;

            double diff = m_timeCache - dc[k].timeReceived;

            var txt = "";

            // bool frst = true;
            // foreach (int i in set)
            // {
            //     if (frst)
            //     {
            //         frst = false;
            //     } 
            //     else
            //     {
            //         txt += ",";
            //     }

            //     txt += i;
            // }

            //    while (txt.Length < 14) { txt += " "; }
            txt += k + (state ? "idle" : "NOT idle");

            //    fullTxt += txt + "\n";

            if (diff > 5.0)
            {
                string line = "<color=#999999>" + txt + "</color>\n";
                stale += line;
            }
            else if (state)
            {
                string line = "<color=#00ff00>" + txt + "</color>\n";
                fullTxt += line;
            }
            else
            {
                string line = "<color=#ddaa00>" + txt + "</color>\n";
                fullTxt += line;
            }
        }

        return fullTxt + stale;
    }

    public bool IdleSingle(int id)
    {
        HashSet<int> ids = new HashSet<int>();
        ids.Add(id);
        // return Idle(ids);

        return UdpServer.Instance.Idle(ids);
    }

    void Awake()
    {
        //        
        //        m_idleDict = 
        //            new Dictionary<HashSet<int>, ReceivedIdleTuple>(comp);
        //
        //        UnityEngine.Debug.Log("initialized dict");

    }

    // Use this for initialization
    void Start()
    {
        followWaypointStreamRates = new Dictionary<int, StreamRate>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_setupComplete)
        {
            m_setupComplete = true;
            Init();
        }

        lock (this)
        {
            m_timeCache = Time.time;
        }
        Ping();

        if (!Connected())
        {
            InfoText.GetComponent<UnityEngine.UI.Text>().text =
                "NOT CONNECTED";
            InfoText.GetComponent<UnityEngine.UI.Text>().color = Color.red;
        }

    }

    public bool Connected()
    {
        try
        {
            return m_client.Connected;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning(e.ToString());
            return false;
        }
    }

    void Init()
    {
        //        m_tcpThread = new Thread(new ThreadStart(TcpLoop));
        //        m_tcpThread.IsBackground = true;
        //        m_tcpThread.Start();

        var ip = ServerIp;
        if (UseLocalGroundControl)
        {
            ip = "127.0.0.1";
        }

        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), PORT);
        m_client = new TcpClient();

        var result = m_client.BeginConnect(IPAddress.Parse(ip), PORT, null, null);

        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

        if (!success)
        {
            m_client.Close();
            throw new Exception("Failed to connect.");
        }

        // we have connected
        m_client.EndConnect(result);

        //m_client.Connect(ep);

        UnityEngine.Debug.Log("connected?");

        m_running = true;
        m_tcpThread = new Thread(new ThreadStart(ReceiveData));
        m_tcpThread.IsBackground = true;
        m_tcpThread.Start();

        m_stopwatch.Start();
    }

    void ProcessNetCmdResponse(byte[] buf)
    {
        var nc = new NetCmdRecordFloat();

        nc.type = BitConverter.ToInt32(buf, 0);
        nc.context = BitConverter.ToInt32(buf, 4);
        nc.intParam1 = BitConverter.ToInt32(buf, 8);
        nc.intParam2 = BitConverter.ToInt32(buf, 12);

        nc.floatVecParam[0] = BitConverter.ToSingle(buf, 16);
        nc.floatVecParam[1] = BitConverter.ToSingle(buf, 20);
        nc.floatVecParam[2] = BitConverter.ToSingle(buf, 24);
        nc.floatVecParam[3] = BitConverter.ToSingle(buf, 28);

        switch ((int)nc.type)
        {
            case (int)GcTypes.NetCmdType.CMD_EXT_CHECK_ALL_IDLE:
                // var idSet = BitmaskToIdSet(nc.context);

                // string s = "";

                // foreach (int i in idSet)
                // {
                //     s += i + ", ";
                // }

                //UnityEngine.Debug.Log("IDLE: " + nc.intParam1 + " for " + nc.context);

                ReceivedIdleTuple rit;
                rit.received = true;
                rit.idle = nc.intParam1 == 0 ? false : true;
                rit.timeReceived = m_timeCache;
                m_idleDict[nc.context] = rit;
                break;

            case (int)GcTypes.NetCmdType.PING_RESPONSE:
                lock (this)
                {
                    if (m_pingReqTimeInS < 0.0)
                    {
                        UnityEngine.Debug.LogError("Stray ping response received (not waiting for response)");
                        return;
                    }
                    if (nc.intParam1 != m_pingSequence)
                    {
                        UnityEngine.Debug.LogError("Server ping failed, bad response sequence (local: )" + m_pingSequence);
                        return;
                    }

                    m_lastPingLatencyInUs = (m_stopwatch.Elapsed.TotalSeconds - m_pingReqTimeInS) * 1000.0 * 1000.0;

                    m_maxPingLatencyInUs = Math.Max(m_maxPingLatencyInUs, m_lastPingLatencyInUs);
                    m_pingSuccessTimeInS = m_timeCache;
                    m_pingReqTimeInS = -1.0;
                    ++m_pingSequence;

                    //UnityEngine.Debug.Log("ping OK " + m_pingSequence + " : " + m_lastPingLatencyInUs + " us ");
                }
                break;

            case (int)GcTypes.NetCmdType.ACK_CMD_TIMEOUT:
                CheckVehicleCmdSuccess(nc, 0);
                UnityEngine.Debug.LogWarning(
                    "Command of type " + ((NetCmdType)nc.intParam1).ToString() +
                    " timed out for ID " + nc.context);



                break;

            case (int)GcTypes.NetCmdType.ACK_CMD_SUCCESS:
                CheckVehicleCmdSuccess(nc, 1);
                UnityEngine.Debug.Log(
                    "Command of type " + ((NetCmdType)nc.intParam1).ToString() +
                    " was succesfull for ID " + nc.context);
                break;

            default:
                UnityEngine.Debug.LogWarning("unknown net command response type " + nc.type);
                break;
        }
    }

    void ReceiveData()
    {
        while (m_running)
        {
            byte[] buffer = new byte[1024];
            var stream = m_client.GetStream();
            if (stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                //    UnityEngine.Debug.Log("Read " + bytesRead);

                if (m_setupComplete)
                {
                    ProcessNetCmdResponse(buffer);
                }
            }

        }
    }

    public void OnApplicationQuit()
    {
        // this is called here to ensure execution on decommissioning
        CmdExtTimecodeActive(false);

        UnityEngine.Debug.Log("Destroying TcpMgr");
        m_running = false;
        if (m_tcpThread.IsAlive)
        {
            m_tcpThread.Abort();
        }
        m_client.Close();
    }

    void WriteData(NetCmdRecord nc)
    {

        byte[] outBytes = new byte[32];


        byte[] bType = BitConverter.GetBytes(nc.type);
        Buffer.BlockCopy(bType, 0, outBytes, 0, 4);
        byte[] bContext = BitConverter.GetBytes(nc.context);
        Buffer.BlockCopy(bContext, 0, outBytes, 4, 4);
        byte[] bIntParam1 = BitConverter.GetBytes(nc.intParam1);
        Buffer.BlockCopy(bIntParam1, 0, outBytes, 8, 4);
        byte[] bIntParam2 = BitConverter.GetBytes(nc.intParam2);
        Buffer.BlockCopy(bIntParam2, 0, outBytes, 12, 4);

        int offs = 16;

        if (nc is NetCmdRecordFloat)
        {
            foreach (float f in (nc as NetCmdRecordFloat).floatVecParam)
            {
                byte[] curBytes = BitConverter.GetBytes(f);
                Buffer.BlockCopy(curBytes, 0, outBytes, offs, 4);
                offs += 4;
            }
        }
        else if (nc is NetCmdRecordDouble)
        {
            foreach (double f in (nc as NetCmdRecordDouble).doubleVecParam)
            {
                byte[] curBytes = BitConverter.GetBytes(f);
                Buffer.BlockCopy(curBytes, 0, outBytes, offs, 8);
                offs += 8;
            }
        }
        else if (nc is NetCmdRecordLong)
        {
            foreach (long f in (nc as NetCmdRecordLong).longVecParam)
            {
                byte[] curBytes = BitConverter.GetBytes(f);
                Buffer.BlockCopy(curBytes, 0, outBytes, offs, 8);
                offs += 8;
            }
        }


        if (Connected())
        {
            int sent = m_client.Client.Send(outBytes);

            if (((GcTypes.NetCmdType)nc.type) != GcTypes.NetCmdType.CMD_EXT_CHECK_ALL_IDLE
            && ((GcTypes.NetCmdType)nc.type) != GcTypes.NetCmdType.PING_REQUEST)
            {
                //                UnityEngine.Debug.Log("sent " + sent);
            }
        }

    }

    public void CmdDroneSetManualTarget(int droneAnimId, Vector4 manualTargetInGpsCoords)
    {
        NetCmdRecordFloat nc = new NetCmdRecordFloat();
        nc.type = (int)GcTypes.NetCmdType.CMD_SET_MANUAL_TARGET;
        nc.context = droneAnimId;
        nc.floatVecParam[0] = manualTargetInGpsCoords.x;
        nc.floatVecParam[1] = manualTargetInGpsCoords.y;
        nc.floatVecParam[2] = manualTargetInGpsCoords.z;
        nc.floatVecParam[3] = manualTargetInGpsCoords.w;

        if (Connected())
        {
            WriteData(nc);
        }
    }

    public void CmdDroneSetHomeToPosition(Dictionary<int, Vector4> idPositionAndRotation)
    {
        foreach (KeyValuePair<int, Vector4> item in idPositionAndRotation)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_SET_HOME_POSITION_COORDINATE;
            nc.context = item.Key;
            nc.floatVecParam[0] = item.Value.x;
            nc.floatVecParam[1] = item.Value.y;
            nc.floatVecParam[2] = item.Value.z;
            nc.floatVecParam[3] = item.Value.w;

            if (Connected())
            {
                print("Set Home: " + nc.context);
                WriteData(nc);
                SetPreliminaryNonIdle(nc.context);
            }

        }
    }

    public void CmdDroneSetHomeToCurrentPosition(HashSet<int> ids)
    {
        foreach (int id in ids)
        {
            var droneTgt = GameObject.Find("dt" + id);
            if (droneTgt == null)
            {
                continue;
            }
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_SET_HOME_POSITION_COORDINATE;
            nc.context = id;
            var homeCoordinate =
                new Vector4(droneTgt.transform.position.x, droneTgt.transform.position.z,
                droneTgt.transform.position.y, droneTgt.transform.rotation.eulerAngles.y);
            nc.floatVecParam[0] = homeCoordinate.x;
            nc.floatVecParam[1] = homeCoordinate.y;
            nc.floatVecParam[2] = homeCoordinate.z;
            nc.floatVecParam[3] = homeCoordinate.w;

            if (Connected())
            {
                WriteData(nc);
                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void UiGoExternal()
    {
        int numDrones = Main.Instance.NumDrones;

        HashSet<int> ids = new HashSet<int>();
        for (int i = 0; i < numDrones; i++)
        {
            ids.Add(i);
        }

        CmdExtGoExternal(ids);
    }

    public void UiCheckAllIdle()
    {
        int numDrones = Main.Instance.NumDrones;

        HashSet<int> ids = new HashSet<int>();
        for (int i = 0; i < numDrones; i++)
        {
            ids.Add(i);
        }

        CmdExtCheckAllIdle(ids);
    }

    void SetPreliminaryNonIdle(int context)
    {
        ReceivedIdleTuple rit;
        rit.received = false;
        rit.idle = false;
        rit.timeReceived = m_timeCache;
        m_idleDict[context] = rit;

        //        UnityEngine.Debug.Log("prelim non idle set for " + Util.IdSetToString(TcpMgr.BitmaskToIdSet(context)));
    }

    public void CmdExtGoExternal(HashSet<int> ids)
    {
        foreach (int id in ids)
        {
            var nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_GO_EXTERNAL;
            nc.context = id;// IdSetToBitmask(ids);

            if (Connected())
            {
                //print("Go External: " + id);
                WriteData(nc);
                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtGoHome(HashSet<int> ids)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_GO_HOME;
            nc.context = id;// IdSetToBitmask(ids);


            if (Connected())
            {
                WriteData(nc);
                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtGoHomeAtPosition(HashSet<int> ids)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_GO_HOME_AT_POSITION;
            nc.context = id;// IdSetToBitmask(ids);

            if (Connected())
            {
                WriteData(nc);
                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtPause(HashSet<int> ids, float lengthInS)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_PAUSE;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = lengthInS;
            if (Connected())
            {
                WriteData(nc);
            }
        }
    }

    public void CmdExtColor(HashSet<int> ids, Vector3 color, float lengthInS)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat ncParam = new NetCmdRecordFloat();
            ncParam.type = (int)GcTypes.NetCmdType.EXT_PARAM_1;
            ncParam.context = IdSetToBitmask(ids);
            ncParam.floatVecParam[0] = color.x;
            ncParam.floatVecParam[1] = color.y;
            ncParam.floatVecParam[2] = color.z;
            ncParam.floatVecParam[3] = 0;

            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_COLOR;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = lengthInS;

            if (Connected())
            {
                WriteData(ncParam);
                WriteData(nc);
            }
        }
    }

    public void CmdExtColorGradient(HashSet<int> ids, Vector3 color1, Vector3 color2, float lengthInS)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat ncParam = new NetCmdRecordFloat();
            ncParam.type = (int)GcTypes.NetCmdType.EXT_PARAM_1;
            ncParam.context = id;//IdSetToBitmask(ids);
            ncParam.floatVecParam[0] = color1.x;
            ncParam.floatVecParam[1] = color1.y;
            ncParam.floatVecParam[2] = color1.z;
            ncParam.floatVecParam[3] = 0;

            NetCmdRecordFloat ncParam2 = new NetCmdRecordFloat();
            ncParam2.type = (int)GcTypes.NetCmdType.EXT_PARAM_2;
            ncParam2.context = id;//IdSetToBitmask(ids);
            ncParam2.floatVecParam[0] = color2.x;
            ncParam2.floatVecParam[1] = color2.y;
            ncParam2.floatVecParam[2] = color2.z;
            ncParam2.floatVecParam[3] = 0;

            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_COLOR_GRADIENT;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = lengthInS;

            if (Connected())
            {
                WriteData(ncParam);
                WriteData(ncParam2);
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    private NetCmdRecordLong GetAckCmdParam(int id, AckInfo ackInfo)
    {
        NetCmdRecordLong ncParam = new NetCmdRecordLong();
        ncParam.type = (int)GcTypes.NetCmdType.EXT_PARAM_ACK;

        ncParam.context = id;

        if (!ackInfo.isAck) return ncParam; // no need to continue

        if (ackInfo.isAck) ncParam.intParam1 |= 1 << 0;
        if (ackInfo.overrideCmds) ncParam.intParam1 |= 1 << 1;

        ncParam.longVecParam[0] = ackInfo.retries;
        ncParam.longVecParam[1] = ackInfo.rateInMs;

        return ncParam;
    }

    public void CmdExtText(HashSet<int> ids, string inputMsg, AckInfo ackInfo)
    {
        foreach (int id in ids)
        {
            NetCmdRecordLong nc = new NetCmdRecordLong();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_TEXT;
            nc.context = id;
            nc.intParam1 = 0; // TODO: reserved for layer and other infromation

            // fill with whitespace
            string msg = inputMsg;
            int missingChars = Mathf.Clamp(20 - inputMsg.Length, 0, 20);
            for (int i = 0; i < missingChars; i++)
                msg += "\0";

            byte[] sub1 = Encoding.Default.GetBytes(msg.Substring(0, 4));
            nc.intParam2 = BitConverter.ToInt32(sub1, 0);


            byte[] sub2 = Encoding.Default.GetBytes(msg.Substring(4, 8));
            nc.longVecParam[0] = BitConverter.ToInt64(sub2, 0);

            byte[] sub3 = Encoding.Default.GetBytes(msg.Substring(12, 8));
            nc.longVecParam[1] = BitConverter.ToInt64(sub3, 0);

            if (Connected())
            {
                WriteData(GetAckCmdParam(id, ackInfo));
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtVideo(HashSet<int> ids, int vid, int startFrame, int endFrame, int brightness,
        int layer, int mapping, int effect, Vector4 dims, float rz, AckInfo ackInfo, GcTypes.VideoCmdType cmd = GcTypes.VideoCmdType.START_ONCE)
    {
        UnityEngine.Debug.Log("video cmd: " + Util.IdSetToString(ids) + " : " + vid + " " + startFrame + " " + endFrame + " " + brightness + " "
                + layer + " " + mapping + " " + effect + " " + dims.ToString() + " " + rz + " ");

        foreach (int id in ids)
        {
            NetCmdRecordFloat ncParam2 = new NetCmdRecordFloat();
            ncParam2.type = (int)GcTypes.NetCmdType.EXT_PARAM_2;
            ncParam2.context = id;//IdSetToBitmask(ids);
            ncParam2.intParam1 = (int)cmd;

            NetCmdRecordFloat ncParam = new NetCmdRecordFloat();
            ncParam.type = (int)GcTypes.NetCmdType.EXT_PARAM_1;
            ncParam.context = id;//IdSetToBitmask(ids);
            ncParam.floatVecParam[0] = dims.x;
            ncParam.floatVecParam[1] = dims.y;
            ncParam.floatVecParam[2] = dims.z;
            ncParam.floatVecParam[3] = dims.w;
            ncParam.intParam1 = (int)(rz * 100.0);
            ncParam.intParam2 = effect;

            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_VIDEO;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = startFrame;
            nc.floatVecParam[1] = endFrame;
            nc.floatVecParam[2] = brightness;
            nc.floatVecParam[3] = layer;
            nc.intParam1 = vid;
            nc.intParam2 = mapping;

            if (Connected())
            {
                WriteData(ncParam);
                WriteData(ncParam2);
                //UnityEngine.Debug.Log("sending ackinfo " + ackInfo.isAck);
                WriteData(GetAckCmdParam(id, ackInfo));
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtVideoCmd(HashSet<int> ids, int layer, GcTypes.VideoCmdType videoCmd, AckInfo ackInfo)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_VIDEO_CMD;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[3] = layer;
            nc.intParam1 = (int)videoCmd;

            //if (videoCmd == GcTypes.VideoCmdType.PLAY)
            //{
            //    nc.floatVecParam[0] = -1;
            //}

            if (Connected())
            {
                UnityEngine.Debug.Log("VideoCmd: sending ackinfo " + ackInfo.isAck);
                WriteData(GetAckCmdParam(id, ackInfo));
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    //public void CmdExtVideoCtrl(HashSet<int> ids, int vid, int startFrame, int endFrame, int brightness,
    //    int layer, int mapping, int effect, Vector4 dims, float rz, AckInfo ackInfo)
    //{
    //    foreach (int id in ids)
    //    {
    //        NetCmdRecordFloat ncParam = new NetCmdRecordFloat();
    //        ncParam.type = (int)GcTypes.NetCmdType.EXT_PARAM_1;
    //        ncParam.context = id;//IdSetToBitmask(ids);
    //        ncParam.floatVecParam[0] = dims.x;
    //        ncParam.floatVecParam[1] = dims.y;
    //        ncParam.floatVecParam[2] = dims.z;
    //        ncParam.floatVecParam[3] = dims.w;
    //        ncParam.intParam1 = (int)(rz * 100.0);
    //        ncParam.intParam2 = effect;

    //        NetCmdRecordFloat nc = new NetCmdRecordFloat();
    //        nc.type = (int)GcTypes.NetCmdType.CMD_EXT_VIDEO;
    //        nc.context = id;//IdSetToBitmask(ids);
    //        nc.floatVecParam[0] = startFrame;
    //        nc.floatVecParam[1] = endFrame;
    //        nc.floatVecParam[2] = brightness;
    //        nc.floatVecParam[3] = layer;
    //        nc.intParam1 = vid;
    //        nc.intParam2 = mapping;

    //        if (Connected())
    //        {
    //            WriteData(ncParam);
    //            WriteData(GetAckCmdParam(ackInfo));
    //            WriteData(nc);

    //            SetPreliminaryNonIdle(nc.context);
    //        }
    //    }
    //}

    public void CmdExtVehicleCmd(HashSet<int> ids, int cmdType, int cmdParam, Vector4 values, AckInfo ackInfo)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat ncParam = new NetCmdRecordFloat();
            ncParam.context = id;
            ncParam.type = (int)GcTypes.NetCmdType.CMD_EXT_VEHICLECMD;
            ncParam.intParam1 = cmdType;
            ncParam.intParam2 = cmdParam;
            ncParam.floatVecParam[0] = values.x;
            ncParam.floatVecParam[1] = values.y;
            ncParam.floatVecParam[2] = values.z;
            ncParam.floatVecParam[3] = values.w;

            if (Connected())
            {
                WriteData(GetAckCmdParam(id, ackInfo));
                WriteData(ncParam);
                SetPreliminaryNonIdle(ncParam.context);
            }

            //      RELATIVE or ABSOLUTE rotation
            if (cmdType == 1 || cmdType == 2)
            {
                if (BaseRotationSentEvent != null)
                {
                    BaseRotationSentEvent(id, cmdType, values.x);
                }
            }
        }
    }

    public void CmdExtWaypoint(HashSet<int> ids, Vector4 waypointAndSpeed, float yawInDeg, int flags = 0)
    {
        if (!m_setupComplete)
            return;

        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_WAYPOINT;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = waypointAndSpeed.x;
            nc.floatVecParam[1] = waypointAndSpeed.y;
            nc.floatVecParam[2] = waypointAndSpeed.z;
            nc.floatVecParam[3] = waypointAndSpeed.w;
            nc.intParam1 = Util.YawInDegE100(yawInDeg);
            nc.intParam2 = flags;


            if (Connected())
            {
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }
    /// <summary>
    /// CAUTION: useReducedRate = true
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="waypointAndSpeed"></param>
    /// <param name="yawInDeg"></param>
    /// <param name="flags"></param>
    /// <param name="useReducedRate"></param>
    public void CmdExtWaypointFollow(HashSet<int> ids, Vector4 waypointAndSpeed, float yawInDeg,
        int flags = 0, bool useReducedRate = false)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_WAYPOINT_FOLLOW;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = waypointAndSpeed.x;
            nc.floatVecParam[1] = waypointAndSpeed.y;
            nc.floatVecParam[2] = waypointAndSpeed.z;
            nc.floatVecParam[3] = waypointAndSpeed.w;
            nc.intParam1 = Util.YawInDegE100(yawInDeg);
            nc.intParam2 = flags;

            if (Connected())
            {
                if (useReducedRate)
                {
                    SendWaypointFollowWithReducedRate(id, nc);
                }
                else
                {
                    WriteData(nc);
                    SetPreliminaryNonIdle(nc.context);
                }
            }
        }
    }


    public void CmdExtWaypointFollowColor(HashSet<int> ids, Vector4 waypointAndSpeed, float yawInDeg,
            Vector4 color, int flags = 0, bool useReducedRate = false)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_WAYPOINT_FOLLOW_COLOR;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = waypointAndSpeed.x;
            nc.floatVecParam[1] = waypointAndSpeed.y;
            nc.floatVecParam[2] = waypointAndSpeed.z;
            nc.floatVecParam[3] = waypointAndSpeed.w;
            nc.intParam1 = Util.YawInDegE100(yawInDeg);
            nc.intParam2 = flags;

            NetCmdRecordFloat ncParam = new NetCmdRecordFloat();
            ncParam.type = (int)GcTypes.NetCmdType.EXT_PARAM_1;
            ncParam.context = id;//IdSetToBitmask(ids);
            ncParam.floatVecParam[0] = color.x;
            ncParam.floatVecParam[1] = color.y;
            ncParam.floatVecParam[2] = color.z;
            ncParam.floatVecParam[3] = color.w;


            if (Connected())
            {
                if (useReducedRate)
                {
                    SendWaypointFollowWithReducedRate(id, nc);
                }
                else
                {
                    WriteData(ncParam);
                    WriteData(nc);
                    SetPreliminaryNonIdle(nc.context);
                }
            }
        }
    }

    private void SendWaypointFollowWithReducedRate(int id, NetCmdRecordFloat nc)
    {
        if (followWaypointStreamRates.ContainsKey(id))
        {
            if (followWaypointStreamRates[id].TimeExceeded())
            {
                WriteData(nc);
                SetPreliminaryNonIdle(nc.context);
            }
        }
        else
        {
            print("INIT");
            followWaypointStreamRates.Add(id, new StreamRate(m_maxFollowWpRateInS));
        }
    }

    public void CmdExtPlaybackAnim(HashSet<int> ids, int firstFrame, int lastFrame, bool loop, float playbackSpeed,
        bool useChoreography, bool useColor)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_PLAYBACK_ANIM;
            nc.context = id;//IdSetToBitmask(ids);
            nc.intParam1 = firstFrame;
            nc.intParam2 = lastFrame;
            nc.floatVecParam[0] = loop ? 1f : 0f;
            nc.floatVecParam[1] = playbackSpeed;
            nc.floatVecParam[2] = useChoreography ? 1f : 0f;
            nc.floatVecParam[3] = useColor ? 1f : 0f;

            if (Connected())
            {
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtSetActive(HashSet<int> ids, bool active)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_SET_ACTIVE;
            nc.context = id;//IdSetToBitmask(ids);
            nc.intParam1 = active ? 1 : 0;

            if (Connected())
            {
                //print("Set Active: " + id + " to " + active);
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtResetTrackingToFrame(HashSet<int> ids, int frame)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_RESET_TRACKING_TO_FRAME;
            nc.context = id;//IdSetToBitmask(ids);
            nc.intParam1 = frame;

            if (Connected())
            {
                WriteData(nc);

                SetPreliminaryNonIdle(nc.context);
            }
        }
    }

    public void CmdExtAnimCancelLoop(HashSet<int> ids)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_ANIM_CANCEL_LOOP;
            nc.context = id;//IdSetToBitmask(ids);
            if (Connected())
            {
                WriteData(nc);
            }
        }
    }

    public void CmdExtAnimAdjustPlaybackSpeed(HashSet<int> ids, float speed)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_ANIM_ADJ_PLAYBACK_SPEED;
            nc.context = id;//IdSetToBitmask(ids);
            nc.floatVecParam[0] = speed;
            if (Connected())
            {
                WriteData(nc);
            }
        }
    }


    public void CmdExtSetBoundingBox(Vector4 bMin, Vector4 bMax)
    {

        NetCmdRecordFloat nc = new NetCmdRecordFloat();
        nc.type = (int)GcTypes.NetCmdType.EXT_PARAM_1;
        nc.floatVecParam[0] = bMin.x;
        nc.floatVecParam[1] = bMin.y;
        nc.floatVecParam[2] = bMin.z;
        nc.floatVecParam[3] = bMin.w;

        NetCmdRecordFloat nc2 = new NetCmdRecordFloat();
        nc2.type = (int)GcTypes.NetCmdType.CMD_EXT_SET_BOUNDING_BOX;
        nc2.floatVecParam[0] = bMax.x;
        nc2.floatVecParam[1] = bMax.y;
        nc2.floatVecParam[2] = bMax.z;
        nc2.floatVecParam[3] = bMax.w;

        if (Connected())
        {
            WriteData(nc);
            WriteData(nc2);
        }
    }



    public void CmdExtHalt(HashSet<int> ids)
    {
        UnityEngine.Debug.Log("Sending Halt " + Util.IdSetToString(ids));
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_HALT;
            nc.context = id;//IdSetToBitmask(ids);
            if (Connected())
            {
                WriteData(nc);
            }
        }
    }

    public void CmdExtResetHomePosToDronePos(HashSet<int> ids)
    {
        UnityEngine.Debug.Log("Resetting home pos " + Util.IdSetToString(ids));
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_RESET_HOME_POSITION;
            nc.context = id;//IdSetToBitmask(ids);
            if (Connected())
            {
                WriteData(nc);
            }
        }
    }

    public void CmdExtCheckAllIdle(HashSet<int> ids)
    {
        //foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_CHECK_ALL_IDLE;
            nc.context = 0;// id;//IdSetToBitmask(ids);
            if (Connected())
            {
                //    UnityEngine.Debug.Log("chk ");
                WriteData(nc);
            }
        }
    }

    public void CmdExtHaltAndSaveCurCmd(HashSet<int> ids)
    {
        foreach (int id in ids)
        {

            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_HALT_AND_SAVE_CUR_CMD;
            nc.context = id;//IdSetToBitmask(ids);
            if (Connected())
            {
                WriteData(nc);
            }
        }
    }

    public void CmdExtPushSavedCmd(HashSet<int> ids)
    {
        foreach (int id in ids)
        {
            NetCmdRecordFloat nc = new NetCmdRecordFloat();
            nc.type = (int)GcTypes.NetCmdType.CMD_EXT_PUSH_SAVED_CMD;
            nc.context = id;//IdSetToBitmask(ids);
            if (Connected())
            {
                WriteData(nc);
            }
        }
    }


    public void CmdExtPingRequest()
    {
        NetCmdRecordDouble nc = new NetCmdRecordDouble();
        nc.type = (int)GcTypes.NetCmdType.PING_REQUEST;
        nc.intParam1 = m_pingSequence;
        nc.doubleVecParam[0] = m_lastPingLatencyInUs;
        nc.doubleVecParam[1] = m_maxPingLatencyInUs;
        if (Connected())
        {
            WriteData(nc);
            m_pingReqTimeInS = m_stopwatch.Elapsed.TotalSeconds;
            m_pingAttemptTimeInS = m_pingReqTimeInS;
        } 
    }

    public void CmdExtTimecode(int timecode, int version, float playbackspeed, int suppressLayerMask = 0x0)
    {
        NetCmdRecordFloat nc = new NetCmdRecordFloat();
        nc.type = (int)GcTypes.NetCmdType.CMD_EXT_TIMECODE;
        nc.intParam1 = timecode;
        nc.intParam2 = version;
        nc.floatVecParam[0] = playbackspeed;
        nc.floatVecParam[1] = suppressLayerMask; // layers bitmask

        if (Connected())
        {
            WriteData(nc);
        }
    }

    public void CmdExtTimecodeActive(bool isActive)
    {
        NetCmdRecord nc = new NetCmdRecord();
        nc.type = (int)GcTypes.NetCmdType.CMD_EXT_TIMECODE_ACTIVE;
        nc.intParam1 = isActive ? 1 : 0;

        if (Connected())
        {
            WriteData(nc);
        }

        UnityEngine.Debug.Log("TC MODE " + isActive);
    }

    /// <summary>
    /// </summary>
    /// <param name="nc"></param>
    /// <param name="success">0 is timeout, 1 is successful</param>
    private void CheckVehicleCmdSuccess(NetCmdRecordFloat nc, int success)
    {
        // check if type is vehicle cmd
        if ((NetCmdType)nc.intParam1 == NetCmdType.CMD_EXT_VEHICLECMD)
        {
            rotationCmdDict[nc.context] = success;
        }
    }

    private void Ping()
    {
        lock (this)
        {
            var timeSinceLastAttempt = m_stopwatch.Elapsed.TotalSeconds - m_pingAttemptTimeInS;
            //UnityEngine.Log("time since last ping attempt " + timeSinceLastAttempt);

            if (m_pingReqTimeInS >= 0.0 && m_stopwatch.Elapsed.TotalSeconds - m_pingReqTimeInS >= PING_TIMEOUT_IN_S)
            {
                UnityEngine.Debug.LogWarning("server ping failed - no answer. " + m_pingSequence);
                m_pingReqTimeInS = -1.0;
                m_lastPingLatencyInUs = -1.0;
                m_maxPingLatencyInUs = -1.0;
                ++m_pingSequence;
            }
            if (m_stopwatch.Elapsed.TotalSeconds - m_pingAttemptTimeInS >= PING_INTERVAL_IN_S)
            {
                //        UnityEngine.Debug.Log("sending ping req");
                CmdExtPingRequest();
            }
            if (m_stopwatch.Elapsed.TotalSeconds - m_pingSuccessTimeInS >= CONNECTION_TIMEOUT_IN_S)
            {
                UnityEngine.Debug.LogError("connection timeout...");

            }

            float timeout = (float)(m_stopwatch.Elapsed.TotalSeconds - m_pingSuccessTimeInS);

            InfoText.GetComponent<UnityEngine.UI.Text>().text =
                "TCP ping: " + m_pingSequence + " / " + Util.FmtDouble(m_lastPingLatencyInUs) + " us\n"
                + "last pkt: " + Util.FmtFloat(timeout);

            if (timeout > CONNECTION_TIMEOUT_IN_S)
            {
                InfoText.GetComponent<UnityEngine.UI.Text>().color = Color.red;
            }
            else
            {
                InfoText.GetComponent<UnityEngine.UI.Text>().color = Color.white;
            }
        }

    }

    public static int IdSetToBitmask(HashSet<int> ids)
    {
        int bitmask = 0x0;
        foreach (int i in ids)
        {
            bitmask |= (0x01 << i);
        }

        return bitmask;
    }

    public static HashSet<int> BitmaskToIdSet(int bitmask)
    {
        HashSet<int> ids = new HashSet<int>();
        for (int i = 0; i < 32; i++)
        {
            bool inSet = (((uint)GcTypes.ObjectInfoFlags.FLAG_IDLE << i) & bitmask) != 0;
            if (inSet)
            {
                ids.Add(i);
            }
        }

        return ids;
    }

    //    private void TcpLoop()
    //    {
    //        
    //
    //        m_running = true;
    //
    //        while (m_running)
    //        {
    //            try
    //            {
    //                m_client.
    //            }
    //            catch (Exception e)
    //            {
    //
    //            }
    //        }
    //
    //
    //    }
}
