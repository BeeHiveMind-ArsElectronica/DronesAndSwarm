using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowCollisions : MonoBehaviour
{
    private MeshRenderer m_meshRenderer;

    private void Start()
    {
        m_meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void OnTriggerEnter(Collider collider)
    {
        m_meshRenderer.enabled = true;
    }

    private void OnTriggerExit(Collider collider)
    {
        m_meshRenderer.enabled = false;
    }
}
