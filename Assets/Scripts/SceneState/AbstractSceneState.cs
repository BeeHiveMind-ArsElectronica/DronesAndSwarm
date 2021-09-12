using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbstractSceneState : MonoBehaviour {

    public HashSet<int> DroneIds = new HashSet<int>();

    public AbstractScene Scene { get; set; }


    public delegate void AdditionalCommandsDelegate();
    
    public AdditionalCommandsDelegate DoOnEnterDelegate =     ()=>{/*Debug.Log("entering state!");*/};
    public AdditionalCommandsDelegate DoOnExitDelegate =     ()=>{/*Debug.Log("exiting state!");*/};

    protected AdditionalCommandsDelegate DoBeforeExitDelegate =     ()=>{Debug.Log("before exiting state!");};

    public AdditionalCommandsDelegate DoBeforeUpdateDelegate = ()=>{};

    public bool WaitForIdle = true;

    protected bool m_exitCommandsPresent = false;
    protected bool m_exitCommandsDone = false;

    public void SetBeforeExitDelegate(AdditionalCommandsDelegate d)
    {
        m_exitCommandsPresent = true;
        m_exitCommandsDone = false;
        DoBeforeExitDelegate = d;
    }

    public enum EStateType
    {
        NONE,
        IDLE,
        INTERACTIVE,
        PLAYBACK,
        ANIMATED,
        MIXED
    }

    public string StateName;
    public EStateType StateType;

    protected Dictionary<string, string> m_transitions = new Dictionary<string, string>();

    protected float m_stateTimeInS = 0.0f;
    protected float m_timeSinceLastIdleCheck = 0.0f;

    public const float IDLE_INTERVAL = 0.5f;


    public float StateTime() { return m_stateTimeInS; }


    public Dictionary<string, string> Transitions()
    {
        return m_transitions;
    }

    public void AddTransition(string trans, string tgt)
    {
        m_transitions.Add(trans, tgt);
    }

    public string GetOutputStateName(string tname)
    {
        if (m_transitions.ContainsKey(tname))
        {
            return m_transitions[tname];
        }

        return "NULL";
    }

    public virtual void TransitionIn (EStateType prev)
    {
        gameObject.SetActive(true);
        m_stateTimeInS = 0.0f;
        m_exitCommandsDone = false;

        DoOnEnterDelegate();
    }

    public virtual void TransitionOut (EStateType next)
    {
        DoOnExitDelegate();

        gameObject.SetActive(false);
    }

    public virtual bool OkToTransition (EStateType next)
    {
        return true;
    }

    public virtual void PreStateUpdate()
    {
        m_stateTimeInS += Time.deltaTime;
        Main.Instance.StateClockText.GetComponent<UnityEngine.UI.Text>().text = Util.SecondsToClockString(m_stateTimeInS);

        DoBeforeUpdateDelegate();
    }

    protected void TransitionIfRequested()
    {
        if (m_transitions.ContainsKey("AUTO_NEXT") && m_stateTimeInS > 1.0f && UdpServer.Instance.Idle(DroneIds))
        {
            Debug.Log("performing auto transition");
            Scene.PerformTransition("AUTO_NEXT");
        }

        if (Scene.IsTransitionRequested() && WaitForIdle && !UdpServer.Instance.Idle(DroneIds))
        {
            SceneManager.Instance.SetMessage("waiting for drones to be idle");
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

    public virtual void StateUpdate ()
    {
        TransitionIfRequested();
    }
}
