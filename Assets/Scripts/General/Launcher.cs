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

    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text roomsAmountText;
    [SerializeField] TMP_Text roomCodeError;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text disconnectedText;
    [SerializeField] Transform roomListContent;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] GameObject startGameButton;
    [SerializeField] GameObject scanCodeButton;
    [SerializeField] Toggle publicToggle;

    [SerializeField] private RawImage rawImageQRcode;
    private Texture2D encodedQRcode;

    private int maxPlayersOnServer = 20;
    private byte maxPlayersPerRoom = 6;
    private List<RoomInfo> roomList = new List<RoomInfo>();

    // Setup

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //if (PlayerPrefs.HasKey("username"))
        //    usernameInput.text = PlayerPrefs.GetString("username");
        //MenuManager.Instance.OpenMenu("login");
                
        encodedQRcode = new Texture2D(256, 256);

        if (!Application.isMobilePlatform)
            scanCodeButton.gameObject.SetActive(false);

        if (!PhotonNetwork.IsConnected)
            ConnectToServer();
        else
        {
            if (PhotonNetwork.InRoom)
                OnJoinedRoom();
            else
                MenuManager.Instance.OpenMenu("main");
        }
    }

    // Connecting

    public void ConnectToServer()
    {
        //string username = usernameInput.text;
        Debug.Log("Connecting to server...");
        if (PhotonNetwork.CountOfPlayers < maxPlayersOnServer)
        {
            PhotonNetwork.NickName = GenerateUniqueUsername();
            //PlayerPrefs.SetString("username", username);
            PhotonNetwork.GameVersion = "0.0.1";
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
            MenuManager.Instance.OpenMenu("connecting");
        }
        else
        {
            disconnectedText.text = "Server is full. Please try again later";
            MenuManager.Instance.OpenMenu("disconnected");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        MenuManager.Instance.OpenMenu("main");
        StartCoroutine(UpdatePing());
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            Destroy(RoomManager.Instance.gameObject);
            SceneManager.LoadScene(0);
        }

        StopCoroutine(UpdatePing());
        disconnectedText.text = "Disconnected from server";
        MenuManager.Instance.OpenMenu("disconnected");
    }

    // Create room

    public void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = false,
            IsOpen = true
        };
        roomOptions.BroadcastPropsChangeToAll = true;
        PhotonNetwork.CreateRoom(GenerateUniqueRoomName(), roomOptions, null);
        MenuManager.Instance.OpenMenu("loading");
        //PlayerPrefs.SetString("roomname", roomNameInput.text);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        roomCodeError.text = "Room creation failed: " + message;
        MenuManager.Instance.OpenMenu("error");
        Debug.Log("OnCreateRoomFailed");
    }

    public void OnAccessStateChanged()
    {
        PhotonNetwork.CurrentRoom.IsVisible = publicToggle.isOn;
    }

    // Find room

    //public void JoinRoom(RoomInfo info)
    //{
    //    PhotonNetwork.JoinRoom(info.Name);
    //    MenuManager.Instance.OpenMenu("loading");
    //}

    public void JoinRoom(TMP_InputField inputField)
    {
        JoinRoom(inputField.text);
    }

    public void JoinRoom(string code)
    {
        bool isJoined = PhotonNetwork.JoinRoom(code);
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
        Debug.Log("OnJoinRoomFailed");
        roomCodeError.text = "Failed to join room";
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
            Destroy(child.gameObject);

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().Setup(players[i]);
        }

        float width = playerListContent.gameObject.GetComponent<RectTransform>().sizeDelta.x;
        float height = (players.Length * 28) + 8;
        playerListContent.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

        if (!PhotonNetwork.IsMasterClient)
        {
            publicToggle.interactable = false;
            publicToggle.transform.GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            publicToggle.interactable = true;
            publicToggle.transform.GetChild(0).gameObject.SetActive(true);
            publicToggle.isOn = PhotonNetwork.CurrentRoom.IsVisible;
        }

        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().Setup(newPlayer);

        float width = playerListContent.gameObject.GetComponent<RectTransform>().sizeDelta.x;
        float heightItem = playerListContent.GetComponent<RectTransform>().sizeDelta.y;
        float heightSpacing = playerListContent.GetComponent<VerticalLayoutGroup>().spacing;
        float heightPaddingTop = playerListContent.GetComponent<VerticalLayoutGroup>().padding.top;
        float heightPaddingBottom = playerListContent.GetComponent<VerticalLayoutGroup>().padding.bottom;
        float height = (PhotonNetwork.CurrentRoom.PlayerCount * (heightItem + heightSpacing)) + (heightPaddingTop + heightPaddingBottom);
        playerListContent.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
                PhotonNetwork.CurrentRoom.IsOpen = false;
            else if (PhotonNetwork.CurrentRoom.IsOpen == false)
                PhotonNetwork.CurrentRoom.IsOpen = true;
        }

        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1);
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            startGameButton.GetComponent<Button>().Select();
    }

    public override void OnPlayerLeftRoom(Player newPlayer)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1);
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            startGameButton.GetComponent<Button>().Select();
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
        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1);
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            startGameButton.GetComponent<Button>().Select();
        publicToggle.interactable = true;
        publicToggle.transform.GetChild(0).gameObject.SetActive(true);
    }

    // Update

    public override void OnRoomListUpdate(List<RoomInfo> roomListChanged)
    {
        for (int i = 0; i < roomListChanged.Count; i++)
        {
            //Debug.Log(string.Format("{0}: {1} and {2}", roomListChanged[i].Name, roomListChanged[i].IsVisible, roomListChanged[i].IsOpen));
            //roomListNames.Add(roomList[i].Name);

            //Debug.Log(roomListChanged[i].Name);

            if (roomList.Contains(roomListChanged[i]) && (roomListChanged[i].RemovedFromList || !roomListChanged[i].IsVisible || !roomListChanged[i].IsOpen || roomListChanged[i].PlayerCount == 0 || roomListChanged[i].MaxPlayers == 0))
                roomList.Remove(roomListChanged[i]);

            if (!roomList.Contains(roomListChanged[i]) && roomListChanged[i].IsVisible && roomListChanged[i].IsOpen && roomListChanged[i].PlayerCount != 0 && roomListChanged[i].MaxPlayers != 0)
                roomList.Add(roomListChanged[i]);

            //if (roomListChanged[i].RemovedFromList)
            //{
            //    if (roomList.Contains(roomListChanged[i]))
            //        roomList.Remove(roomListChanged[i]);
            //}
            //else if (roomListChanged[i].IsVisible && roomListChanged[i].IsOpen)
            //{
            //    if (!roomList.Contains(roomListChanged[i]))
            //        roomList.Add(roomListChanged[i]);
            //}
        }

        foreach (Transform t in roomListContent)
            Destroy(t.gameObject);

        foreach (RoomInfo room in roomList)
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().Setup(room);

        roomsAmountText.text = string.Format("({0})", roomList.Count);
        float width = roomListContent.gameObject.GetComponent<RectTransform>().sizeDelta.x;
        float heightItem = roomListContent.GetComponent<RectTransform>().sizeDelta.y;
        float heightSpacing = roomListContent.GetComponent<VerticalLayoutGroup>().spacing;
        float heightPaddingTop = roomListContent.GetComponent<VerticalLayoutGroup>().padding.top;
        float heightPaddingBottom = roomListContent.GetComponent<VerticalLayoutGroup>().padding.bottom;
        float height = (roomList.Count * (heightItem + heightSpacing)) + (heightPaddingTop + heightPaddingBottom);
        roomListContent.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Text toggleText = publicToggle.transform.GetChild(1).gameObject.GetComponent<Text>();
        toggleText.text = !PhotonNetwork.CurrentRoom.IsVisible && !PhotonNetwork.IsMasterClient ? "Private" : "Public";
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
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.IsMessageQueueRunning = false;
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
            for (int i = 0; i < roomList.Count; i++)
                if (name == roomList[i].Name)
                    isTaken = true;
        }
        while (isTaken);

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

    private string GenerateUniqueUsername()
    {
        string[] adjectives = new string[] { "Fluffy", "Adorable", "Charming", "Generous", "Courageous", "Plucky", "Fabulous", "Rousing", "Dazzling", "Lustrous", "Ravishing", "Spellbinding", "Nifty", "Gnarly", "Bodacious", "Magnificent", "Outlandish", "Quirky", "Whimsical", "Pompous" };
        string[] nouns = new string[] { "Pancake", "Cupcake", "Cucumber", "Pumpkin", "Toast", "Pudding", "Jelly Bean", "Pickle", "Cherry", "Potato", "Unicorn", "Aardvark", "Hamster", "Kitten", "Puppy", "Bumblebee", "Goose", "Ladybug", "Badger", "Jabberwocky", "Nimrod", "Pollywog", "Rainbow", "Sponge", "Plunger", "Spoon", "Sombrero", "Pantaloon" };

        List<string> usernames = new List<string>();
        foreach (Player player in PhotonNetwork.PlayerList)
            usernames.Add(player.NickName);

        string username;
        do username = string.Format("{0} {1}", adjectives[Random.Range(0, adjectives.Length)], nouns[Random.Range(0, nouns.Length)]);
        while (usernames.Contains(username));

        return username;
    }
}
