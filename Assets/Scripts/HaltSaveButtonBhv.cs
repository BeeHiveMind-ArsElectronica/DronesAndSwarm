using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HaltSaveButtonBhv : MonoBehaviour {

    public bool playing = true;

    public UnityEngine.UI.Text buttonText;

    // Use this for initialization
    void Start () {
        
    }
    
    // Update is called once per frame
    void Update () {
        
    }

    public void Reset()
    {
        playing = true;
        buttonText.text = "Halt+Save";

    }

    public void FireHaltAndSave()
    {
        if (playing)
        {
            TcpMgr.Instance.CmdExtHaltAndSaveCurCmd(Util.SingleIdToSet(-1));
            playing = false;
            buttonText.text = "Push Saved";
            AudioMgrBhv.Instance.Pause();
        }
        else
        {
            TcpMgr.Instance.CmdExtPushSavedCmd(Util.SingleIdToSet(-1));
            playing = true;
            buttonText.text = "Halt+Save";
            AudioMgrBhv.Instance.Resume();
        }
    }
}
