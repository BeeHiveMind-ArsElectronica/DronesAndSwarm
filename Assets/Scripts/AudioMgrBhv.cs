using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioMgrBhv : MonoBehaviour {

    public enum ClipEnum : int {
        NONE = -1,

        S0,
        S0X,
        S1,
        S1X,
        S2,
        S2X,
        S3,
        S3X,
        S4,
        S4X,
        S5,
        S5X,
        S6,
        S6X,
        S7,
        S7X,
        S8,
        S8X,

        END

    }
    private class AudioLine
    {
        public AudioSource source;
        public Coroutine fader;
        public int currentClip;
        public float fadeVol;

        public bool IsPlaying { get { return source.isPlaying; } }
    }

    public AudioClip[] Clips;
    private AudioLine[] m_lines;
    private int m_currentLine = 0;

    public UnityEngine.UI.Text AudioDbgText;

    private static AudioMgrBhv instance;
    public static AudioMgrBhv Instance
    {
        // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
        // otherwise we assign instance to a new component and return that
        get { return instance ?? (instance = GameObject.Find("AudioMgr").GetComponent<AudioMgrBhv>()); }
    }

    private void Start()
    {
        m_lines = new AudioLine[2];

        // destroy existing AudioSources
        AudioSource[] srcs = GetComponents<AudioSource>();
        foreach (AudioSource s in srcs)
        {
            Destroy(s);
        }

        m_lines[0] = new AudioLine();
        m_lines[0].source = gameObject.AddComponent<AudioSource>();
        m_lines[1] = new AudioLine();
        m_lines[1].source = gameObject.AddComponent<AudioSource>();
    }

    private void Update () {
        string dbg = "NO AUDIO";
        if (m_lines[m_currentLine].IsPlaying &&
            m_lines[m_currentLine].currentClip > (int)ClipEnum.NONE &&
            m_lines[m_currentLine].currentClip <= (int)ClipEnum.END)
        {
            dbg = string.Format("{0}: {1:D3} / {2:D3}",
                ((ClipEnum)m_lines[m_currentLine].currentClip).ToString(),
                (int)m_lines[m_currentLine].source.time,
                (int)Clips[m_lines[m_currentLine].currentClip].length);
        }

        AudioDbgText.text = dbg;
    }

    public void Pause()
    {
        foreach (AudioLine line in m_lines)
        {
            line.source.Pause();
        }
    }

    public void Resume()
    {
        foreach (AudioLine line in m_lines)
        {
            line.source.UnPause();
        }
    }

    public void PlayIdleOne()
    {
    //    Instance.PlayBackgroundClip(ClipEnum.IDLELOOP, 0.4f);
    }

    public void PlayIdleOneLoud()
    {
        //Instance.PlayClip(ClipEnum.SWARM);
        Instance.PlayBackgroundClip(ClipEnum.S0X, 0.85f);
    }

    public void PlayIdleTwo()
    {
    //    Instance.PlayBackgroundClip(ClipEnum.IDLELOOP_2, 0.45f);
    }

    public void StopIdle()
    {
        Instance.StopBackgroundClip();
    }

    public void PlayClip (ClipEnum key, float fadeDur = 0.0f)
    {
        var src = GetComponent<AudioSource>();
        src.clip = Clips[(int)key];
        src.loop = false;

        if (fadeDur > 0.0f)
        {
            src.volume = 0f;
            m_lines[m_currentLine].fadeVol = 0f;
            src.Play();
            m_lines[m_currentLine].fader = StartCoroutine(FadeIn(m_lines[m_currentLine], 1.0f, fadeDur));
        }
        else
        {
            src.volume = 1f;
            src.Play();
        }
        m_lines[m_currentLine].currentClip = (int)key;
    }

    public void TestCrossFade()
    {
        int nxtClp = (m_lines[m_currentLine].currentClip + 1) % 4;
        CrossFadeToClip((ClipEnum)nxtClp, 1.0f, 1.0f);
    }

    public void CrossFadeToClip(ClipEnum key, float targetVol = 1.0f, float fadeDur = 1.0f)
    {
        // stop line fade if running
        if (m_lines[m_currentLine].fader != null)
        {
            StopCoroutine(m_lines[m_currentLine].fader);
        }
        // initiate fade out
        m_lines[m_currentLine].fader = StartCoroutine(FadeOut(m_lines[m_currentLine], fadeDur));

        // switch line
        m_currentLine = ((m_currentLine + 1) % 2);

        // prepare track
        m_lines[m_currentLine].currentClip = (int)key;
        m_lines[m_currentLine].source.clip = Clips[(int)key];
        m_lines[m_currentLine].source.loop = true;
        m_lines[m_currentLine].source.volume = 0f;
        m_lines[m_currentLine].fadeVol = 0f;
        m_lines[m_currentLine].source.Play();

        // stop line fade if running
        if (m_lines[m_currentLine].fader != null)
        {
            StopCoroutine(m_lines[m_currentLine].fader);
        }
        // initiate fade in
        m_lines[m_currentLine].fader = StartCoroutine(FadeIn(m_lines[m_currentLine], targetVol, fadeDur));
    }

    IEnumerator FadeIn(AudioLine line, float tgtVol, float fadeDur = 1.0f)
    {
        fadeDur = Mathf.Clamp(fadeDur, 0.1f, 10.0f);
        while (line.fadeVol < tgtVol)
        {
            line.fadeVol += (1.0f / fadeDur * Time.deltaTime);
            line.source.volume = line.fadeVol;
            yield return null;
            //yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator FadeOut(AudioLine line, float fadeDur = 1.0f)
    {
        fadeDur = Mathf.Clamp(fadeDur, 0.1f, 10.0f);
        while (line.fadeVol > 0f)
        {
            line.fadeVol -= (1.0f / fadeDur * Time.deltaTime);
            line.source.volume = line.fadeVol;
            yield return null;
            //yield return new WaitForSeconds(0.1f);
        }

        line.source.Stop();
    }

    

    public void PlayBackgroundClip (ClipEnum key, float tgtVol)
    {
        if (m_lines[m_currentLine].fader != null)
        {
            StopCoroutine(m_lines[m_currentLine].fader);
        }

        m_lines[m_currentLine].source.clip = Clips[(int)key];
        m_lines[m_currentLine].source.loop = true;
        m_lines[m_currentLine].source.volume = 0f;
        m_lines[m_currentLine].fadeVol = 0f;
        m_lines[m_currentLine].source.Play();
        m_lines[m_currentLine].currentClip = (int) key;

        m_lines[m_currentLine].fader = StartCoroutine(FadeIn(m_lines[m_currentLine], tgtVol));
    }

    public void StopBackgroundClip ()
    {
        if (m_lines[m_currentLine].fader != null)
        {
            StopCoroutine(m_lines[m_currentLine].fader);
        }
        m_lines[m_currentLine].fader = StartCoroutine(FadeOut(m_lines[m_currentLine]));
    }

    public void StopClip ()
    {
        foreach (AudioLine line in m_lines)
        {
            line.source.Stop();
        }
    }
}
