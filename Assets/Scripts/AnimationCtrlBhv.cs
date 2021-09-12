using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimationCtrlBhv : MonoBehaviour
{
    public Text playPausebuttonText;

    public event SimpleDelegate TogglePlayPauseAnimEvent;
    public event FloatDelegate CurrentTimeUpdatedEvent;
    public event SimpleDelegate StartAnimatorEvent;

    public Text textAnimSpd;
    public Text textLoop;

    private float m_playSpeed = 1;
    private float m_playSpeedOverride = 1;


    private bool m_looping = false;
    private int m_maxLoops = 0;
    private int m_currentLoop = 0;

    public float PlaySpeedOverride()
    {
        return m_playSpeedOverride;
    }

    private Slider m_slider;
    public Slider PlaySlider, PlaySpdSlider;
    public Animator m_animator;
    public Animator Animator
    {
        get
        {
            return m_animator;
        }
        set
        {
            m_animator = value;
        }
    }

    private float m_normalizedTime = 0;
    private float m_animLengthInS = 0.0f;
    public float AnimLengthInS
    {
        get
        {
            return m_animLengthInS;
        }
    }

    private float m_currentTimeInS = 0.0f;
    public float CurrentTime
    {
        get
        {
            return m_currentTimeInS;
        }
    }

    private bool m_playing = false;
    private bool m_isDragging = false;

    #region unity callbacks
    private void Awake()
    {
       // m_animator = FindObjectOfType<Animator>();
    }
    private void Start()
    {
        m_slider = PlaySlider;
    }
    private void Update()
    {
        //print(m_animator.GetCurrentAnimatorStateInfo(0).shortNameHash);
        UpdateProgress();
    }
    #endregion

    #region public
    public void SetPlaySpeed()
    {
        m_playSpeedOverride = PlaySpdSlider.value;
        if (IsPlaying()) // live update
        {
            m_animator.speed = m_playSpeedOverride;
        }
        textAnimSpd.text = m_playSpeedOverride.ToString("F2");
    }

    public void SetLoopingParams(bool looping, int maxLoops, int curLoop)
    {
        m_looping = looping;
        m_maxLoops = maxLoops;
        m_currentLoop = curLoop;
    }

    public void SetLooping(bool looping)
    {
        m_looping = looping;
    }

    public void NextLoop()
    {
        m_currentLoop++;
        FireReset(true);
    }

    public bool IsLooping()
    {
        return m_looping;
    }

    public int CurrentLoop()
    {
        return m_currentLoop;
    }

    public int MaxLoops()
    {
        return m_maxLoops;
    }

    public void SetPlaying(bool isPlaying)
    {
        //StopAllCoroutines();
        //StartCoroutine(SetPlayingRoutine(isPlaying, useRotationAnticipation));
        m_playing = isPlaying;

        if (!m_playing)
        {
            CurrentTimeUpdatedEvent(0.0f);
            PauseAnimator();
        }
        else
        {
            StartAnimator();
            m_animator.speed = m_playSpeedOverride;
        }
        UpdatePlayPauseText();
    }
    public bool IsPlaying()
    {
        return m_playing;
    }
    public void FirePlayPause()
    {
        if (TogglePlayPauseAnimEvent != null)
            TogglePlayPauseAnimEvent();

        if (IsPlaying())
        {
            Debug.Log("stopping playback");
            m_animator.speed = 0.0f;

            AudioMgrBhv.Instance.Pause();
        }
        else
        {
            m_animator.speed = m_playSpeedOverride;
            Debug.Log("resuming playback");

            AudioMgrBhv.Instance.Resume();
        }

        UpdateState();
    }
    public void FireStop()
    {
        SetPlaying(false);
        FireReset(true);
    }
    public void FireReset(bool force = false)
    {
        if (m_animator && (!m_playing || force))
        {
            Debug.Log("fire reset");

            int nameHash = m_animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            m_animator.Play(nameHash, 0, 0.0000000f); // FIXME this is to trigger a single timecode message as soon as we reset the anim (even if we are at 0)

            m_normalizedTime = 0;
            UpdateProgress();
        }
    }

    // animation slider
    public void BeginDrag()
    {
        m_isDragging = true;
    }
    public void EndDrag()
    {
        m_isDragging = false;
    }
    public void DragSlider()
    {
        int nameHash = m_animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        m_animator.Play(nameHash, 0, m_slider.value);
    }
    #endregion

    #region private

    private void PauseAnimator()
    {
        if (m_animator.speed > 0)
        {
            m_playSpeed = m_animator.speed;
        }
        m_animator.speed = 0.0f;
    }
    private void StartAnimator()
    {
        if (m_playSpeed == 0)
        {
            m_playSpeed = m_playSpeedOverride;
        }
        m_animator.speed = m_playSpeed;

        if (StartAnimatorEvent != null)
            StartAnimatorEvent();
    }
    private void UpdatePlayPauseText()
    {
        playPausebuttonText.text = (m_playing ? "Pause" : "Play");
    }
    private void UpdateState()
    {
        m_playing = !m_playing;
        UpdatePlayPauseText();
    }
    private void UpdateProgress()
    {
        if(m_animator)
        {
            var stateInfo = m_animator.GetCurrentAnimatorStateInfo(0);

            if (m_playing || m_isDragging)
            {
                m_normalizedTime = stateInfo.normalizedTime;
            }

            if (m_playing)
            {
                m_animLengthInS = stateInfo.length;
            }

            m_slider.value = m_normalizedTime;

            m_currentTimeInS = m_normalizedTime * m_animLengthInS;
            
            CurrentTimeUpdatedEvent(m_currentTimeInS);

            textLoop.text = !m_looping ? "-" : ("L" + m_currentLoop + "/" + m_maxLoops);


        }
    }
    #endregion
}
