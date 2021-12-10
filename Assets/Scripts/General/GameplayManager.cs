using System.Collections;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameplayManager : MonoBehaviourPunCallbacks
{
    public static GameplayManager Instance;

    PhotonView pv;
    public PlayerManager pm;
    public PlayerController pc;
    [SerializeField] Canvas touchControls;
    [SerializeField] TMP_Text itemDescriptionText;
    [SerializeField] GameObject activeMenu;
    [SerializeField] GameObject pauseMenu;
    [SerializeField] TMP_Text timerText;
    [SerializeField] GameObject robberOverlay;
    [SerializeField] GameObject agentOverlay;
    [SerializeField] TMP_Text moneyText;
    [SerializeField] GameObject overviewCamera;
    [SerializeField] GameObject endScreen;
    [SerializeField] TMP_Text resultTitle;
    [SerializeField] TMP_Text resultInfo;
    [SerializeField] TMP_Text resultMoney;
    [SerializeField] TMP_Text resultTime;
    [SerializeField] GameObject leaveButton;
    [SerializeField] GameObject lobbyButton;

    public float startTime = 0f;
    public int maxTime = 15 * 60;
    private float endTime = 0f;
    public float sensitivity = 1;

    private void Awake()
    {
        if (!PhotonNetwork.IsConnected)
            SceneManager.LoadScene(0);

        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
        //DontDestroyOnLoad(gameObject);

        pv = GetComponent<PhotonView>();

        if (Application.isMobilePlatform)
            touchControls.enabled = true;
    }

    //private void Start()
    //{
    //    PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), Vector3.zero, Quaternion.identity);
    //}

    public bool CheckIfMatchIsFinished()
    {
        bool isFinished = true;

        PlayerManager[] playerManagers = FindObjectsOfType<PlayerManager>();

        if (PhotonNetwork.CurrentRoom.PlayerCount == playerManagers.Length)
        {
            foreach (PlayerManager playerManager in playerManagers)
            {
                if (playerManager.role == "robber" && !playerManager.isArrested && !playerManager.hasFinishedSpree)
                    isFinished = false;

                if (playerManager.role == null || playerManager.role == "")
                    isFinished = false;
            }

            if (PhotonNetwork.IsMasterClient && isFinished)
                EndMatch();
        }

        return isFinished;
    }

    public void SwitchToMainCamera(bool toCamera)
    {
        overviewCamera.GetComponent<Camera>().enabled = toCamera;
    }

    public void EndMatch()
    {
        float _endTime = (float) PhotonNetwork.Time - startTime;
        pv.RPC("RPC_EndMatch", RpcTarget.All, _endTime);
    }

    [PunRPC]
    private void RPC_EndMatch(float _endTime)
    {
        endTime = _endTime;
        PlayerManager[] playerManagers = FindObjectsOfType<PlayerManager>();

        overviewCamera.GetComponent<Camera>().enabled = true;
        timerText.gameObject.SetActive(false);

        int robbers = 0;
        int agents = 0;
        int arrested = 0;

        foreach (PlayerManager playerManager in playerManagers)
        {
            if (playerManager.role == "robber")
                robbers++;
            else
                agents++;

            if (playerManager.isArrested)
                arrested++;
        }

        string resultTitleText = "";
        string resultInfoText = "";
        string resultMoneyText = "";
        string resultTimeText = "";

        int escaped = robbers - arrested;

        if (pm.role == "robber")
        {
            // Title
            if (arrested == 0)
                resultTitleText = "Everyone got away";
            else if (arrested == robbers)
                resultTitleText = "Everyone got caught";
            else
                resultTitleText = string.Format("{0} robber{1} got away", escaped, escaped != 1 ? "s" : "");

            // Info
            if (!pm.isArrested && (int)PhotonNetwork.LocalPlayer.CustomProperties["money"] > 0)
                resultInfoText = string.Format("You stole ${0}", (int) PhotonNetwork.LocalPlayer.CustomProperties["money"]);

            // Money
            if (arrested < robbers)
                resultMoneyText = string.Format("For a total amount of ${0}", (int) PhotonNetwork.CurrentRoom.CustomProperties["totalmoney"]);

            // Time
            if (endTime <= maxTime)
            {
                int[] timeArr = GetMinutesAndSeconds(endTime + 1);
                if (timeArr[0] > 0)
                    resultTimeText = string.Format("In {0}{1} minute{2} and {3} second{4}", endTime <= 300 ? "only " : "", timeArr[0], timeArr[0] != 1 ? "s" : "", timeArr[1], timeArr[1] != 1 ? "s" : "");
                else if (timeArr[1] == 0)
                    resultTimeText = string.Format("In {0}{1} minute{2}", endTime <= 300 ? "only " : "", timeArr[0], timeArr[0] != 1 ? "s" : "");
                else
                    resultTimeText = string.Format("In only {0} second{1}", timeArr[1] + 1, timeArr[1] + 1 != 1 ? "s" : "");
            }
        }
        else if (pm.role == "agent")
        {
            // Title
            if (arrested == 0)
                resultTitleText = "The robbers got away";
            else if (arrested == robbers)
                resultTitleText = "The robbers were caught";
            else
                resultTitleText = string.Format("{0} robber{1} got away", escaped, escaped != 1 ? "s" : "");

            // Info
            int arrests = (int) PhotonNetwork.LocalPlayer.CustomProperties["arrests"];

            if (arrests > 0)
                resultInfoText = string.Format("You arrested {0} robber{1}", (int) PhotonNetwork.LocalPlayer.CustomProperties["arrests"], robbers != 1 ? "s" : "");

            // Money
            if (arrested < robbers)
                resultMoneyText = string.Format("They were able to steal ${0}", (int) PhotonNetwork.CurrentRoom.CustomProperties["totalmoney"]);

            // Time
            if (endTime <= maxTime)
            {
                int[] timeArr = GetMinutesAndSeconds(endTime + 1);
                if (timeArr[0] > 0)
                    resultTimeText = string.Format("In {0}{1} minute{2} and {3} second{4}", endTime <= 300 ? "only " : "", timeArr[0], timeArr[0] != 1 ? "s" : "", timeArr[1], timeArr[1] != 1 ? "s" : "");
                else if (timeArr[1] == 0)
                    resultTimeText = string.Format("In {0}{1} minute{2}", endTime <= 300 ? "only " : "", timeArr[0], timeArr[0] != 1 ? "s" : "");
                else
                    resultTimeText = string.Format("In only {0} second{1}", timeArr[1] + 1, timeArr[1] + 1 != 1 ? "s" : "");
            }
        }

        if (resultTitleText != "")
            resultTitle.gameObject.SetActive(true);
        if (resultInfoText != "")
            resultInfo.gameObject.SetActive(true);
        if (resultMoneyText != "")
            resultMoney.gameObject.SetActive(true);
        if (resultTimeText != "")
            resultTime.gameObject.SetActive(true);

        resultTitle.text = resultTitleText;
        resultInfo.text = resultInfoText;
        resultMoney.text = resultMoneyText;
        resultTime.text = resultTimeText;

        if (pm.role == "robber")
            robberOverlay.SetActive(false);
        if (pm.role == "agent")
            agentOverlay.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
            lobbyButton.SetActive(true);

        pm.DisableCameraBinding();
        PhotonNetwork.Destroy(pc.pv);
        PhotonNetwork.Destroy(pm.pv);

        endScreen.SetActive(true);

        Cursor.visible = true;

        if (!Application.isMobilePlatform)
            Cursor.lockState = CursorLockMode.None;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        lobbyButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void SetPlayerManager(int pvid)
    {
        pm = PhotonView.Find(pvid).GetComponent<PlayerManager>();
    }

    public void SetPlayerController(int pvid)
    {
        pc = PhotonView.Find(pvid).GetComponent<PlayerController>();
    }

    public void SetTime(float time)
    {
        int[] timeArr = GetMinutesAndSeconds(time);
        timerText.text = string.Format("{0}:{1}", timeArr[0], timeArr[1].ToString("00"));
    }

    private int[] GetMinutesAndSeconds(float time)
    {
        int minutes = (int) Mathf.Floor(time - (time % 60));
        int seconds = (int) Mathf.Floor(time - minutes);
        minutes /= 60;

        return new int[] { minutes, seconds };
    }

    public void ShowRobberOverlay()
    {
        robberOverlay.SetActive(true);
    }

    public void ShowAgentOverlay()
    {
        agentOverlay.SetActive(true);
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
        pauseMenu.GetComponent<References>().selectButton.Select();
        pauseMenu.SetActive(true);
        activeMenu = pauseMenu;
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

    public void SwitchMenu(GameObject menu)
    {
        activeMenu.SetActive(false);
        menu.SetActive(true);
        menu.GetComponent<References>().selectButton.Select();
        activeMenu = menu;
    }

    public void ChangeSensitivity(float value)
    {
        sensitivity = value;
    }

    public void ResumeGame()
    {
        pm.IsPaused = false;
        //HidePauseMenu();
    }

    public void QuitGame()
    {
        overviewCamera.GetComponent<Camera>().enabled = true;
        PhotonNetwork.Destroy(pc.pv);
        PhotonNetwork.Destroy(pm.pv);
        Destroy(RoomManager.Instance.gameObject);
        PhotonNetwork.LeaveRoom();
    }

    public void ReplayGame()
    {
        PhotonNetwork.IsMessageQueueRunning = false;
        if (PhotonNetwork.IsMasterClient)
        {
            //PhotonNetwork.DestroyAll();
            PhotonNetwork.Destroy(RoomManager.Instance.gameObject);
            PhotonNetwork.LoadLevel(0);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
        base.OnLeftRoom();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (PhotonNetwork.IsMasterClient && propertiesThatChanged.ContainsKey("totalmoney"))
        {
            CheckIfMatchIsFinished();
        }

        base.OnRoomPropertiesUpdate(propertiesThatChanged);
    }
}
