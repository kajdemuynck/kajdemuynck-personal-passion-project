using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchControls : MonoBehaviour
{
    public static TouchControls Instance;

    public Joystick MoveJoystick;
    public Joystick LookJoystick;
    public Button grabButton;
    public Button pauseButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    public void ActivateControls()
    {
        MoveJoystick.gameObject.SetActive(true);
        LookJoystick.gameObject.SetActive(true);
        grabButton.gameObject.SetActive(true);
    }
}
