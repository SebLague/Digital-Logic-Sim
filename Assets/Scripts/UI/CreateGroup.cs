using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateGroup : MonoBehaviour
{

    public event System.Action<int> onGroupSizeSettingPressed;

    public TMP_InputField groupSizeInput;
    public Button setSizeButton;
    public GameObject menuHolder;

    int groupSizeValue;

    // Start is called before the first frame update
    void Start ()
    {
        setSizeButton.onClick.AddListener (SetGroupSize);
        groupSizeInput.onValueChanged.AddListener (SetCurrentText);
    }

    void SetCurrentText (string groupSize) {
        groupSizeValue = int.Parse(groupSize);
    }

    void CloseMenu () {
        menuHolder.SetActive(false);
    }
    
    void SetGroupSize () {
        if (onGroupSizeSettingPressed != null) {
            onGroupSizeSettingPressed.Invoke (groupSizeValue);
        }
        CloseMenu ();
    }
}
