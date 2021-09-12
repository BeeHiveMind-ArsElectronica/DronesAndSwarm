using UnityEditor.Animations;
using UnityEngine;

public class AnimatorSceneManager : SceneManager
{
    public Material trailMaterial;

    [Header("Animator Controller")]
    [Tooltip("The AnimatorController will be built on Start.\nThe existing one will be overwritten!")]
    [SerializeField]
    private bool m_initAnimatorOnStart = false;

    private Animator m_showFileAnimator;
    private AnimatorController m_animatorController;
    private AnimatorStateMachine m_rootStateMachine;

    public static new AnimatorSceneManager Instance
    {
        get
        {
            return (AnimatorSceneManager)instance ??
                (AnimatorSceneManager)(instance = GameObject.FindObjectOfType<SceneManager>());
        }
    }

    public AbstractAnimatorScene AnimatorScene
    {
        get { return (AbstractAnimatorScene)CurScene; }
    }
    protected override void Start()
    {
        InitializeAnimator(false);
        base.Start();
    }

    [ExecuteInEditMode]
    public void InitializeAnimator(bool force, string animationControllerName = "GeneratedAnimatorController")
    {
        if (animationControllerName == "")
        {
            animationControllerName = "GeneratedAnimatorController";
        }

        m_showFileAnimator = FindObjectOfType<Animator>();

        // ---------------------------------------
        // ---- just set animator in scenes ------
        if (!m_initAnimatorOnStart && !force)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                AbstractAnimatorScene s = transform.GetChild(i).GetComponent<AbstractAnimatorScene>();
                if (s != null)
                {
                    s.ShowFileAnimator = m_showFileAnimator;
                }
            }
            return;
        }

        // ---------------------------------------
        // ---- Create the AnimatorController ----
        m_animatorController = 
            AnimatorController.CreateAnimatorControllerAtPath(
                "Assets/AnimationController/"+ animationControllerName + ".controller");
        m_showFileAnimator.runtimeAnimatorController = m_animatorController;

        // ---------------------------------------
        // ---- get the root state machine -------
        m_rootStateMachine = m_animatorController.layers[0].stateMachine;

        // ---- add the idle state ---------------
        AnimatorState idleState = m_rootStateMachine.AddState("Idle");

        // remove idle state
        foreach (ChildAnimatorState item in m_rootStateMachine.states)
        {
            if (item.state.name != "Idle")
            {
                m_rootStateMachine.RemoveState(item.state);
            }
        }

        // ---------------------------------------
        // ---- initialize scenes ----------------
        for (int i = 0; i < transform.childCount; i++)
        {
            AbstractAnimatorScene s = transform.GetChild(i).GetComponent<AbstractAnimatorScene>();
            if (s != null)
            {
                s.PreInitialize(m_showFileAnimator, m_animatorController, m_rootStateMachine);
            }
        }

        Debug.Log("Initialized new AnimatorController (" + animationControllerName + ")");
    }
}