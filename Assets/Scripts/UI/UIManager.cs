using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UIManagerState {
    Create, Update
}

public class UIManager : MonoBehaviour
{
    public GameObject createButton;
    public GameObject updateButton;

    UIManagerState state;

    public void ChangeState(UIManagerState newState) {
        if (state != newState) {
            state = newState;
            UpdateState();
        }
    }

    void UpdateState() {
        switch (state) {
            case UIManagerState.Create:
                createButton.SetActive(true);
                updateButton.SetActive(false);
                break;
            case UIManagerState.Update:
                createButton.SetActive(false);
                updateButton.SetActive(true);
                break;
        }
    }

    public void Start() {
        UpdateState();
    }
}
