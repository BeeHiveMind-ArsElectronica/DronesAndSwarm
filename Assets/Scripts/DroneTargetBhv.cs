using UnityEngine;
using TMPro;

public class DroneTargetBhv : MonoBehaviour
{
    public DroneObjectBhv droneObj;

    public TextMeshPro textMesh;
    public LineRenderer ObjTgtLine;
    string m_currentStateString;

    private BoundingVolBhv m_boundingVol;

    // Use this for initialization
    void Start()
    {
        m_boundingVol = FindObjectOfType<BoundingVolBhv>();
    }

    int DroneId()
    {
        DroneObjectBhv dobhv = droneObj.GetComponent<DroneObjectBhv>();
        int droneId = dobhv.droneId;
        return droneId;
    }

    // Update is called once per frame
    void Update()
    {

        transform.position =
            new Vector3(
                droneObj.objectInfo.targetX,
                droneObj.objectInfo.targetZ,
                droneObj.objectInfo.targetY);

        //StayInsideBoundingVol();

        var cs = Util.DroneStateToString(droneObj.objectInfo.currentState);
        //if (m_currentStateString != cs)      
        //    m_currentStateString = cs;
        string txt = "<b>" + droneObj.droneId + "</b> " /*+ cs + " V " + droneObj.videoInfo.videoId*/;

        string line;

        if ((droneObj.objectInfo.flags & (uint)GcTypes.ObjectInfoFlags.FLAG_IDLE) != 0)
        {
            line = "<color=#00aa00>" + txt + "</color>";

        }
        else
        {
            line = "<color=#ddaa00>" + txt + "</color>";

        }

        //if (Main.Instance.DroneLabelInfo.ContainsKey(droneObj.droneId))
        //{
        //    line += "<size=40%>s " + Main.Instance.DroneLabelInfo[droneObj.droneId] + "</size>";
        //}

        textMesh.text = line;

        bool active = (droneObj.objectInfo.flags & 0x02) != 0;

        gameObject.GetComponent<MeshRenderer>().enabled = active;
        textMesh.gameObject.SetActive(active);
    }

    private Vector3 screenPoint;
    private Vector3 offset;

    void OnMouseDown()
    {
        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        offset = gameObject.transform.position
            - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,
                Input.mousePosition.y, screenPoint.z));
    }

    void OnMouseDrag()
    {
        Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y,
            screenPoint.z);
        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;
        transform.position = cursorPosition;

        StayInsideBoundingVol();

        Vector4 v;
        v.x = gameObject.transform.position.x;
        v.y = gameObject.transform.position.z;
        v.z = gameObject.transform.position.y;
        v.w = 1000;
        //tcp.CmdDroneSetManualTarget(droneId, v);

        TcpMgr.Instance.CmdExtWaypointFollow(Util.SingleIdToSet(DroneId()), v, 0);
    }

    void OnMouseUp()
    {

    }

    private void StayInsideBoundingVol()
    {
        if (m_boundingVol)
        {
            float x = Mathf.Clamp(
                transform.position.x, Main.Instance.BoundingVolMin.x, Main.Instance.BoundingVolMax.x);
            float y = Mathf.Clamp(
                transform.position.y, Main.Instance.BoundingVolMin.y, Main.Instance.BoundingVolMax.y);
            float z = Mathf.Clamp(
                transform.position.z, Main.Instance.BoundingVolMin.z, Main.Instance.BoundingVolMax.z);

            transform.position = new Vector3(x, y, z);
        }
    }
}
