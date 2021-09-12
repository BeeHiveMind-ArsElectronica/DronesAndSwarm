using UnityEngine;
using UnityEngine.UI;

public class UI_ShowFPS : MonoBehaviour
{
    private Text m_text;

    private void Start()
    {
        m_text = GetComponent<Text>();
    }
    private void Update()
    {
        m_text.text = (1.0f / Time.smoothDeltaTime).ToString("0");
    }
}
