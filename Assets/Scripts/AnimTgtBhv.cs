using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimTgtBhv : MonoBehaviour {

    public int targetId;
    public float yawInDeg;
    public Transform AnimTransformObject;

    public bool SendWaypoints = true;

    public Material trailMaterial;

    private TrailRenderer m_lineRenderer;

    private ChangeColorBeatDetection changeColorBeat;

    private void Start()
    {
        changeColorBeat = FindObjectOfType<ChangeColorBeatDetection>();
    }

    private void Update() {

        if (!SendWaypoints)
        {
            return;
        }
        else if (m_lineRenderer)
        {
            Destroy(m_lineRenderer);
        }

        Vector4 v;
        v.x = gameObject.transform.position.x;
        v.y = gameObject.transform.position.z;
        v.z = gameObject.transform.position.y;
        v.w = 1000;
        yawInDeg = Util.NormalizeAngle((180.0f - gameObject.transform.eulerAngles.y));
        HashSet<int> ids = new HashSet<int>();
        ids.Add(targetId);

        bool reverse = false;


        if (AnimTransformObject.childCount > 0)
        {
            var child = AnimTransformObject.GetChild(0);
            if (child.gameObject.name.Contains("reverseFlag"))
            {
                child.gameObject.GetComponent<MeshRenderer>().enabled = false;

                var revObjPos = child.transform.position;
                var revValue = revObjPos.y;
                //  Debug.Log("bot " + targetId + " rev val " + revValue);

                reverse = Util.ValueCodesReverseFlag(revValue);

            }
        }

        int flags = 0;

        if (reverse)
        {
            flags |= GcTypes.UplinkFlags.UPLINK_FLAG_REVERSE;

            gameObject.GetComponent<MeshRenderer>().material = Main.Instance.BakeChildMaterialRev;
        }
        else
        {
            gameObject.GetComponent<MeshRenderer>().material = Main.Instance.BakeChildMaterial;
        }

        // FIXME (used this to test top rotation control issues)
        //   yawInDeg = 45.0f + Mathf.Sin(Time.time/2.0f) * 10.0f;

        //    Debug.Log("bot " + targetId + " rev " + flags);
        //TcpMgr.Instance.CmdExtWaypointFollow(ids, v, yawInDeg, flags);

        Color _droneColor = changeColorBeat.GetDroneColor();

        TcpMgr.Instance.CmdExtWaypointFollowColor(ids, v, yawInDeg, _droneColor /*UI_ManualColor.MANUAL_COLOR*/, flags);
    }

    public void AnimationDone () {

    }
}