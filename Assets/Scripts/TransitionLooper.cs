using UnityEngine;
using System.Collections;

public class TransitionLooper : MonoBehaviour
{

    // Static singleton instance
    private static TransitionLooper instance;

    // Static singleton property
    public static TransitionLooper Instance
    {
        // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
        // otherwise we assign instance to a new component and return that
        get { return instance ?? (instance = GameObject.Find("TransitionLooper").GetComponent<TransitionLooper>()); }

    }

    public float TimecodeStart;
    public float TimecodeEnd;

    public UnityEngine.UI.Text TextFrom, TextTo, TextCur;
    public UnityEngine.UI.Toggle Use;

    private bool m_isRunning;
    private bool m_useOverride;
    private float m_startTime;
    private float m_loopLength;

    public void StartLoop()
    {
        m_isRunning = true;
        m_startTime = Time.realtimeSinceStartup;
        m_loopLength = TimecodeEnd - TimecodeStart;

        Main.Instance.SetWaypointMode(Main.WaypointMode.TIMECODE);
    }

    public void StopLoop()
    {
        m_isRunning = false;
        //Main.Instance.SetWaypointMode(Main.WaypointMode.OFF);
    }

    public void PauseLoop()
    {
        m_isRunning = false;
    }

    public void ResumeLoop()
    {
        m_isRunning = true;
    }

    public void FireToggleUse()
    {
        m_useOverride = Use.isOn;
    }

    // Use this for initialization
    void Start()
    {
        TextFrom.text = "" + TimecodeStart;
        TextTo.text = "" + TimecodeEnd;
        TextCur.text = "0";
        m_useOverride = Use.isOn;
    }



    
    void FixedUpdate()
    {
        if (!m_isRunning || !m_useOverride)
        {
            return;
        }

        float loopTime = Time.realtimeSinceStartup - m_startTime;
        
        while (loopTime > m_loopLength)
        {
            loopTime -= m_loopLength;
        }

        float timecode = TimecodeStart + loopTime;

        Debug.Log("TransitionLooper at " + timecode);

        TextCur.text = "" + timecode;

        TcpMgr.Instance.CmdExtTimecode((int) (timecode * 1000.0f), 0, 1.0f,
            TcpMgr.TIMECODE_SUPPRESS_WAYPOINT | TcpMgr.TIMECODE_SUPPRESS_LAYER0);
    }
}
