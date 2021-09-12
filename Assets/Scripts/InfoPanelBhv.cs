using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanelBhv : MonoBehaviour {

    private Dictionary<string, GameObject> InfoPanels = new Dictionary<string, GameObject>();

    public GameObject PanelPrefab;

    // Use this for initialization
    void Start () {
        
    }
    
    // Update is called once per frame
    void Update () {
        if (SceneManager.Instance == null || SceneManager.Instance.CurScene == null)
        {
            return;
        }
        var infos = SceneManager.Instance.CurScene.InfoPanels;

        foreach (KeyValuePair<string, GameObject> kv in InfoPanels)
        {
            if (!infos.ContainsKey(kv.Key))
            {
                Destroy(InfoPanels[kv.Key]);
                InfoPanels.Remove(kv.Key);
                
            }

        }

        foreach (KeyValuePair<string, string> kv in infos)
        {
        //    Debug.Log("INFO: " + kv.Key + " / " + kv.Value);

            if (!InfoPanels.ContainsKey(kv.Key))
            {
                var go = GameObject.Instantiate(PanelPrefab);
                go.transform.parent = transform;
                InfoPanels[kv.Key] = go;
            }
        }

        foreach (KeyValuePair<string, GameObject> kv in InfoPanels)
        {
            InfoPanels[kv.Key].GetComponent<UnityEngine.UI.Text>().text = infos[kv.Key];
        }
        
    }
}
