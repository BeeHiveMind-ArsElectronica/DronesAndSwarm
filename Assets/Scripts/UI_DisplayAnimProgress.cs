using UnityEngine;
using UnityEngine.UI;

public class UI_DisplayAnimProgress : MonoBehaviour
{
    private AnimationCtrlBhv m_animCtrlBhv;
    private Text m_text;

    private void Start()
    {
        m_text = GetComponent<Text>();
        m_animCtrlBhv = FindObjectOfType<AnimationCtrlBhv>();
        m_animCtrlBhv.CurrentTimeUpdatedEvent += OnTimeUpdated;
    }

    private void OnDestroy()
    {
        m_animCtrlBhv.CurrentTimeUpdatedEvent -= OnTimeUpdated;
    }

    private void OnTimeUpdated(float value)
    {
        if (float.IsInfinity(value))
        {

        }
        m_text.text = value.ToString("F1") + "/" + m_animCtrlBhv.AnimLengthInS.ToString("F1");
    }
}
