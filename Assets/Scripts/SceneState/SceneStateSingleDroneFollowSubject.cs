using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStateSingleDroneFollowSubject : AbstractSceneState {

    public int DroneId = -1;
    public int SubjectId = 42;

    //string m_substate = "FOLLOWING";

    // Use this for initialization
    void Start () {
        StateType = EStateType.INTERACTIVE;
        DroneIds = new HashSet<int>();
        DroneIds.Add(DroneId);
    }

    // Update is called once per frame
    void Update () {

    }

    public override void TransitionIn (EStateType prev)
    {
        base.TransitionIn(prev);
    //    OscControl.Instance.SendState("/scene2", "follow_enter");
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


        //var sub = GameObject.Find("sub" + SubjectId);

        GameObject sub = null;
        foreach (KeyValuePair<int, UnityPharus.UnityPharusManager.Subject> kv in UnityPharus.UnityPharusManager.Instance.Subjects)
        {
            sub = GameObject.Find("subx" + kv.Value.id);
            if (sub != null)
            {
                break;
            }
        }

        if (sub != null)
        {
            
        

            var bhv = sub.GetComponent<SubjectBhv>();
            var tgt = sub.transform.position;

            var droneTgt = GameObject.Find("dt" + DroneId);
            var droneTgtBhv = droneTgt.GetComponent<DroneTargetBhv>();
            var subToDrone = droneTgt.transform.position - tgt;
            subToDrone.Normalize();
            subToDrone.Scale(new Vector3(0.1f, 0f, 0.1f));
        
            var newTgt = tgt + subToDrone;
            newTgt.y = 2.5f;
            newTgt = Util.ClampToBoundaries(newTgt, Main.Instance.BoundingVolMin, Main.Instance.BoundingVolMax);
            if (Vector3.Distance(droneTgt.transform.position, newTgt) > 0.5f)
            {
                var wp = new Vector4(newTgt.x, newTgt.z, newTgt.y, 0.8f);
                TcpMgr.Instance.CmdExtWaypointFollow(Util.SingleIdToSet(DroneId), wp, 0);
            } else
            {
                TcpMgr.Instance.CmdExtHalt(Util.SingleIdToSet(DroneId));
            }

        }

        if (m_transitions.ContainsKey("AUTO_NEXT") && TcpMgr.Instance.IdleSingle(DroneId))
        {
            Scene.PerformTransition("AUTO_NEXT");
        }

        TransitionIfRequested();
    }
}
