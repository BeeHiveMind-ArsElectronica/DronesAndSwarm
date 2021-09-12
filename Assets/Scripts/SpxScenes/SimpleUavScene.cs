using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor.Animations;

/// <summary>
/// This is an example for a scripted uav show. See in Editor how it is used.
/// Each instance of "SimpleUavScene" represents an animation (.dae).
/// </summary>
public class SimpleUavScene : AbstractAnimatorScene
{
    private float m_playSpeed = 1;
    private AnimationCtrlBhv m_animCtrlBhv;

    [Header("Set active ID ranges here (rest inactive)")]
    public Util.DroneIdRange[] activeIds;

    public AudioMgrBhv.ClipEnum AudioClip, AudioClipTransition;

    private int m_sceneNumber = 0;

    #region AbstractScene
    protected override void Init()
    {
        base.Init();

        // NOTE: most of this could also go to AbstractAnimatorScene
        var chld = AnimationFile.GetChild(0);
        string[] c4dName = chld.name.Split('_');

        string sceneNumberStr = c4dName[2].Replace("S", "");
        int.TryParse(sceneNumberStr, out m_sceneNumber);

        m_animCtrlBhv = FindObjectOfType<AnimationCtrlBhv>();
        AnimationCtrlBhv pausePlayAnim = m_animCtrlBhv;

        // instantiate bake object if a prefab is provided
        // if not - try getting existing one
        Transform bake;
        if (BakeObject != null)
        {
            // delete existing bake objects - if one exists
            DeleteExistingBakeObjs();

            bake = Instantiate(BakeObject, m_showFileAnimator.transform);
            bake.localPosition = bake.localPosition * 0.01f;
            bake.localScale = Vector3.one * 0.01f;
            bake.name = BakeObject.name;
            // scale down anim targets
            for (int i = 0; i < bake.childCount; i++)
            {
                bake.GetChild(i).transform.localScale = Vector3.one;
                bake.GetChild(i).GetComponent<MeshRenderer>().material = Main.Instance.BakeChildMaterial;
                var bcInfo = Instantiate(Main.Instance.BakeChildInfoPrefab, bake.GetChild(i).transform);
                bcInfo.GetComponent<TextMeshPro>().text = "" + i;
            }
        }
        else
        {
            bake = m_showFileAnimator.transform.GetChild(0);
        }

        var droneIdsAnim = Util.MakeIdRange(0, bake.childCount - 1);

        HashSet<int> droneIdsAnimStream = new HashSet<int>();
        HashSet<int> droneIdsInactiveStream = Util.MakeIdRange(0, bake.childCount - 1);

        // set active
        foreach (Util.DroneIdRange range in activeIds)
        {
            droneIdsAnimStream.UnionWith(Util.MakeIdRange(range.Start, range.End));
        }

        // set inactive
        droneIdsInactiveStream = Util.MakeIdRange(0, bake.childCount - 1);
        droneIdsInactiveStream.ExceptWith(droneIdsAnimStream);

        ///////////////////////////////////////////////////////////
        ////////////         ANIMATION STREAMED OUT OF UNITY
        ///////////////////////////////////////////////////////////
        States = new List<AbstractSceneState>();
        {
            var s = CreateStateObject("IDLE").AddComponent<SceneStateMultiDroneIdle>();
            s.AddTransition("MANUAL_NEXT", "READY");
            s.AddTransition("MANUAL_NEXT_2", "GO_HOME");
            s.WaitForIdle = false;
            s.DroneIds = droneIdsAnim;

            s.DoOnEnterDelegate = () =>
            {
                pausePlayAnim.SetPlaying(false);
                m_showFileAnimator.SetTrigger(m_triggerToIdle);
            };

            AddState(s);
        }

        {
            var s = CreateStateObject("READY").AddComponent<SceneStateMultiDroneIdle>();
            s.AddTransition("MANUAL_NEXT", "TAKEOFF");
            s.AddTransition("MANUAL_NEXT_2", "START-STREAM");
            s.AddTransition("MANUAL_NEXT_3", "IDLE");
            s.WaitForIdle = false;
            s.DroneIds = droneIdsAnim;
            s.DoOnEnterDelegate = () =>
            {
                m_showFileAnimator.avatar = m_animationAvatar;

                Dictionary<int, Vector4> idPosAndRot = new Dictionary<int, Vector4>();
                if (bake == null)
                {
                    bake = m_showFileAnimator.transform.GetChild(0);
                }
                int id = 0;
                foreach (Transform bot in bake)
                {
                    Vector4 posAndRot =
                        new Vector4(bot.localPosition.x, bot.localPosition.z, bot.localPosition.y,
                        bot.localRotation.eulerAngles.y);

                    idPosAndRot.Add(id++, posAndRot);
                }

                id = 0;
                foreach (Transform bot in bake)
                {
                    var animTgt = bot.gameObject.GetComponent<AnimTgtBhv>();
                    if (animTgt == null)
                    {
                        animTgt = bot.gameObject.AddComponent<AnimTgtBhv>();
                        animTgt.trailMaterial = sceneManager.trailMaterial;
                    }
                    animTgt.SendWaypoints = false;
                    animTgt.targetId = id;

                    animTgt.AnimTransformObject = bot;
                    id++;
                }

                m_showFileAnimator.SetTrigger(TriggetPrefix + AnimationFile.name); // Trigger Animation Take
                m_showFileAnimator.Play(m_showFileAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash);
            };

            AddState(s);
        }

        {
            var s = CreateStateObject("TAKEOFF").AddComponent<SceneStateMultiDroneIdle>();
            s.AddTransition("MANUAL_NEXT", "START-STREAM");
            s.WaitForIdle = false;
            s.DroneIds = new HashSet<int>(droneIdsAnimStream);

            s.DoOnEnterDelegate = () =>
            {
                // reset animation conrtol
                pausePlayAnim.FireReset(true);

                Main.Instance.SetWaypointMode(Main.WaypointMode.OFF);

                StopAllCoroutines();
                StartCoroutine(StartAnimation());

                TcpMgr.Instance.CmdExtGoExternal(s.DroneIds);
            };

            AddState(s);
        }

        {
            var s = CreateStateObject("START-STREAM").AddComponent<SceneStateMultiDroneIdle>();
            s.AddTransition("MANUAL_NEXT", "START-ANIMATION");
            s.WaitForIdle = false;
            s.DroneIds = new HashSet<int>(droneIdsAnim);

            s.DoOnEnterDelegate = () =>
            {
                TcpMgr.Instance.CmdExtHalt(s.DroneIds);
                Main.Instance.SetWaypointMode(Main.WaypointMode.STREAMING);
            };

            AddState(s);
        }

        {
            var s = CreateStateObject("START-ANIMATION").AddComponent<SceneStateMultiDroneIdle>();
            s.AddTransition("MANUAL_NEXT", "NEXT-SCENE");
            s.AddTransition("MANUAL_NEXT2", "IDLE");
            s.WaitForIdle = false;
            s.DroneIds = new HashSet<int>(droneIdsAnim);

            s.DoOnEnterDelegate = () =>
            {
                m_animCtrlBhv.SetPlaying(true);
            };

            s.DoOnExitDelegate = () =>
            {
                Debug.Log("HALT sent");
                TcpMgr.Instance.CmdExtHalt(s.DroneIds);

                m_animCtrlBhv.SetPlaying(false);
            };

            AddState(s);
        }

        {
            var s = CreateStateObject("NEXT-SCENE").AddComponent<SceneStateMultiDroneIdle>();
            s.AddTransition("MANUAL_NEXT", "IDLE");
            s.WaitForIdle = false;
            s.DroneIds = new HashSet<int>(droneIdsAnim);

            s.DoOnEnterDelegate = () =>
            {
                m_animCtrlBhv.SetPlaying(false);
                SceneManager.Instance.NextSceneRequested = true;
            };

            s.DoOnExitDelegate = () =>
            {
                Debug.Log("HALT sent");
                TcpMgr.Instance.CmdExtHalt(s.DroneIds);
            };

            AddState(s);
        }

        {
            var s = CreateStateObject("GO_HOME").AddComponent<SceneStateMultiDroneGoHome>();
            s.AddTransition("MANUAL_NEXT", "READY");
            s.WaitForIdle = false;
            s.DroneIds = new HashSet<int>(droneIdsAnim);

            s.DoOnEnterDelegate = () =>
            {
                TcpMgr.Instance.CmdExtHalt(s.DroneIds);
            };

            AddState(s);
        }

        SetInitialState("IDLE");

        SceneButton enableWpButton = new SceneButton();
        enableWpButton.Text = "enable WPs";
        enableWpButton.DoOnClickDelegate = () =>
        {

            Main.Instance.SetWaypointMode(Main.WaypointMode.STREAMING);
        };

        SceneButtons.Add(enableWpButton);

        SceneButton disableWpButton = new SceneButton();
        disableWpButton.Text = "disable WPs";
        disableWpButton.DoOnClickDelegate = () =>
        {
            Main.Instance.SetWaypointMode(Main.WaypointMode.OFF);
        };

        SceneButtons.Add(disableWpButton);

        {
            SceneButton sndBtn = new SceneButton();
            sndBtn.Text = "scene audio";
            sndBtn.DoOnClickDelegate = () =>
            {
                AudioMgrBhv.Instance.PlayClip(AudioClip, 1.0f);
            };

            SceneButtons.Add(sndBtn);
        }

        foreach (AbstractSceneState s in States)
        {
            s.Scene = this;
        }
    }

    protected override void UpdateImpl()
    {
        base.UpdateImpl();
    }

    public override void PreInitialize(Animator animator, AnimatorController animatorController, AnimatorStateMachine rootStateMachine)
    {
        base.PreInitialize(animator, animatorController, rootStateMachine);
    }

    public override void ExitScene()
    {
        base.ExitScene();
    }
    #endregion

    #region private
    private void DeleteExistingBakeObjs()
    {
        for (int i = 0; i < m_showFileAnimator.transform.childCount; i++)
        {
            Destroy(m_showFileAnimator.transform.GetChild(0).gameObject);
        }
    }

    private IEnumerator StartAnimation()
    {
        yield return 0;
        m_showFileAnimator.SetTrigger(TriggetPrefix + AnimationFile.name); // Trigger Animation Take
        m_animCtrlBhv.Animator = m_showFileAnimator;

        m_showFileAnimator.Play(m_showFileAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash);

        m_animCtrlBhv.SetPlaying(false);
        m_playSpeed = m_showFileAnimator.speed;
    }
    #endregion
}