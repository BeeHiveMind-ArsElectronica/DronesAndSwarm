
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AbstractAnimatorScene : AbstractScene
{
    public static string TriggetPrefix = "TriggerTo-";
    protected static string m_triggerToIdle = "TriggerTo-Idle";
    private static string AnimationFileEnding = ".dae";
    private static string AnimationDirecotry = "Assets/Animations";

    protected AnimatorSceneManager sceneManager;
    [Header("Collada File. Must be located in 'Assets/Animations' !")]
    public Transform AnimationFile;
    public Transform BakeObject
    {
        get { return AnimationFile.Find("Bake"); }
    }
    protected Avatar m_animationAvatar;
    private Motion m_animationMotion;


    // IMPORTANT: If you want to have more animators wihtin your scene
    // set the animator manually in the editor 
    // and ensure that it is not overwirtten in any script accidentally
    protected Animator m_showFileAnimator;
    public Animator ShowFileAnimator
    {
        get { return m_showFileAnimator; }
        set { m_showFileAnimator = value; }
    }

    #region unity callbacks
    protected override void Start()
    {
        base.Start();
        sceneManager = GetComponentInParent<AnimatorSceneManager>();
    }
    #endregion

    #region AbstractScene
    protected override void Init()
    {
        base.Init();

        if (m_showFileAnimator == null)
        {
            m_showFileAnimator = FindObjectOfType<Animator>();
        }
    }

    // setup animator and animation controller
    public virtual void PreInitialize(Animator animator, AnimatorController animatorController, AnimatorStateMachine rootStateMachine)
    {
        m_showFileAnimator = animator;

        InitializeAnimator(animatorController, rootStateMachine);
    }
    #endregion

    private void InitializeAnimator(AnimatorController animatorController, AnimatorStateMachine rootStateMachine)
    {
        // --------------------------------
        // ---- avatar from file ----------
        Animator fileAnimator = AnimationFile.GetComponent<Animator>();
        m_animationAvatar = fileAnimator.avatar;
        if (m_animationAvatar == null)
        {
            Debug.LogError("Avatar not found!");
        }

        // --------------------------------
        // ---- clip/motion from file -----
        string[] files = System.IO.Directory.GetFiles(
            AnimationDirecotry, AnimationFile.name + AnimationFileEnding, System.IO.SearchOption.AllDirectories);
        string filePath = "notFound";
        if (files.Length == 1)
        {
            filePath = files[0];
        }
        else if(files.Length > 1)
        {
            Debug.LogError("More than one " + AnimationFile.name + AnimationFileEnding + " found!");
        }
        else
        {
            Debug.LogError(AnimationFile.name + AnimationFileEnding + " not found!");
        }

        m_animationMotion = AssetDatabase.LoadAssetAtPath<Motion>(filePath);
        if (m_animationMotion == null)
        {
            Debug.LogError("Motion/Clip not found!");
        }

        // --------------------------------
        // ---- add triggers --------------
        bool triggerToNextExistsAlready = false;
        foreach (AnimatorControllerParameter item in animatorController.parameters)
        {
            if (item.name == m_triggerToIdle)
            {
                triggerToNextExistsAlready = true;
            }
        }
        if (!triggerToNextExistsAlready)
        {
            animatorController.AddParameter(m_triggerToIdle, AnimatorControllerParameterType.Trigger);
        }
        animatorController.AddParameter(TriggetPrefix + AnimationFile.name, AnimatorControllerParameterType.Trigger);

        // --------------------------------
        // ---- add states ----------------
        AnimatorState thisState = rootStateMachine.AddState(AnimationFile.name);
        thisState.motion = m_animationMotion;

        // --------------------------------
        // ---- get idle state ------------
        AnimatorState idleState = null;
        foreach (ChildAnimatorState item in rootStateMachine.states)
        {
            if (item.state.name == "Idle")
            {
                idleState = item.state;
            }
        }

        // --------------------------------
        // ---- add transitions -----------
        foreach (AnimatorStateTransition item in thisState.transitions)
        {
            thisState.RemoveTransition(item);
        }
        AnimatorStateTransition[] transitions = new AnimatorStateTransition[2];
        transitions[0] = idleState.AddTransition(thisState);
        transitions[0].AddCondition(AnimatorConditionMode.If, 0, TriggetPrefix + AnimationFile.name);
        transitions[1] = thisState.AddTransition(idleState);
        transitions[1].AddCondition(AnimatorConditionMode.If, 0, m_triggerToIdle);

        foreach (AnimatorStateTransition t in transitions)
        {
            t.hasFixedDuration = false;
            t.duration = 0;
            t.exitTime = 0;
            t.offset = 0;
        }
    }
}
