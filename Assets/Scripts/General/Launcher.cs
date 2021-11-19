using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;

public class Launcher : MonoBehaviourPunCallbacks
{
    // Variables

    public static Launcher Instance;

    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField roomNameInput;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text roomsAmountText;
    [SerializeField] TMP_Text roomCodeError;
    [SerializeField] Transform roomListContainer;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] GameObject startGameButton;
    [SerializeField] Toggle publicToggle;

    [SerializeField] private RawImage rawImageQRcode;
    private Texture2D encodedQRcode;

    private byte maxPlayersPerRoom = 6;
    private List<string> roomListNames = new List<string>();

    // Setup

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PlayerPrefs.HasKey("username"))
            usernameInput.text = PlayerPrefs.GetString("username");
        if (PlayerPrefs.HasKey("roomname"))
            roomNameInput.text = PlayerPrefs.GetString("roomname");
        MenuManager.Instance.OpenMenu("login");
        encodedQRcode = new Texture2D(256, 256);
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
        //Hashtable hash = new Hashtable();
        //hash.Add("Access", "private");

        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = false
        };
        roomOptions.BroadcastPropsChangeToAll = true;
        //roomOptions.CustomRoomPropertiesForLobby = new string[]
        //{
        //     "Access"
        //};
        PhotonNetwork.CreateRoom(GenerateUniqueRoomName(), roomOptions, null);
        MenuManager.Instance.OpenMenu("loading");
        //PlayerPrefs.SetString("roomname", roomNameInput.text);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        //errorText.text = "Room creation failed: " + message;
        MenuManager.Instance.OpenMenu("error");
    }

    public void OnAccessStateChanged()
    {
        Debug.Log(publicToggle.isOn);
        PhotonNetwork.CurrentRoom.IsVisible = publicToggle.isOn;
        //Hashtable hash = new Hashtable();
        //hash.Add("Access", publicToggle.isOn ? "public" : "private");
        //PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    // Find room

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("loading");
    }

    public void JoinRoom(TMP_InputField inputField)
    {
        string code = inputField.text;
        bool isJoined = PhotonNetwork.JoinRoom(code);
        Debug.Log(isJoined);
        if (isJoined)
        {
            MenuManager.Instance.OpenMenu("loading");
            roomCodeError.gameObject.SetActive(false);
        }
        else
        {
            roomCodeError.text = "Room is full or doesn't exist";
            roomCodeError.gameObject.SetActive(true);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        roomCodeError.text = "Room joining failed: " + message;
        roomCodeError.gameObject.SetActive(true);
        //MenuManager.Instance.OpenMenu("error");
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    // Room

    public override void OnJoinedRoom()
    {
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        MenuManager.Instance.OpenMenu("room");

        EncodeTextToQRcode(PhotonNetwork.CurrentRoom.Name);

        Player[] players = PhotonNetwork.PlayerList;
        
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().Setup(players[i]);
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            publicToggle.interactable = false;
            publicToggle.transform.GetChild(0).gameObject.SetActive(false);
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
        base.OnLeftRoom();
        MenuManager.Instance.OpenMenu("main");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    // Update

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        roomListNames.Clear();
        foreach (Transform t in roomListContainer)
        {
            Destroy(t.gameObject);
        }

        int rooms = 0;
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList)
            {
                continue;
            }
            roomListNames.Add(roomList[i].Name);
            //Debug.Log(string.Format("{0}: {1}", roomList[i].Name, (string) roomList[i].CustomProperties["Access"]));

            if (roomList[i].IsVisible)
            {
                rooms++;
                Instantiate(roomListItemPrefab, roomListContainer).GetComponent<RoomListItem>().Setup(roomList[i]);
            }
        }
        roomsAmountText.text = string.Format("({0})", rooms);
    }

    public IEnumerator UpdatePing()
    {
        while (true && PhotonNetwork.IsConnected)
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

    // Miscellaneous

    private string GenerateUniqueRoomName()
    {
        bool isTaken = false;
        string roomName;

        do
        {
            roomName = (Random.Range(0, 10000)).ToString("0000");
            for (int i = 0; i < roomListNames.Count; i++)
                if (name == roomListNames[i])
                    isTaken = true;
        } while (isTaken);

        return roomName;
    }

    private Color32[] EncodeBarcode(string code, int width, int height)
    {
        BarcodeWriter writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = width,
                Height = height
            }
        };

        return writer.Write(code);
    }

    private void EncodeTextToQRcode(string code)
    {
        Color32[] convertPixelToTexture = EncodeBarcode(code, encodedQRcode.width, encodedQRcode.height);
        encodedQRcode.SetPixels32(convertPixelToTexture);
        encodedQRcode.Apply();
        rawImageQRcode.texture = encodedQRcode;
    }
}
