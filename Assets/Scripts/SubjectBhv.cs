using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubjectBhv : MonoBehaviour {

    public UnityPharus.UnityPharusManager.Subject Subject;

    // Use this for initialization
    void Start () {
        
    }
    
    // Update is called once per frame
    void Update () {
        transform.position = Subject.position;
        GetComponentInChildren<TextMesh>().text = "sub" + Subject.id;
    }
}
