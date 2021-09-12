using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStateMultiDronePlayback : AbstractSceneState {
    // These Variables can be called an manipulated from within the state object
    // See how DroneIds are set in SceneDemo for example -> s.DroneIds = new HashSet<int>(droneIdsAnim); ...where s is the state object with the appropriate component added.
    public int FrameStart = 0;
    public int FrameEnd = 0;
    public bool Loop = false;
    public float PlaybackSpeed = 1.0f;
    public bool UseChoreography = true;
    public bool UseColor = false;

    public bool FadeOut = false;

    public AudioMgrBhv.ClipEnum PlayClip = AudioMgrBhv.ClipEnum.NONE;

    public bool StopClipOnExit = true;

    private bool m_loopActive = false;
    private bool m_fadingOut = false;

    // Use this for initialization
    void Start () {
        StateType = EStateType.PLAYBACK;
    }

    // Update is called once per frame
    void Update () {

    }

    public override void TransitionIn (EStateType prev)
    {
        base.TransitionIn(prev);

        if (Loop)
        {
            m_loopActive = true;
        }

        m_fadingOut = false;

        TcpMgr.Instance.CmdExtPlaybackAnim(DroneIds, FrameStart, FrameEnd, Loop, PlaybackSpeed, UseChoreography, UseColor);
        if (PlayClip != AudioMgrBhv.ClipEnum.NONE)
        {
            AudioMgrBhv.Instance.PlayClip(PlayClip);
        }
    }

    public override void TransitionOut (EStateType next)
    {

        if (PlayClip != AudioMgrBhv.ClipEnum.NONE && StopClipOnExit)
        {
            AudioMgrBhv.Instance.StopClip();
        }

        base.TransitionOut(next);
    }

    public override bool OkToTransition (EStateType next)
    {
        return true;
    }

    public override void StateUpdate ()
    {

        var idStr = Util.IdSetToString(DroneIds);
        var idl = UdpServer.Instance.Idle(DroneIds) || !WaitForIdle;

    //    Debug.Log("playback ids queried " + idStr + " -- idle: " + idl + " / time: " + m_stateTimeInS);

        if (m_transitions.ContainsKey("AUTO_NEXT") && m_stateTimeInS > 1.0f && idl)
        {
            Debug.Log("playback ; performing auto transition");
            Scene.PerformTransition("AUTO_NEXT");
        }

        if (Scene.IsTransitionRequested() && !idl)
        {
            SceneManager.Instance.SetMessage("waiting for drones to be idle");

            if (Loop && m_loopActive)
            {
                SceneManager.Instance.SetMessage("waiting for drones to be idle -- cancelled loop!");
                Debug.Log("cancelled anim for " + Util.IdSetToString(DroneIds));
                TcpMgr.Instance.CmdExtHalt(DroneIds);
                m_loopActive = false;
            }

            if (FadeOut && !m_fadingOut)
            {
                m_fadingOut = true;
                TcpMgr.Instance.CmdExtColorGradient(DroneIds, new Vector3(10f, 0f, 0f), new Vector3(0f, 0f, 0f), 1f);
            }
        }
        else if (Scene.IsTransitionRequested() && m_exitCommandsPresent && !m_exitCommandsDone)
        {
            DoBeforeExitDelegate();
            m_exitCommandsDone = true;
        }
        else if (Scene.IsTransitionRequested())
        {
            
            Scene.PerformTransition(Scene.TransitionRequested);
            Scene.TransitionRequested = "";
        }
    }
}
