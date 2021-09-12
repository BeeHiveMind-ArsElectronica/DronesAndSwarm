using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class VNectSwarmOSWrapper : MonoBehaviour
{
    [HideInInspector]
    public int DroneCount { get => drones.Length; }
    public bool Ready { get; private set; }

    public VNectModel vnectModel;

    [SerializeField]
    public PositionIndex[] MappedJoints;
    [SerializeField]
    public PositionIndex RefJoint;
    public Vector3 OffsetPosition = new Vector3(0.0f, 0.5f, 0.0f);
    public float Scale = 1f;

    private DroneTargetBhv[] drones;
    private Vector3[] dronePos;

    void Start()
    {
        //Init();
        dronePos = new Vector3[MappedJoints.Length];

        var diff = Main.Instance.BoundingVolMax - Main.Instance.BoundingVolMin;
        var mid = diff + Main.Instance.BoundingVolMin;
        var outerRadiusVec = mid / 2;
        
        var innerRadius = new[] { outerRadiusVec.x, outerRadiusVec.y, outerRadiusVec.z }.Min();
    }

    public void Init()
    {
        drones = GameObject.FindObjectsOfType<DroneTargetBhv>();

        if (MappedJoints.Length != DroneCount)
        {
            Debug.LogError("Mapped positions and drone count mismatch.");
            Ready = false;
        }
        else
        {
            Ready = true;
            dronePos = new Vector3[MappedJoints.Length];
        }
    }

    void Update()
    {
        //if (MappedJoints.Length != DroneCount || !Ready || vnectModel == null)
        //{
        //    return;
        //}

        Vector3 refPos = vnectModel.JointPoints[RefJoint.Int()].Pos3D;
        for (int i = 0; i < MappedJoints.Length; i++)
        {
            var map = MappedJoints[i];

            Vector3 jointPos = vnectModel.JointPoints[map.Int()].Pos3D;
            Vector3 normalizedPos = ((jointPos - refPos).normalized + OffsetPosition) * Scale;
            dronePos[i] = normalizedPos;
        }


        //m_curTarget = Util.ClampToMinHeight(new Vector3(x, y, z), Main.Instance.MinimumFlightHeight);
        //var tgt = Util.ConvertToGcCoords(Util.Vec3ToVec4(m_curTarget, 0.8f));
        //TcpMgr.Instance.CmdExtWaypoint(Util.SingleIdToSet(DroneId), tgt, 0.0f);

    }

    private void OnDrawGizmos()
    {
        foreach (var pos in dronePos)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pos, 0.05f);
            print(pos);



        }
    }
}
