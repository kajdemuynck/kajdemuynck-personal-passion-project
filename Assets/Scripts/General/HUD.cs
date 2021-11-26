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

    private void Awake()
    {
        if (Application.isMobilePlatform)
            touchControls.gameObject.SetActive(true);
    }

    public void SetPlayerManager(int pvid)
    {
        pm = PhotonView.Find(pvid).GetComponent<PlayerManager>();
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

    public void ShowPauseMenu()
    {
        Cursor.visible = true;
        if (!Application.isMobilePlatform)
            Cursor.lockState = CursorLockMode.None;
        pauseMenu.SetActive(true);
    }

    public void HidePauseMenu()
    {
        Cursor.visible = false;
        if (!Application.isMobilePlatform)
            Cursor.lockState = CursorLockMode.Locked;
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
