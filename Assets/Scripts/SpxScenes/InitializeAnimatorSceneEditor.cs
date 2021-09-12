using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InitializeAnimatorScene))]
public class InitializeAnimatorSceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        InitializeAnimatorScene ias = (InitializeAnimatorScene)target;

        GUILayout.Label("Click to initialize the AnimatorController.\nExisting AnimatorControllers might get overwritten!");

        DrawDefaultInspector();
        GUILayout.Label("If name is empty, <GeneratedAnimatorController> is default.");

        if (GUILayout.Button("Initialize Animator"))
        {
            ias.InitializeAnimator();
        }
    }
}
