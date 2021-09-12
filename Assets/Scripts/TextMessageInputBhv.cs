using System.Collections.Generic;

public class TextMessageInputBhv : InputFieldBhv {
    public static StringInputFieldDelegate MessageChanged;

    public UnityEngine.UI.InputField TextMessageDroneIdInputField;

    protected override void InputChanged()
    {
        if (MessageChanged != null)
            MessageChanged(m_inputField.text);
    }

    public void FireTextMessage()
    {
        HashSet<int> ids = Util.ParseDrones(TextMessageDroneIdInputField.text);

        TcpMgr.Instance.CmdExtHalt(ids);
        TcpMgr.Instance.CmdExtText(ids, m_inputField.text,
            new TcpMgr.AckInfo(false));
    }
}
