using UnityEngine;

public class InputManager : MonoBehaviour {

    private float counter = 0;
    // Use this for initialization
    void Start () {
    
    }
    
    // Update is called once per frame
    void Update () 
    {
        if (Input.GetKeyUp(KeyCode.N))
        {
            SceneManager.Instance.NextSceneRequested = true;
        }
        if (Input.GetKeyUp(KeyCode.J))
        {
            SceneManager.Instance.CurScene.NextStateRequested = true;
        }

        if (Input.GetKey(KeyCode.C))
        {
            if (SceneManager.Instance.selectionEnabled && counter > 0.0f)
            {
                counter += Time.deltaTime;
                if (counter >= 1.0f) // hold down for 1sec
                {
                    Main.Instance.SelectAllDrones();
                    counter = 0;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            SceneManager.Instance.selectionEnabled = !SceneManager.Instance.selectionEnabled;
            if (SceneManager.Instance.selectionEnabled)
            {
                counter += Time.deltaTime;
            }
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            counter = 0;
        }

        if (Input.GetKeyUp(KeyCode.P))
        {
            GameObject panelAttrRep = GameObject.Find("PanelAttrRep");
            float newScale = panelAttrRep.transform.localScale.x == 1.0 ? 0.0f : 1.0f;
            panelAttrRep.transform.localScale = new Vector3(newScale, newScale, newScale);
        }
    }
}
