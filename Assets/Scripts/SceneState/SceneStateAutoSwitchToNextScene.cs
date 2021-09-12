using UnityEngine;

public class SceneStateAutoSwitchToNextScene : AbstractSceneState {

    public Animator Animator;

    private void Start () {
        StateType = EStateType.ANIMATED;
    }

    public override void StateUpdate()
    {
        base.StateUpdate();

        AnimatorStateInfo asi = Animator.GetCurrentAnimatorStateInfo(0);

        // switch to next state when scene is over
        // FIXME: we are checking length here because normalizedTime is > 1.0 for a couple of frames
        //        in the beginning. Find a cleaner solution!

        // TODO: additionally wait and check if all bots @ correct positoin!
        if (asi.length != 1 && asi.normalizedTime >= 1.0f)
        {
            TransitionOut(StateType);
            SceneManager.Instance.NextSceneRequested = true;
        }

        //if (Scene.AutoProceedToStartState && Scene.ReadyToProceedToStart
        //    && m_transitions.ContainsKey("AUTO_START"))
        //{
        //    Scene.ReadyToProceedToStart = false;
        //    Scene.PerformTransition("AUTO_START");
        //}
    }

    public override void TransitionIn (EStateType prev)
    {
        base.TransitionIn(prev);
     //   TcpMgr.Instance.CmdExtGoExternal(DroneIds);
    }
}
