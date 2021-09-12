using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_FireVoidVehicleCmdBhv : MonoBehaviour
{
    public void FireVoidVehicleCmd()
    {
        HashSet<int> dids = Util.MakeIdRange(0, Main.Instance.NumDrones - 1);

        TcpMgr.Instance.CmdExtVehicleCmd(dids, 4 /*VOID_CMD*/, 0/*param*/,
            new Vector4(0, 0, 0, 0), new TcpMgr.AckInfo(true, true, 3, 200));
    }
}
