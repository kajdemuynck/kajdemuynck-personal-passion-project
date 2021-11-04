using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Launcher : MonoBehaviourPunCallbacks
{
    // Variables

    public static Launcher Instance;

    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField roomNameInput;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContainer;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] GameObject startGameButton;

    // Setup

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PlayerPrefs.HasKey("username"))
            usernameInput.text = PlayerPrefs.GetString("username");
        MenuManager.Instance.OpenMenu("login");
    }

    // Connecting

    public void ConnectToServer()
    {
        string username = usernameInput.text;
        if (username != "")
        {
            Debug.Log("Connecting to server...");
            PhotonNetwork.NickName = username;
            PlayerPrefs.SetString("username", username);
            PhotonNetwork.GameVersion = "0.0.1";
            PhotonNetwork.ConnectUsingSettings();
            MenuManager.Instance.OpenMenu("connecting");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected!");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        MenuManager.Instance.OpenMenu("main");
        StartCoroutine(UpdatePing());
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if(SceneManager.GetActiveScene().buildIndex != 0)
        {
            PhotonNetwork.LoadLevel(0);
        }

        MenuManager.Instance.OpenMenu("disconnected");
        Debug.Log("Disconnected from server: " + cause.ToString());
        StopCoroutine(UpdatePing());
    }

    // Create room

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
            return;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 6; // for example
        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions, null);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        //errorText.text = "Room creation failed: " + message;
        MenuManager.Instance.OpenMenu("error");
    }

    // Find room

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        //errorText.text = "Room creation failed: " + message;
        MenuManager.Instance.OpenMenu("error");
    }

    // Room

    public override void OnJoinedRoom()
    {
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        MenuManager.Instance.OpenMenu("room");

        Player[] players = PhotonNetwork.PlayerList;
        
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().Setup(players[i]);
        }

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().Setup(newPlayer);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnLeftRoom()
    {
        MenuManager.Instance.OpenMenu("main");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    // Update

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform t in roomListContainer)
        {
            Destroy(t.gameObject);
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList)
            {
                continue;
            }
            Instantiate(roomListItemPrefab, roomListContainer).GetComponent<RoomListItem>().Setup(roomList[i]);
        }
    }

    public IEnumerator UpdatePing()
    {
        while (true)
        {
            Hashtable hash = new Hashtable();
            hash.Add("ping", PhotonNetwork.GetPing());
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            yield return new WaitForSeconds(2f);
        }
    }

    // General

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
