using UnityEngine;

public class BoundingVolBhv : MonoBehaviour {

    void Update () {
        var diff = Main.Instance.BoundingVolMax - Main.Instance.BoundingVolMin;
        transform.localScale = diff;
        transform.localPosition = Main.Instance.BoundingVolMin + diff/2.0f;
    }

    void OnPreRender() {
        GL.wireframe = true;
    }
    void OnPostRender() {
        GL.wireframe = false;
    }
}
