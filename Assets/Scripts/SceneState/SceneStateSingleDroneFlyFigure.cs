using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneStateSingleDroneFlyFigure : AbstractSceneState {

    public GameObject AnimTargetObject;
    public bool RelativeToStartPosition = false;
    GameObject m_animTargetObject = null;
    public int DroneTargetId = -1;

    bool m_animationLooping = true;

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

        if (m_animTargetObject == null)
        {
            m_animTargetObject = Instantiate(AnimTargetObject);

            m_animTargetObject.transform.SetParent(transform);

        }

        m_animTargetObject.SetActive(true);
        var anim = m_animTargetObject.GetComponent<Animator>();
        anim.SetTrigger("goToAnim");
        var bhv = m_animTargetObject.GetComponent<AnimTgtBhv>();

        if (DroneTargetId >= 0)
        {
            bhv.targetId = DroneTargetId;
        }

        if (RelativeToStartPosition)
        {
            GameObject startO = new GameObject();
            startO.name = "AnimationRelativeStartPosition";
            startO.transform.position = (GameObject.Find("dt" + bhv.targetId).transform).position;    
            m_animTargetObject.transform.SetParent(startO.transform);
        }
        bhv.SendWaypoints = true;
    //    m_animTargetObject.GetComponent<Animator>().Play(ani);
//        ani.wrapMode = WrapMode.Loop;
    }

    public override void TransitionOut (EStateType next)
    {
        m_animTargetObject.SetActive(false);

        if (RelativeToStartPosition)
        {
            Destroy(m_animTargetObject.transform.parent.gameObject);
        }

        base.TransitionOut(next);
    }

    public override bool OkToTransition (EStateType next)
    {
        return true;
    }

    public override void StateUpdate ()
    {
        var bhv = m_animTargetObject.GetComponent<AnimTgtBhv>();
        int droneId = bhv.targetId;
        

        HashSet<int> ids = new HashSet<int>();
        ids.Add(droneId);

        var anim = m_animTargetObject.GetComponent<Animator>();
    
        if (Scene.IsTransitionRequested() && anim.GetCurrentAnimatorStateInfo(0).IsName("TestAnim1"))
        {
            anim.SetTrigger("goToIdle");
            SceneManager.Instance.SetMessage("waiting for drone " + DroneTargetId + " to stop looping");
        } 
        else if (Scene.IsTransitionRequested() && !TcpMgr.Instance.IdleSingle(droneId))
        {
            bhv.SendWaypoints = false;
            SceneManager.Instance.SetMessage("waiting for drone " + DroneTargetId + " to be idle");
        }
        else if (Scene.IsTransitionRequested())
        {
            Scene.PerformTransition(Scene.TransitionRequested);
            Scene.TransitionRequested = "";
        }
    }
}

