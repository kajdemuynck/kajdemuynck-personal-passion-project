using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUD : MonoBehaviourPunCallbacks
{
    PlayerManager pm;
    [SerializeField] Canvas touchControls;
    [SerializeField] TMP_Text itemDescriptionText;
    [SerializeField] GameObject pauseMenu;
    [SerializeField] Button resumeButton;
    [SerializeField] GameObject robberOverlay;
    [SerializeField] TMP_Text moneyText;

    private void Awake()
    {
        if (Application.isMobilePlatform)
            touchControls.gameObject.SetActive(true);
    }

    public void SetPlayerManager(int pvid)
    {
        pm = PhotonView.Find(pvid).GetComponent<PlayerManager>();
    }

    public void ShowRobberOverlay()
    {
        robberOverlay.SetActive(true);
    }

    public void ShowDescription(string description)
    {
        itemDescriptionText.gameObject.SetActive(true);
        itemDescriptionText.text = description;
    }

    public void HideDescription()
    {
        itemDescriptionText.gameObject.SetActive(false);
        itemDescriptionText.text = "";
    }

    public void SetMoney(int money)
    {
        moneyText.text = string.Format("${0}", money);
    }

    public void ShowPauseMenu()
    {
        Cursor.visible = true;
        if (!Application.isMobilePlatform)
            Cursor.lockState = CursorLockMode.None;
        else
            touchControls.gameObject.SetActive(false);
        resumeButton.Select();
        pauseMenu.SetActive(true);
    }

    public void HidePauseMenu()
    {
        Cursor.visible = false;
        if (!Application.isMobilePlatform)
            Cursor.lockState = CursorLockMode.Locked;
        else
            touchControls.gameObject.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void ResumeGame()
    {
        pm.IsPaused = false;
        //HidePauseMenu();
    }

    public void QuitGame()
    {
        Destroy(RoomManager.Instance.gameObject);
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
        base.OnLeftRoom();
    }
}
