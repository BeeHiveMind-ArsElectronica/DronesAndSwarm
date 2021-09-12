using UnityEngine;
using UnityEngine.UI;

public delegate void IntInputFieldDelegate(int value);
public delegate void StringInputFieldDelegate(string value);

[RequireComponent(typeof(InputField))]
public class InputFieldBhv : MonoBehaviour
{
    public static event StringInputFieldDelegate InputChangedString;
    protected InputField m_inputField;

    private void Start()
    {
        m_inputField = GetComponent<InputField>();
        m_inputField.onValueChanged.AddListener(delegate { InputChanged(); });
    }

    protected virtual void InputChanged()
    {
        if(InputChangedString != null)
            InputChangedString(m_inputField.text);
    }
}