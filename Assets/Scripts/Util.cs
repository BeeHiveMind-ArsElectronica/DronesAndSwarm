using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public delegate void SimpleDelegate();
public delegate void FloatDelegate(float value);
public delegate void IntDelegate(int value);
public delegate void IntIntFloatDelegate(int id, int cmdType, float value);

public class Util : MonoBehaviour {

    public static string DroneStateToString(GcTypes.DroneState st)
    {
        switch (st)
        {
            case GcTypes.DroneState.NONE: return "NONE";
            case GcTypes.DroneState.PUPPET: return "PUPPET";
            case GcTypes.DroneState.RESET_GEOFENCE: return "RESET_GEOFENCE";
            case GcTypes.DroneState.SET_GEOFENCE: return "SET_GEOFENCE";
            case GcTypes.DroneState.GUESS_STATE: return "GUESS_STATE";
            case GcTypes.DroneState.ON_GROUND: return "ON_GROUND";
            case GcTypes.DroneState.TAKEOFF_WAIT: return "TAKEOFF_WAIT";
            case GcTypes.DroneState.TAKEOFF_MOTORS_ON: return "TAKEOFF_MOTORS_ON";
            case GcTypes.DroneState.TAKEOFF_LOW_LEVEL: return "TAKEOFF_LOW_LEVEL";
            case GcTypes.DroneState.TAKEOFF: return "TAKEOFF";
            case GcTypes.DroneState.FOLLOW_TARGET: return "FOLLOW_TARGET";
            case GcTypes.DroneState.LANDING_INITIATE: return "LANDING_INITIATE";
            case GcTypes.DroneState.LANDING_WAIT: return "LANDING_WAIT";
            case GcTypes.DroneState.LANDING_PREPARE: return "LANDING_PREPARE";
            case GcTypes.DroneState.LANDING_FAST: return "LANDING_FAST";
            case GcTypes.DroneState.LANDING_SLOW: return "LANDING_SLOW";
            case GcTypes.DroneState.LANDING_MOTORS_OFF: return "LANDING_MOTORS_OFF";
            default: return "UNKNOWN";
        }
    }

    [System.Serializable]
    public class DroneIdRange
    {
        public int Start;
        public int End;
    }

    public static HashSet<int> SingleIdToSet(int sid)
    {
        HashSet<int> s = new HashSet<int>();
        s.Add(sid);
        return s;
    }

    public static HashSet<int> MakeIdRange(int lowerInclusive, int higherInclusive)
    {
        HashSet<int> s = new HashSet<int>();
        for (int i = lowerInclusive; i <= higherInclusive; i++)
        {
            s.Add(i);
        }
        return s;
    }

    public static string IdSetToString(HashSet<int> set)
    {
        string txt = "";
        bool frst = true;
        foreach (int i in set)
        {
            if (frst)
            {
                frst = false;
            } 
            else
            {
                txt += ",";
            }

            txt += i;
        }

        return txt;
    }
    
    public static float NormalizeAngle(float yaw)
    {
        if (yaw < 0.0f)
        {
            yaw += 360.0f;
        }
        else if (yaw > 360.0f)
        {
            yaw -= 360.0f;
        }
        
        return yaw;
    }

    public static Vector3 ClampToBoundaries(Vector3 v, Vector3 bMin, Vector3 bMax)
    {
        v.x = Mathf.Clamp(v.x, bMin.x, bMax.x);
        v.y = Mathf.Clamp(v.y, bMin.y, bMax.y);
        v.z = Mathf.Clamp(v.z, bMin.z, bMax.z);
        return v;
    }

    public static Vector3 ClampToMinHeight(Vector3 v, float minHeight)
    {
        v.y = Mathf.Max(v.y, minHeight);
        return v;
    }

    public static Vector4 MakeWaypoint(Vector3 v, float s)
    {
        var vout = new Vector4(v.x, v.z, v.y, s);
        return vout;
    }

    public static string SecondsToClockString(float seconds)
    {
        int mins = ((int) seconds) / 60;
        int secs = (int) seconds - mins * 60;

        return string.Format(mins.ToString("D2") + ":" + secs.ToString("D2"));

    }

    public static Vector4 Vec3ToVec4(Vector3 v, float w)
    {
        return new Vector4(v.x, v.y, v.z, w);

    }

    public static Vector4 ConvertToGcCoords(Vector4 v)
    {
        return new Vector4(v.x, v.z, v.y, v.w);
    }

    public static Vector3 ConvertToGcCoords(Vector3 v)
    {
        return new Vector4(v.x, v.z, v.y);
    }

    public static string FmtFloat(float f)
    {
        return string.Format("{0:0.000}", f); 
    }

    public static string FmtDouble(double d)
    {
        return string.Format("{0:0.000}", d); 
    }

    public static bool ValueCodesReverseFlag(float val)
    {
        // FIXME-PETERHO this will be 0 when C4D
        // workflow has been adapted
        return val > 0;
    }

    public static int YawInDegE100(float yaw)
    {
        return (int)(yaw * 100.0f);
    }

    public static HashSet<int> ParseDrones(string text)
    {
        HashSet<int> drones = new HashSet<int>();
        string[] ids = null;

        // check separator
        bool isRange = false;
        
        if(text.Contains("-"))
        {
            isRange = true;
            ids = text.Split('-');
        }
        else
        {
            ids = text.Split(',');
        }

        if (ids.Length == 1)
        {
            drones = Util.SingleIdToSet(int.Parse(ids[0]));
        }
        else if (ids.Length == 2 && isRange)
        {
            int first = int.Parse(ids[0]);
            int last = int.Parse(ids[1]);

            drones = Util.MakeIdRange(first, last);
        }
        else if (ids.Length >= 2)
        {
            foreach(string id in ids)
            {
                drones.Add(int.Parse(id));
            }
        }
        else
        {
            drones = Util.SingleIdToSet(0);
        }

        return drones;
    }

    public static float MinimizeRotationAngle(float omega)
    {
        float omegaComplement;

        if (omega < 0.0f)
        {
            omegaComplement = omega + 360.0f;
        }
        else
        {
            omegaComplement = omega - 360.0f;
        }

        if (Mathf.Abs(omegaComplement) < Mathf.Abs(omega))
        {
            return omegaComplement;
        }
        else
        {
            return omega;
        }
    }

    public static float AbsoluteNormalizedAngle(Vector3 vec)
    {
        float angle = Vector3.SignedAngle(Vector3.forward, vec, Vector3.down);
        angle = Util.NormalizeAngle(angle);
        return angle;
    }

    public static string RemoveSpecialCharacters(string str)
    {
        return Regex.Replace(str, "[^a-zA-Z0-9_.-]+", "", RegexOptions.Compiled);
    }
}
