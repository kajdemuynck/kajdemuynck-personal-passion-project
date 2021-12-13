using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchControls : MonoBehaviour
{
    public static TouchControls Instance;

    public Joystick MoveJoystick;
    public Joystick LookJoystick;
    public Button interactButton;
    public Button crouchButton;
    public Button nightvisionButton;
    public Button pauseButton;
    [SerializeField] Sprite crouchOffSprite;
    [SerializeField] Sprite crouchOnSprite;
    [SerializeField] Sprite nvOffSprite;
    [SerializeField] Sprite nvOnSprite;

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
        interactButton.gameObject.SetActive(true);
    }

    public void CrouchButtonToggle(bool state)
    {
        if (state)
            crouchButton.GetComponent<Image>().sprite = crouchOnSprite;
        else
            crouchButton.GetComponent<Image>().sprite = crouchOffSprite;
    }

    public void NightVisionButtonToggle(bool state)
    {
        if (state)
            nightvisionButton.GetComponent<Image>().sprite = nvOnSprite;
        else
            nightvisionButton.GetComponent<Image>().sprite = nvOffSprite;
    }
}
