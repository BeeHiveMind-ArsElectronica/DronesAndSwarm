using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavInputField : UnityEngine.MonoBehaviour
{
    UnityEngine.UI.InputField m_field;

    // Start is called before the first frame update
    void Start()
    {

        m_field = gameObject.GetComponent<UnityEngine.UI.InputField>();


    }

    // Update is called once per frame
    void Update()
    {
        if (m_field.isFocused && Input.GetKeyUp(KeyCode.Tab))
        {
            m_field.FindSelectableOnRight().Select();
        }
    }
}
