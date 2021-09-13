using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class ExtensionMethods
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static Vector3 Remap(this Vector3 value, Vector3 from1, Vector3 to1, Vector3 from2, Vector3 to2)
    {
        var n_x = (value.x - from1.x) / (to1.x - from1.x) * (to2.x - from2.x) + from2.x;
        var n_y = (value.y - from1.y) / (to1.y - from1.y) * (to2.y - from2.y) + from2.y;
        var n_z = (value.z - from1.z) / (to1.z - from1.z) * (to2.z - from2.z) + from2.z;
        return new Vector3(n_x, n_y, n_z);
    }
}

public class VNectSwarmOSWrapper : MonoBehaviour
{
    [HideInInspector]
    public int DroneCount { get => drones.Length; }

    [HideInInspector]
    public bool Ready;

    public float MinScoreThreshold = 0.3f;
    public bool Simulation = true;
    public float DroneVelocity = 0.8f;

    public bool DebugMode = true;

    public VNectModel vnectModel;


    [SerializeField]
    public PositionIndex[] MappedJoints;
    [SerializeField]
    public PositionIndex RefJoint;
    public Vector3 OffsetPosition = new Vector3(0.0f, 0.5f, 0.0f);
    public float Scale = 1f;
    public float MaxJointToRefDist = 250f;

    private DroneTargetBhv[] drones;
    private Vector3[] dronePos;
    private float innerRadius;

    public float[] yPercent;


    void Start()
    {
        if (DroneVelocity <= 0)
            DroneVelocity = Main.Instance.StandardFlightVelocity;

        Init();
        //dronePos = new Vector3[MappedJoints.Length];

    }

    public void Init()
    {
        drones = GameObject.FindObjectsOfType<DroneTargetBhv>();

        //if (MappedJoints.Length != DroneCount)
        //{
        //    Debug.LogError("Mapped positions and drone count mismatch.");
        //    Ready = false;
        //}
        //else
        //{
        //    Ready = true;
        //    dronePos = new Vector3[MappedJoints.Length];


        //    var diff = Main.Instance.BoundingVolMax - Main.Instance.BoundingVolMin;
        //    var mid = diff + Main.Instance.BoundingVolMin;
        //    var outerRadiusVec = mid / 2;

        //    innerRadius = new[] { outerRadiusVec.x, outerRadiusVec.y, outerRadiusVec.z }.Min();
        //}

        dronePos = new Vector3[MappedJoints.Length];
        yPercent = new float[MappedJoints.Length];
    }

    void Update()
    {
        if (!Simulation)
        {
            if (MappedJoints.Length != DroneCount)
            {
                return;
            }
        }

        Vector3 refPos = vnectModel.JointPoints[RefJoint.Int()].Pos3D;
        for (int i = 0; i < MappedJoints.Length; i++)
        {
            var map = MappedJoints[i];

            Vector3 jointPos = vnectModel.JointPoints[map.Int()].Pos3D;
            float score = vnectModel.JointPoints[map.Int()].score3D;
            if(score < MinScoreThreshold)
            {
                continue;
            }

            //Vector3 finalPos = (jointPos - refPos + OffsetPosition) * Scale;
            var diff = jointPos - refPos;
            diff = Vector3.ClampMagnitude(diff, MaxJointToRefDist);

            var bbDiff = Main.Instance.BoundingVolMax - Main.Instance.BoundingVolMin;
            var height = bbDiff.y;

            innerRadius = new[] { bbDiff.x, bbDiff.y, bbDiff.z }.Min();

            var mid = (Main.Instance.BoundingVolMax + Main.Instance.BoundingVolMin) / 2;
            var midFloor = new Vector3(mid.x, Main.Instance.BoundingVolMin.y, mid.z);

            Vector3 targetPosition = diff.Remap(Vector3.zero, new Vector3(MaxJointToRefDist, MaxJointToRefDist, MaxJointToRefDist), Vector3.zero, new Vector3(innerRadius, innerRadius, innerRadius));
            targetPosition = targetPosition + midFloor;
            dronePos[i] = Vector3.Lerp(dronePos[i], targetPosition, 0.25f); // smoothing

            yPercent[i] = (dronePos[i].y - Main.Instance.BoundingVolMin.y) / height;

            if (!Simulation)
            {
                var m_curTarget = Util.ClampToMinHeight(dronePos[i], Main.Instance.MinimumFlightHeight);
                var tgt = Util.ConvertToGcCoords(Util.Vec3ToVec4(m_curTarget, DroneVelocity));
                TcpMgr.Instance.CmdExtWaypointFollow(Util.SingleIdToSet(drones[i].DroneId()), tgt, 0.0f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var pos in dronePos)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pos, 0.25f);
            //print(pos);
        }
    }

    public float[] GetYPercent() { return yPercent; }
    public int GetNumMappedJoints() { return MappedJoints.Length; }
}
