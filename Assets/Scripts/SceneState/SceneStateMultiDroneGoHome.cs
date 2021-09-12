using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SceneStateMultiDroneGoHome : AbstractSceneState {

    public bool Staggered = false;
    private List<int> m_droneIdsList;
    private int m_staggerIndex = 0;

    // Use this for initialization
    void Start () {
        StateType = EStateType.ANIMATED;
    }

    // Update is called once per frame
    void Update () {

    }

    public override void TransitionIn (EStateType prev)
    {
        base.TransitionIn(prev);

        m_droneIdsList = DroneIds.ToList();
        m_staggerIndex = 0;

        if (!Staggered)
        {
            Debug.Log("sending drones home");
            TcpMgr.Instance.CmdExtGoHome(DroneIds);
        }
        else
        {
            TcpMgr.Instance.CmdExtGoHome(Util.SingleIdToSet(m_droneIdsList[m_staggerIndex++]));
        }
    }

    public override void StateUpdate ()
    {

        var idStr = Util.IdSetToString(DroneIds);
        var idl = UdpServer.Instance.Idle(DroneIds);

        //Debug.Log("go home ids queried " + idStr + " -- idle: " + idl + " / time: " + m_stateTimeInS);

        if (Staggered && TcpMgr.Instance.IdleSingle(m_droneIdsList[m_staggerIndex-1])
            && m_staggerIndex < m_droneIdsList.Count)
        {
            Debug.Log("Staggered WP: " + m_staggerIndex);
            TcpMgr.Instance.CmdExtGoHome(Util.SingleIdToSet(m_droneIdsList[m_staggerIndex++]));
        }

        TransitionIfRequested();
    }
}
