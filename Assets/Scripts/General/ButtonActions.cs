using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonActions : MonoBehaviour
{
    public bool isPressed;

    private void Awake()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(OnPress);
    }

    public void OnPress()
    {
        isPressed = true;
    }
}
