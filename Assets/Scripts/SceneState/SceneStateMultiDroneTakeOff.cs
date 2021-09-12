using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStateMultiDroneTakeOff : AbstractSceneState {

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
        TcpMgr.Instance.CmdExtGoExternal(DroneIds);
    }

}
