using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour
{
    public List<AbstractScene> SceneItems;
    public AbstractScene CurScene = null;
    protected int m_curSceneIdx = 0;
    [HideInInspector]
    public bool NextSceneRequested = false;

    public int SpecificSceneRequested = -1;

    protected UnityEngine.UI.Text m_messageText = null;
    public bool selectionEnabled = false;

    // Static singleton instance
    protected static SceneManager instance;

    // Static singleton property
    public static SceneManager Instance
    {
        // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
        // otherwise we assign instance to a new component and return that
        get { return instance ?? (instance = GameObject.FindObjectOfType<SceneManager>()); }
    }

    public void SetMessage(string msg)
    {
        m_messageText.text = msg;
        Debug.Log(msg);
    }


    public string NextSceneName()
    {
        return SceneItems[(m_curSceneIdx + 1) % SceneItems.Count].SceneName();
    }

    protected virtual void Start()
    {
        SceneItems.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            AbstractScene s = transform.GetChild(i).GetComponent<AbstractScene>();
            if (s != null)
            {
                SceneItems.Add(s);
            }
        }
        // FIXME: finding objs by name tends to cause trouble. 
        // FIXME: e.g.: it's likely that multiple gameobjects are named "TextMessage"
        m_messageText = GameObject.Find("TextMessage").GetComponent<UnityEngine.UI.Text>();
    }

    protected virtual void Update()
    {
        if (CurScene == null)
        {
            CurScene = SceneItems[m_curSceneIdx];
            CurScene.EnterScene();
        }

        if (NextSceneRequested)
        {
            CurScene.ExitScene();
            m_curSceneIdx++;
            m_curSceneIdx %= SceneItems.Count;

            if (SpecificSceneRequested != -1)
            {
                Debug.Log("scene req: " + SpecificSceneRequested);
                m_curSceneIdx = SpecificSceneRequested;
                SpecificSceneRequested = -1;
            }

            CurScene = SceneItems[m_curSceneIdx];
            CurScene.EnterScene();
            Debug.Log("CUR SCENE: " + CurScene.SceneName());
            NextSceneRequested = false;
        }
    }
}
