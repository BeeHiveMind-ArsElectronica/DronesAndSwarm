using UnityEngine;
using System.Collections.Generic;

public class Main : MonoBehaviour {

    public int NumDrones = 1;

    public Vector3 VideoMapP0 = new Vector3(-2.5f, -3.67f, 0.0f);
    public Vector3 VideoMapP1 = new Vector3(4.65f, 3.47f, 0);

    public Vector3 BoundingVolMin = new Vector3(-3,0,-3);
    public Vector3 BoundingVolMax = new Vector3(3, 5, 3);
    public float MinimumFlightHeight = 0.75f;
    public float StandardFlightVelocity = 0.8f;

    public GameObject SceneClockText;
    public GameObject StateClockText;
    public GameObject BakeChildInfoPrefab;
    public Material BakeChildMaterial, BakeChildMaterialRev;

    public Dictionary<string, object> ParameterCache = new Dictionary<string, object>();

    // Static singleton instance
    private static Main instance;

    private bool m_transferredInitialState = false;

    public Dictionary<string, GameObject> DictDrones = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> DictDroneTargets = new Dictionary<string, GameObject>();
    public Dictionary<int, string> DroneLabelInfo = new Dictionary<int, string>();

    public enum WaypointMode
    {
        OFF,
        STREAMING,
        TIMECODE
    }

    // Static singleton property
    public static Main Instance
    {
        // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
        // otherwise we assign instance to a new component and return that
        get { return instance ?? (instance = GameObject.Find("MainObject").GetComponent<Main>()); }

    }

    #region unity callbacks
    void Start () {
    
        Application.runInBackground = true;

        InvokeRepeating("CheckIdleDict", 1.0f, 0.5f);

        SetWaypointMode(WaypointMode.OFF);
    }

    void Update () {
    
        if (!m_transferredInitialState && TcpMgr.Instance.Connected())
        {
            TcpMgr.Instance.CmdExtSetBoundingBox(
                Util.ConvertToGcCoords(Util.Vec3ToVec4(BoundingVolMin, 1.0f)), 
                Util.ConvertToGcCoords(Util.Vec3ToVec4(BoundingVolMax, 1.0f)));
            m_transferredInitialState = true;
        }

        
    }
    #endregion

    public HashSet<int> GetSelectedIds()
    {
        HashSet<int> droneIds = new HashSet<int>();
        foreach (var drone in DictDrones)
        {
            if (drone.Value.GetComponent<DroneObjectBhv>().selected)
            {
                droneIds.Add(drone.Value.GetComponent<DroneObjectBhv>().droneId);
            }
        }
        return droneIds;
    }

    public void SelectAllDrones()
    {
        foreach (var drone in DictDrones)
        {
            drone.Value.GetComponent<DroneObjectBhv>().selected = true;
        }
    }

    public void SetWaypointMode(WaypointMode mode)
    {
        if (mode == WaypointMode.STREAMING)
        {
            AnimTgtBhv[] animTargets = Object.FindObjectsOfType<AnimTgtBhv>();

            foreach (AnimTgtBhv animTarget in animTargets)
            {
                animTarget.SendWaypoints = true;
            }

            PrintWpTcInfo("[WP-ON]");
        }

        else if (mode == WaypointMode.TIMECODE)
        {
            AnimTgtBhv[] animTargets = Object.FindObjectsOfType<AnimTgtBhv>();

            foreach (AnimTgtBhv animTarget in animTargets)
            {
                animTarget.SendWaypoints = false;
            }

            PrintWpTcInfo("[TC-ON]");
            Debug.LogError("Timecode Mode Not Supported!");
        }

        else if (mode == WaypointMode.OFF)
        {
            AnimTgtBhv[] animTargets = Object.FindObjectsOfType<AnimTgtBhv>();

            foreach (AnimTgtBhv animTarget in animTargets)
            {
                animTarget.SendWaypoints = false;
            }

            PrintWpTcInfo("[WP-OFF]");
        }
    }

    private void CheckIdleDict()
    {
        var tcp = TcpMgr.Instance;
        tcp.UiCheckAllIdle();
    }

    private void PrintWpTcInfo(string msg)
    {
        GameObject go = GameObject.Find("TextSceneDetails2");

        if (go)
        {
            go.GetComponent<UnityEngine.UI.Text>().text = msg;
        }
    }
}
