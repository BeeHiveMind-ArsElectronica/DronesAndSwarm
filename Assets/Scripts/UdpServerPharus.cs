using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpServerPharus : MonoBehaviour {

    public int port;
    public GameObject SubjectPrefab;

    public class Subject
    {
        public Vector3 position;
        public UInt32 id;
    }

    public List<Subject> Subjects;

    Thread _rcvThread;
    UdpClient _client;
    private bool _running;
    private bool _setupComplete = false;
    private string _portUdp, _ipMcast;
    // Use this for initialization
    void Start () {
        // Init();
        _portUdp = "6667";
    }

    Vector3 m_lastRnd;

    // Update is called once per frame
    void Update () 
    {
        if (!_setupComplete)
        {
            _setupComplete = true;
            //WallSetup.Instance.instanceCamera.orthographic = true;
            Init();
        }

        var r = UnityEngine.Random.insideUnitSphere;

        r.Scale(new Vector3(0.15f * Mathf.Sin(Time.realtimeSinceStartup), 0.0f, 0.05f));
        r.y = 0;
        m_lastRnd = r;

        lock (this)
        {
            for (int i = 0; i < Subjects.Count; i++)
            {
                int idx = i;

                GameObject go = GameObject.Find("sub" + Subjects[i].id);

                if (go == null)
                {
                    go = Instantiate(SubjectPrefab);
                    go.name = "sub" + Subjects[i].id;
                }

                Subject s = new Subject();
                s.id = Subjects[i].id;
                s.position = Subjects[i].position;
            //    go.GetComponent<SubjectBhv>().Subject = s;

            }

        }
    }

    private void Init()
    {

        Debug.Log("UDP SRV PHARUS INIT");
        port = int.Parse(_portUdp);
        _rcvThread = new Thread(new ThreadStart(ReceiveData));
        _rcvThread.IsBackground = true;
        _rcvThread.Start();

        Subjects = new List<Subject>();
        // sim

        var su = new Subject();
        su.id = 42;
        Subjects.Add(su);
    }


    float NextFloat(byte[] b, ref int byteIdx)
    {
        float f = BitConverter.ToSingle(b, byteIdx);
        byteIdx += 4;
        return f;
    }

    UInt32 NextUInt32(byte[] b, ref int byteIdx)
    {
        UInt32 i = BitConverter.ToUInt32(b, byteIdx);
        byteIdx += 4;
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
        _client = new UdpClient(port);

        IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);

        _running = true;

        while (_running)
        {
            try
            {
                // simulate

                lock (this) {

                    var rnd = m_lastRnd; // UnityEngine.Random.insideUnitSphere;
                //rnd.z = 0;
                var newpos = Subjects[0].position + rnd;

                //var p = Subjects[0].position;
                if (!(newpos.x < -3 || newpos.x > 3 || newpos.z < -3 || newpos.z > 3))
                {
                        Subjects[0].position = newpos;
                }


//                byte[] rcvBuf = _client.Receive(ref ip);
//
//
//                if (rcvBuf != null && rcvBuf.Length > 0)
//                {
//                    
//                }
                }

                System.Threading.Thread.Sleep(100);
            }
            catch (Exception e)
            {
                Debug.LogError("UDP PHARUS EXC " + e.ToString());
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
        if (_rcvThread.IsAlive)
        {
            _rcvThread.Abort();
        }
        _client.Close();
    }
}