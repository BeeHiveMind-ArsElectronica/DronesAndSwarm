using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_HideButtonBhv : MonoBehaviour, IPointerClickHandler
{
    private UI_HideElementBhv m_hideElement;

    public void Initialze(UI_HideElementBhv element)
    {
        m_hideElement = element;
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        m_hideElement.Toggle();
    }
}
