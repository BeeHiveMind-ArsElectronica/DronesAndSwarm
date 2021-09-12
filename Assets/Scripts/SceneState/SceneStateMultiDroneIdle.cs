using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStateMultiDroneIdle : AbstractSceneState {


    public delegate void MilestoneFn();

    public class Milestone
    {
        public Milestone(MilestoneFn t)
        {
            task = t;
        }

        public bool completed = false;
        public MilestoneFn task = () => { Debug.Log("milestone "); };
    }

    public Dictionary<string, Milestone> Milestones = new Dictionary<string, Milestone>();

    public bool IsMilestoneCompleted(string s)
    {
        return Milestones.ContainsKey(s) && Milestones[s].completed;
    }

    public void RunMilestoneTaskIfNotCompleted(string s)
    {
        if (!Milestones.ContainsKey(s))
        { 
            return;
        }
        else if (!Milestones[s].completed)
        {
            string mTxt = SceneManager.Instance.CurScene.InfoPanels.ContainsKey("milestone") ? 
                SceneManager.Instance.CurScene.InfoPanels["milestone"] : "";
            SceneManager.Instance.CurScene.SetInfo("milestone", mTxt + s + "; ");

            Debug.Log("RUN MILESTONE: " + s);

            Milestones[s].task();
            Milestones[s].completed = true;
        }
    }

    // Use this for initialization
    void Start () {
        StateType = EStateType.IDLE;
    }

    // Update is called once per frame
    void Update () {

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
        TransitionIfRequested();
    }
}
