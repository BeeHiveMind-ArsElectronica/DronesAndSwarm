using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : MonoBehaviour {

    public static event SimpleDelegate ResizedEvent;

    [SerializeField]
    private int m_lastScreenSize;

    public GameObject NextButtonPfb;
    public GameObject ScButtonPfb;
    public GameObject SceneMenuButtonPfb;
    public GameObject HaltSaveBtn;


    private AbstractScene m_lastScene;
    private string m_lastState;
    private bool m_waitingForNewButtons = false;

    public Dropdown SceneDrop;

    #region unity callbacks
    void Start () {

        m_lastScreenSize = Screen.width + Screen.height;

        if (SceneManager.Instance == null)
        {
            return;
        }
        List<AbstractScene> scenes = SceneManager.Instance.SceneItems;

        StartCoroutine(PopulateSceneDropDown(scenes));
    }

    void Update()
    {
        if (m_lastScreenSize != Screen.width + Screen.height)
        {
            m_lastScreenSize = Screen.width + Screen.height;
            if (ResizedEvent != null)
            {
                ResizedEvent();
            }
        }

        if (SceneManager.Instance == null ||
            SceneManager.Instance.CurScene == null ||
            m_waitingForNewButtons)
        {
            return;
        }

        if (SceneManager.Instance.CurScene != m_lastScene
            || SceneManager.Instance.CurScene.CurrentState != m_lastState)
        {
            m_lastScene = SceneManager.Instance.CurScene;
            m_lastState = SceneManager.Instance.CurScene.CurrentState;
            Rebuild();
        }
    }

    private void OnDestroy()
    {
    }
    #endregion

    #region public
    public void SceneSelected(int val)
    {
        FireSelectSceneEvent(val);
    }

    public void FireGoHome()
    {
        TcpMgr.Instance.CmdExtGoHome(Util.SingleIdToSet(-1));
    }

    public void FireNextScene()
    {
        SceneManager.Instance.NextSceneRequested = true;
    }

    public void FireSelectSceneEvent(int i)
    {
        SceneManager.Instance.SpecificSceneRequested = i;
        SceneManager.Instance.NextSceneRequested = true;
    }

    public void FireHalt()
    {
    //    var alldr = new HashSet<int>();
        // FIXME: should depend on actual drone count
        // for (int i = 0; i < Main.Instance.NumDrones; i++)
        // {
        //     alldr.Add(i);
        // }
        
        TcpMgr.Instance.CmdExtHalt(Util.SingleIdToSet(-1));

        HaltSaveBtn.GetComponent<HaltSaveButtonBhv>().Reset();
    }

    public void FireNextStopPoint()
    {
        var txt = GameObject.Find("NextStopPointInput").GetComponent<UnityEngine.UI.InputField>().text;
        var idAndDeg = txt.Split(':');

        string didTxt = idAndDeg[0];
        HashSet<int> dids = Util.ParseDrones(didTxt);
    }

    public void FireGoExternal()
    {
        TcpMgr.Instance.CmdExtGoExternal(Util.SingleIdToSet(-1));
    }

    public void FireSetHomeToAnimationPosition()
    {
        Dictionary<int, Vector4> idPosAndRot = new Dictionary<int, Vector4>();

        int id = 0;
        if (AnimatorSceneManager.Instance.CurScene != null)
        {
            AbstractAnimatorScene aas = (AbstractAnimatorScene)AnimatorSceneManager.Instance.CurScene;

            Transform bakeObj = aas.ShowFileAnimator.transform.GetChild(0);
            if (bakeObj != null)
            {
                foreach (Transform bot in bakeObj)
                {
                    Debug.Log("home pos/ id " + id + " = " + bot.gameObject.name);

                    Vector4 posAndRot =
                        new Vector4(bot.position.x, bot.position.z, bot.position.y,
                            bot.rotation.eulerAngles.y);

                    idPosAndRot.Add(id++, posAndRot);
                }
            }
        }

        HashSet<int> ids = m_lastScene.StateForName(m_lastScene.CurrentState).DroneIds;
        TcpMgr.Instance.CmdExtHalt(ids);

        TcpMgr.Instance.CmdDroneSetHomeToPosition(idPosAndRot);
    }

    public void FireSetHomePositionCoordinates()
    {
        HashSet<int> ids;

        if (m_lastScene != null)
        {
            ids = m_lastScene.StateForName(m_lastScene.CurrentState).DroneIds;
        }
        else
        {
            ids = Util.MakeIdRange(0, Main.Instance.DictDrones.Count -1);
        }
        
        TcpMgr.Instance.CmdExtHalt(ids);
        TcpMgr.Instance.CmdDroneSetHomeToCurrentPosition(ids);
    }
    #endregion

    #region private
    private IEnumerator PopulateSceneDropDown(List<AbstractScene> scenes)
    {
        // delay by one tick so SceneManager is initialized
        yield return 0;

        List<string> sceneNames = new List<string>();
        int i = 0;
        foreach (var s in scenes)
        {
            sceneNames.Add(s.SceneName());
        }

        SceneDrop.AddOptions(sceneNames);
        SceneDrop.onValueChanged.AddListener(SceneSelected);
    }

    private IEnumerator WaitBeforeNewButtons()
    {
        m_waitingForNewButtons = true;

        yield return new WaitForSeconds(1.0f);

        var st = m_lastScene.StateForName(m_lastScene.CurrentState);
        {
            int i = 0;
            foreach (var t in st.Transitions())
            {
                if (t.Key.StartsWith("MANUAL_NEXT"))
                {
                    var b = Instantiate(NextButtonPfb);
                    b.name = "NextBtn_" + i;
                    b.GetComponentInChildren<Text>().text = "NEXT: " + t.Value;
                    var bce = new Button.ButtonClickedEvent();
                    bce.AddListener(delegate { FireNextEvent(t.Key); });
                    b.GetComponent<Button>().onClick = bce;
                    b.transform.SetParent(GameObject.Find("UI_PanelButtons").transform);

                    i++;
                }
            }
        }

        m_waitingForNewButtons = false;
        yield return 0;
    }

    private void NoWaitButtons()
    {
        foreach (SceneButton sb in m_lastScene.SceneButtons)
        {
            var b = Instantiate(ScButtonPfb);

            b.name = "SceneBtn_" + sb.Name;
            b.GetComponentInChildren<Text>().text = sb.Text;
            var bce = new Button.ButtonClickedEvent();
            bce.AddListener(sb.DoOnClickDelegate);
            b.GetComponent<Button>().onClick = bce;
            b.transform.SetParent(GameObject.Find("UI_PanelButtonsTop").transform);
        }
    }

    private void Rebuild()
    {
        var canvas = gameObject.GetComponent<Canvas>();

        GameObject.Find("TextState").GetComponent<Text>().text = m_lastState;
        GameObject.Find("TextScene").GetComponent<Text>().text = m_lastScene.SceneName();


        GameObject.Find("TextNextScene").GetComponent<Text>().text = "Next Scene: " + SceneManager.Instance.NextSceneName();
        SceneManager.Instance.SetMessage("entered state " + m_lastState);

        foreach (var bToDestroy in GetComponentsInChildren<Button>())
        {
            if (bToDestroy.name.StartsWith("NextBtn") || bToDestroy.name.StartsWith("SceneBtn"))

            {
                Destroy(bToDestroy.gameObject);
            }
        }

        NoWaitButtons();
        StartCoroutine(WaitBeforeNewButtons());
    }


    private void FireNextEvent(string key)
    {
        m_lastScene.TransitionRequested = key;
    }
    #endregion
}
