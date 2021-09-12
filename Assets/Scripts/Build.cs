#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Diagnostics;


public class Build : MonoBehaviour {

    [MenuItem("Build/Build %#d")]
    public static void DoBuild()
    {
        string[] levels = new string[] {"Assets/Scenes/main.unity"};
        BuildPipeline.BuildPlayer(levels, "Build/SpaxelsUnityCtl.app", BuildTarget.StandaloneWindows64,
                                  BuildOptions.Development | BuildOptions.AllowDebugging);

    }

    [MenuItem("Build/Run %#r")]
    public static void DoRun()
    {

        Process proc = new Process();
        proc.StartInfo.FileName = Application.dataPath + "/../Build/SpaxelsUnityCtl.app";
        proc.StartInfo.UseShellExecute = true;
        proc.Start();
    }


}

#endif