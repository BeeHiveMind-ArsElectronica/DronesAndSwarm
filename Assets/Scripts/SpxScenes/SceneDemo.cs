using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneDemo : AbstractScene {

    //public List<int> DroneIds;

    //HashSet<int> m_droneIds;

    public GameObject AnimTgt;

    void Start () {

    }

    // Update is called once per frame
    void Update () 
    {
        UpdateImpl();
    }

    //    protected virtual void UpdateImpl ()
    //    {
    //
    //    }

    protected override void Init ()
    {
        var droneIdsAnim = new List<int>();

        for (int i = 0; i < 3; i++)
            droneIdsAnim.Add(i);
        // droneIdsAnim.Add(1);
        // droneIdsAnim.Add(2);
        // droneIdsAnim.Add(3);
        // droneIdsAnim.Add(4);
        // droneIdsAnim.Add(5);


        States = new List<AbstractSceneState>();

        {
            var s = CreateStateObject("IDLE").AddComponent<SceneStateMultiDroneIdle>();
            s.AddTransition("MANUAL_NEXT", "TAKEOFF_SINGLE");
            s.DroneIds = new HashSet<int>(droneIdsAnim);
            s.SetBeforeExitDelegate(() => {Debug.Log("before exit");});
            AddState(s);
        }

        {
            var s = CreateStateObject("TAKEOFF_SINGLE").AddComponent<SceneStateMultiDroneTakeOff>();
            s.AddTransition("MANUAL_NEXT", "SINGLE_FIGURE");
            s.DroneIds = Util.SingleIdToSet(0);
            AddState(s);
        }

        {
            var s = CreateStateObject("SINGLE_FIGURE").AddComponent<SceneStateSingleDroneFlyFigure>();
            s.AddTransition("MANUAL_NEXT", "GO_HOME_SINGLE");
            s.AnimTargetObject = AnimTgt;
            s.RelativeToStartPosition = true;
            s.DroneTargetId = 0;
            AddState(s);
        }

        {
            var s = CreateStateObject("GO_HOME_SINGLE").AddComponent<SceneStateMultiDroneGoHome>();
            s.AddTransition("MANUAL_NEXT", "IDLE");
            s.DroneIds = Util.SingleIdToSet(0);
            AddState(s);
        }

        SetInitialState("IDLE");

        foreach (AbstractSceneState s in States)
        {
            s.Scene = this;
        }

    }
}
