using System;
using UnityEngine;

public class UI_HideElementBhv : MonoBehaviour
{
    [SerializeField]
    private UI_HideButtonBhv hideButtonPrefab;
    private UI_HideButtonBhv m_hideButton;

    private RectTransform m_rectTransform;
    private Vector3 m_initialPosition;

    private bool m_isHidden = false;

    private Vector3 m_hideOffset;
    private Vector3 m_buttonViewportPos;

    private void Start()
    {
        MainCanvas.ResizedEvent += OnResize;
        m_rectTransform = GetComponent<RectTransform>();

        m_hideButton = Instantiate(hideButtonPrefab, UpdatePosition(), Quaternion.identity);
        m_hideButton.transform.parent = FindObjectOfType<MainCanvas>().transform;

        RectTransform btnRectTrans = m_hideButton.GetComponent<RectTransform>();
        m_initialPosition = Camera.main.ScreenToViewportPoint(m_rectTransform.position);

        m_hideButton.Initialze(this);
    }

    private void OnDestroy()
    {
        MainCanvas.ResizedEvent -= OnResize;
    }

    public void Toggle()
    {
        m_isHidden = !m_isHidden;

        if (m_isHidden)
        {
            m_rectTransform.position += m_hideOffset;
        }
        else
        {
            m_rectTransform.position = Camera.main.ViewportToScreenPoint(m_initialPosition);
        }
    }

    private Vector3 UpdatePosition()
    {
        Vector3[] v = new Vector3[4];
        m_rectTransform.GetWorldCorners(v);

        Rect res = Camera.main.pixelRect;

        Vector3 direction = Vector3.zero;
        Vector3 buttonPos;
        if (v[0].x < res.width - v[3].x)
        {
            direction += Vector3.left;
            buttonPos = v[0];
        }
        else
        {
            direction += Vector3.right;
            buttonPos = v[3];
        }
        if (res.height - v[1].y < v[0].y)
        {
            direction += Vector3.up;
            buttonPos.y = v[1].y;
        }
        else
        {
            direction += Vector3.down;
            buttonPos.y = v[0].y;
        }

        m_buttonViewportPos = Camera.main.ScreenToViewportPoint(buttonPos);
        m_hideOffset = new Vector3(direction.x * m_rectTransform.rect.width, direction.y * m_rectTransform.rect.height);

        return buttonPos;
    }
    private void OnResize()
    {
        if (!m_isHidden)
        {
            UpdatePosition();
        }

        m_hideButton.transform.position = Camera.main.ViewportToScreenPoint(m_buttonViewportPos);
    }
}
