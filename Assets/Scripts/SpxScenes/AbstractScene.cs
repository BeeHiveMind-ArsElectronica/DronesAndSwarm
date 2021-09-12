using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;

public class AbstractScene : MonoBehaviour {

    public List<AbstractSceneState> States;

    [HideInInspector]
    public string CurrentState;

    public string SceneName()
    {
        return gameObject.name;
    }

    [HideInInspector]
    public bool NextStateRequested = false;
    [HideInInspector]
    public string TransitionRequested = "";

    public List<GcTypes.Drone> Drones;

    public Dictionary<string, string> InfoPanels = new Dictionary<string, string>();

    public List<SceneButton> SceneButtons = new List<SceneButton>();

    protected float m_sceneTimeInS = 0.0f;

    //[Tooltip("Set this to match the order of the scene in the show")]
    //public int m_sceneOffset = 0;

    public float SceneTime()
    {
        return m_sceneTimeInS;
    }

    private string m_initialState;

    #region unity callbacks
    protected virtual void Awake () {
        Init();
    }
    protected virtual void Start() { }
    protected virtual void Update()
    {
        UpdateImpl();
    }
    #endregion

    public bool IsTransitionRequested()
    {
        return TransitionRequested.Length > 0;
    }

    protected virtual void Init () 
    {
        States = new List<AbstractSceneState>();
        Drones = new List<GcTypes.Drone>();

        GameObject g0 = new GameObject();
        g0.transform.SetParent(transform);
        AbstractSceneState s0 = g0.AddComponent<AbstractSceneState>();
        s0.StateName = "STATE0";
        s0.AddTransition("MANUAL_NEXT", "STATE1");
        States.Add(s0);

        GameObject g1 = new GameObject();
        g1.transform.SetParent(transform);
        var s1 = g1.AddComponent<AbstractSceneState>();
        s1.StateName = "STATE1";
        s1.AddTransition("MANUAL_NEXT", "STATE0");
        States.Add(s1);

        SetInitialState("STATE0");

        foreach (AbstractSceneState s in States)
        {
            s.Scene = this;
        }
    }

    protected GameObject CreateStateObject(string name)
    {
        GameObject g = new GameObject();
        g.transform.SetParent(transform);
        g.name = name;
        return g;
    }

    protected void AddState(AbstractSceneState s)
    {
        s.StateName = s.gameObject.name;
        s.Scene = this;
        //s.AddTransition("HOME_OVERRIDE", "GO_HOME");
        States.Add(s);
        s.gameObject.SetActive(false);
    }

    protected virtual void UpdateImpl ()
    {
        m_sceneTimeInS += Time.deltaTime;
        Main.Instance.SceneClockText.GetComponent<UnityEngine.UI.Text>().text = Util.SecondsToClockString(m_sceneTimeInS);

        var s = StateForName(CurrentState);
        s.PreStateUpdate();
        s.StateUpdate();
    }

    public void PerformTransition (string tname) 
    {
        AbstractSceneState cur = StateForName(CurrentState);
        AbstractSceneState nxt = StateForName(cur.GetOutputStateName(tname));

        if (cur != null && nxt != null)
        {
            cur.TransitionOut(nxt.StateType);
            CurrentState = nxt.StateName;
            nxt.TransitionIn(cur.StateType);
            //Debug.Log("CUR SC STATE: " + CurrentState);
        }
    }

    protected void SetInitialState (string name)
    {
        m_initialState = name;
    }

    protected void EnterInitialState ()
    {
        CurrentState = m_initialState;
        AbstractSceneState state = StateForName(CurrentState);
        if (state == null)
        {
            return;
        }
        state.TransitionIn(AbstractSceneState.EStateType.NONE);
    }

    public AbstractSceneState StateForName(string name)
    {
        foreach (var s in States)
        {
            if (s.StateName == name)
            {
                return s;
            }
        }

        return null;
    }

    public AbstractSceneState CurrentStateAsState()
    {
        return StateForName(CurrentState);
    }

    public virtual void EnterScene()
    {
        gameObject.SetActive(true);
        m_sceneTimeInS = .0f;
        EnterInitialState();
    }

    public virtual void ExitScene()
    {
        gameObject.SetActive(false);

    }

    public void SetInfo(string panel, string info)
    {
        InfoPanels[panel] = info;
    }

    public void ClearInfo(string panel)
    {
        InfoPanels.Remove(panel);
    }
}
