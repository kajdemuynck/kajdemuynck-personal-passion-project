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
    [SerializeField] Sprite spectateOffSprite;

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

    public void SetButtonLayout(string role)
    {
        if (role == "agent")
        {
            crouchButton.gameObject.SetActive(false);
            nightvisionButton.gameObject.SetActive(false);
        }
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

    public void SwitchToSpectateControls()
    {
        MoveJoystick.gameObject.SetActive(false);
        LookJoystick.gameObject.SetActive(false);
        crouchButton.GetComponent<Image>().sprite = spectateOffSprite;
        crouchButton.transform.position = new Vector3(72, crouchButton.transform.position.y, crouchButton.transform.position.z);
        nightvisionButton.transform.position = new Vector3(Screen.width - 72, nightvisionButton.transform.position.y, nightvisionButton.transform.position.z);
    }
}
