using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchControls : MonoBehaviour
{
    public Joystick MoveJoystick;
    public Joystick LookJoystick;

    public void ActivateControls()
    {
        MoveJoystick.gameObject.SetActive(true);
        LookJoystick.gameObject.SetActive(true);
        Debug.Log("Activated");
    }
}
