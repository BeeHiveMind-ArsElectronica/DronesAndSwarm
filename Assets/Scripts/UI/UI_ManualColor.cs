using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_ManualColor : MonoBehaviour
{

    public static Color MANUAL_COLOR;

    [SerializeField]
    private bool m_useMidiInput = true;
    EasyController m_midiController;

    [SerializeField]
    private Slider m_hSlider,m_sSlider, m_vSlider;

    [SerializeField]
    private Color m_startColor;

    private void Start()
    {
        m_midiController = FindObjectOfType<EasyController>();

        float h, s, v;
        Color.RGBToHSV(m_startColor, out h, out s, out v);

        m_hSlider.value = h;
        m_sSlider.value = s;
        m_vSlider.value = v;

        SetSliderColor(m_hSlider, m_startColor);
        StartCoroutine(UpdateColors());
    }

    private IEnumerator UpdateColors()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            UpdateMidiInput();

            Color newColor = Color.HSVToRGB(m_hSlider.value, m_sSlider.value, m_vSlider.value);

            MANUAL_COLOR = newColor;

            SendColor(newColor);
            SetSliderColor(m_hSlider, newColor);
        }
    }

    private void SendColor(Color newColor)
    {
        TcpMgr.Instance.CmdExtColor(Util.SingleIdToSet(-1), new Vector3(newColor.r, newColor.g, newColor.b), 0);
    }

    private void SetSliderColor(Slider slider, Color c)
    {
        slider.transform.GetChild(0).GetComponent<Image>().color = c;
        slider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = c;
        slider.transform.GetChild(2).GetChild(0).GetComponent<Image>().color = c;
    }

    private void UpdateMidiInput()
    {
        if (m_useMidiInput == false || m_midiController == null)
        {
            return;
        }

        m_hSlider.value = m_midiController.H;
        m_sSlider.value = m_midiController.S;
        m_vSlider.value = m_midiController.V;
    }
}
