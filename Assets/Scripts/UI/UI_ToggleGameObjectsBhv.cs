using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ToggleGameObjectsBhv : MonoBehaviour
{
    private Toggle m_toggle;
    public GameObject[] toToggle;

    private void Start()
    {
        m_toggle = GetComponent<Toggle>();
        // dalay one frame to give others the chance
        // to access data before SetActive(false)
        StartCoroutine(StartDelayed());
    }

    public void FireToggleGameObjects()
    {
        m_toggle = GetComponent<Toggle>();
        foreach (GameObject go in toToggle)
        {
            go.SetActive(m_toggle.isOn);
        }
    }

    public void FireEnableToggle()
    {
        m_toggle = GetComponent<Toggle>();
        foreach (GameObject go in toToggle)
        {
            go.SetActive(true);
        }
    }

    public void FireDisableToggle()
    {
        m_toggle = GetComponent<Toggle>();
        foreach (GameObject go in toToggle)
        {
            go.SetActive(false);
        }
    }

    private IEnumerator StartDelayed()
    {
        yield return 0;
        FireToggleGameObjects();
    }
}
