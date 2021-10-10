
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class InputFieldTextSender : UdonSharpBehaviour
{
    InputField inputField;
    TMPro.TextMeshProUGUI tmpugui;
    [UdonSynced] public string content;
 
    void Start()
    {
        inputField = this.gameObject.GetComponentInChildren<InputField>();
        tmpugui = this.gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    void Update()
    {
        
    }

    public void UpdateText()
    {
        content += inputField.text;
        tmpugui.text = content;
    }

    public void ClearText()
    {
        content = "";
        tmpugui.text = content;
    }

    public void NewLine()
    {
        content += "\r\n";
        
    }
}
