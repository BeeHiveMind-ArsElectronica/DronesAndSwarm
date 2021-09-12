using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStateSingleDroneFlyRandom : AbstractSceneState {

    public int DroneId = -1;
    public float IntervalMinInS = 3.0f; 
    public float IntervalMaxInS = 7.0f;

    float m_curIntervalLength = 0.0f;

    float m_timeSinceIntervalStart = 0.0f;

    Vector3 m_curTarget;

    void GenInterval()
    {
        m_curIntervalLength = Random.value * (IntervalMaxInS - IntervalMinInS) + IntervalMinInS;
    }

    void SetTarget()
    {
        float x = Random.value * (Main.Instance.BoundingVolMax.x - Main.Instance.BoundingVolMin.x) + Main.Instance.BoundingVolMin.x;
        float y = Random.value * (Main.Instance.BoundingVolMax.y - Main.Instance.BoundingVolMin.y) + Main.Instance.BoundingVolMin.y;
        float z = Random.value * (Main.Instance.BoundingVolMax.z - Main.Instance.BoundingVolMin.z) + Main.Instance.BoundingVolMin.z;



        Vector3 d = (GameObject.Find("dt" + DroneId).transform.position - new Vector3(x, y, z));


        m_curTarget = Util.ClampToMinHeight(new Vector3(x, y, z), Main.Instance.MinimumFlightHeight);
        var tgt = Util.ConvertToGcCoords(Util.Vec3ToVec4(m_curTarget, 0.8f));
        TcpMgr.Instance.CmdExtWaypoint(Util.SingleIdToSet(DroneId), tgt, 0.0f);
    }

    // Use this for initialization
    void Start () {
        StateType = EStateType.INTERACTIVE;
        //GenInterval();
        SetTarget();
    }

    // Update is called once per frame
    void Update () {
//        float dt = Time.deltaTime;
//
//        //m_timeSinceIntervalStart += dt;
//
//    //    if (m_timeSinceIntervalStart > m_curIntervalLength)
//        
//        {
//            SetTarget();
//    //        m_timeSinceIntervalStart = 0f;
//    //        GenInterval();
//        }
    }

    public override void TransitionIn (EStateType prev)
    {
        base.TransitionIn(prev);

    }

    public override void TransitionOut (EStateType next)
    {

        base.TransitionOut(next);
    }

    public override bool OkToTransition (EStateType next)
    {
        return true;
    }

    public override void StateUpdate ()
    {
        
        //var ids = Util.SingleIdToSet(DroneId);

        var isidle = TcpMgr.Instance.IdleSingle(DroneId);
        Debug.Log("drone is idle? " + isidle);
        if (isidle && !Scene.IsTransitionRequested())
        {
            SetTarget();
        }

        if (Scene.IsTransitionRequested() && !isidle)
        {
            SceneManager.Instance.SetMessage("waiting for drone " + DroneId + " to be idle");
        }
        else if (Scene.IsTransitionRequested())
        {
            Scene.PerformTransition(Scene.TransitionRequested);
            Scene.TransitionRequested = "";
        }
    }
}
