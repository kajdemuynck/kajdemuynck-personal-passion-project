using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public PhotonView pv;
    public GameObject controller;
    public PlayerController activePlayerController;
    private PlayerController[] playerControllers;

    public string role;
    private bool hasAssigned = false;
    public bool isArrested = false;
    public bool hasEscaped = false;
    private bool isPaused = false;
    public bool IsPaused
    {
        get
        {
            return isPaused;
        }

        set
        {
            isPaused = value;
            if (value)
                GameplayManager.Instance.ShowPauseMenu();
            else
                GameplayManager.Instance.HidePauseMenu();
        }
    }

    private string[] roles = new string[] { "robber", "agent" };
    private int[] allSpots = new int[] { 0, 1, 0, 1, 0, 1 };

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (pv.IsMine)
        {
            GameplayManager.Instance.SetPlayerManager(pv.ViewID);

            if (PhotonNetwork.IsMasterClient)
            {
                Hashtable hash = new Hashtable();
                hash.Add("startTime", (float)PhotonNetwork.Time);
                hash.Add("totalmoney", 0);
                PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
            }
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("startTime") && !hasAssigned && pv.IsMine && PhotonNetwork.IsMasterClient)
        {
            Debug.Log((float)PhotonNetwork.CurrentRoom.CustomProperties["startTime"]);
            StartCoroutine(AssignRolesToPlayers());
        }

        base.OnRoomPropertiesUpdate(propertiesThatChanged);
    }

    public IEnumerator AssignRolesToPlayers()
    {
        Player[] playerList;
        PlayerManager[] playerManagers;

        do
        {
            playerList = PhotonNetwork.CurrentRoom.Players.Values.ToArray();
            playerManagers = FindObjectsOfType<PlayerManager>();
            //Debug.Log(string.Format("Players: {0}", playerList.Length));
            //Debug.Log(string.Format("PlayerManagers: {0}", playerManagers.Length));

            yield return null;
        }
        while (playerList.Length > playerManagers.Length);

        hasAssigned = true;

        Dictionary<Player, PlayerManager> playerManagersOfPlayers = new Dictionary<Player, PlayerManager>();

        foreach (PlayerManager pm in playerManagers)
        {
            int i = Array.IndexOf(playerList, pm.gameObject.GetComponent<PhotonView>().Owner);
            playerManagersOfPlayers.Add(playerList[i], pm);
        }

        int[] spots = new ArraySegment<int>(allSpots, 0, playerList.Length).ToArray();
        List<int> spotsTaken = new List<int>();
        List<int> spawnpointsTaken = new List<int>();

        foreach (Player player in playerList)
        {
            //Debug.Log(string.Format("{0}: {1}", player.Key, player.Value.NickName));
            int randomSpot;
            do randomSpot = Random.Range(0, spots.Length);
            while (spotsTaken.Contains(randomSpot));
            spotsTaken.Add(randomSpot);
            string _role = roles[spots[randomSpot]];

            int randomSpawnpointId;
            do randomSpawnpointId = SpawnManager.Instance.GetSpawnpointIdByRole(_role);
            while (spawnpointsTaken.Contains(randomSpawnpointId));
            spawnpointsTaken.Add(randomSpawnpointId);

            Debug.Log(string.Format("{0}: {1}", player.NickName, roles[spots[randomSpot]]));

            playerManagersOfPlayers[player].AssignRole(_role, randomSpawnpointId);
        }
    }

    public void CreateController(Transform spawnpoint)
    {
        //Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
        //controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
        Hashtable hash = new Hashtable();
        if (role == "robber")
        {
            hash.Add("money", 0);
        }
        else
        {
            hash.Add("arrests", "");
            //hash.Add("firstarrest", 0);
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { pv.ViewID, role });
    }

    public void SetController(GameObject obj)
    {
        controller = obj;
        activePlayerController = controller.GetComponent<PlayerController>();
    }

    public void AssignRole(string _role, int _spawnpointId)
    {
        pv.RPC("RPC_AssignRole", RpcTarget.All, _role, _spawnpointId);
    }

    [PunRPC]
    private void RPC_AssignRole(string _role, int _spawnpointId)
    {
        //Debug.Log(string.Format("Role received: {0}", _role));
        role = _role;

        if (pv.IsMine)
        {
            GameplayManager.Instance.startTime = (float) PhotonNetwork.CurrentRoom.CustomProperties["startTime"];
            GameplayManager.Instance.SwitchToMainCamera(false);
            CreateController(SpawnManager.Instance.GetSpawnpointById(_spawnpointId));
            //controller.GetComponent<PlayerController>().cameraContainer.GetComponentInChildren<Camera>().enabled = true;

            if (_role == "robber")
            {
                GameplayManager.Instance.ShowRobberOverlay();
            }
            else if (_role == "agent")
            {
                GameplayManager.Instance.ShowAgentOverlay();
            }

            if (Application.isMobilePlatform)
                TouchControls.Instance.SetButtonLayout(role);
        }

        Debug.Log(string.Format("{0}: {1}", pv.Owner, role));
    }

    public void Respawn()
    {
        Transform spawnpoint = SpawnManager.Instance.GetSpawnpointById(SpawnManager.Instance.GetSpawnpointIdByRole(role));
        controller.transform.position = spawnpoint.position;
        controller.transform.rotation = spawnpoint.rotation;
    }

    public void SetArrested(bool _isArrested)
    {
        pv.RPC("RPC_SetArrested", RpcTarget.All, _isArrested);
    }

    [PunRPC]
    private void RPC_SetArrested(bool _isArrested)
    {
        isArrested = _isArrested;
        controller.GetComponent<PlayerController>().Arrested(isArrested);

        if (PhotonNetwork.IsMasterClient && GameplayManager.Instance.CheckIfMatchIsFinished())
            GameplayManager.Instance.EndMatch();
    }

    public void Escape()
    {
        pv.RPC("RPC_Escape", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RPC_Escape(int playerNumber)
    {
        if (hasEscaped && activePlayerController.pv.Owner.ActorNumber == playerNumber)
            SwitchCamera();

        hasEscaped = true;
        controller.GetComponent<PlayerController>().graphicsContainer.SetActive(false);
        Destroy(controller.GetComponent<PlayerController>().cc);

        if (pv.IsMine)
        {
            int previousTotal = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalmoney"];
            int moneyCollected = (int)PhotonNetwork.LocalPlayer.CustomProperties["money"];
            int totalMoney = previousTotal + moneyCollected;

            Hashtable hash = new Hashtable();
            hash.Add("totalmoney", totalMoney);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

            if (!GameplayManager.Instance.CheckIfMatchIsFinished())
            {
                playerControllers = FindObjectsOfType<PlayerController>();
                SwitchCamera();
                controller.GetComponent<PlayerController>().DisableControls();
                controller.GetComponent<PlayerController>().playerControls.Actions.Special01.started += SwitchCameraInput;
                controller.GetComponent<PlayerController>().playerControls.Actions.Special02.started += ToggleNightVisionInput;

                if (Application.isMobilePlatform)
                {
                    TouchControls.Instance.nightvisionButton.onClick.AddListener(ToggleNightVision);
                    TouchControls.Instance.crouchButton.onClick.AddListener(SwitchCamera);
                    TouchControls.Instance.SwitchToSpectateControls();
                }
            }
            else
            {
                //if (PhotonNetwork.IsMasterClient)
                //    GameplayManager.Instance.EndMatch();
            }
        }
        else
        {
            if (GameplayManager.Instance.pm.role == "agent")
            {
                string arrests = (string)PhotonNetwork.LocalPlayer.CustomProperties["arrests"];
                List<string> arrestsList = new List<string>(arrests.Split(';').ToList());

                Debug.Log(string.Format("Player: {0}", playerNumber));
                for (int i=0; i<arrestsList.Count; i++)
                    Debug.Log(string.Format("Arrest: {0}", arrestsList[i]));

                if (arrestsList.Contains(playerNumber.ToString()))
                    arrestsList.RemoveAt(arrestsList.IndexOf(playerNumber.ToString()));

                arrestsList = arrestsList.FindAll(v => v != "");

                for (int i = 0; i < arrestsList.Count; i++)
                    Debug.Log(string.Format("Arrest: {0}", arrestsList[i]));

                arrests = string.Join(";", arrestsList);

                //Debug.Log(string.Format("Arrests left: {0}", arrests));

                Hashtable hash = new Hashtable();
                hash.Add("arrests", arrests);
                PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            }
        }
    }

    private void SwitchCameraInput(InputAction.CallbackContext context)
    {
        SwitchCamera();
    }

    private void SwitchCamera()
    {
        activePlayerController.cameraContainer.GetComponentInChildren<Camera>().enabled = false;
        activePlayerController.cameraContainer.GetComponentInChildren<AudioListener>().enabled = false;
        activePlayerController.fl.enabled = false;
        activePlayerController.ShowGraphics(true);
        bool isNightVision = activePlayerController.nv.enabled;

        do
        {
            int index = Array.IndexOf(playerControllers, activePlayerController) + 1;
            index %= playerControllers.Length;
            activePlayerController = playerControllers[index];
        }
        while (activePlayerController.pm.role != "robber" || activePlayerController.pm.hasEscaped);

        GameplayManager.Instance.SetMoney((int) activePlayerController.pv.Owner.CustomProperties["money"]);
        activePlayerController.cameraContainer.GetComponentInChildren<Camera>().enabled = true;
        activePlayerController.cameraContainer.GetComponentInChildren<AudioListener>().enabled = true;
        activePlayerController.fl.enabled = true;
        activePlayerController.ShowGraphics(false);
        activePlayerController.nv.enabled = isNightVision;
        if (Application.isMobilePlatform)
            TouchControls.Instance.NightVisionButtonToggle(isNightVision);
    }

    private void ToggleNightVisionInput(InputAction.CallbackContext context)
    {
        ToggleNightVision();
    }

    private void ToggleNightVision()
    {
        activePlayerController.ToggleNightVision();
    }

    public void DisableCameraBinding()
    {
        if (activePlayerController != controller.GetComponent<PlayerController>())
        {
            controller.GetComponent<PlayerController>().playerControls.Actions.Special01.started -= SwitchCameraInput;
            controller.GetComponent<PlayerController>().playerControls.Actions.Special02.started -= ToggleNightVisionInput;
            if (Application.isMobilePlatform)
            {
                TouchControls.Instance.nightvisionButton.onClick.RemoveListener(ToggleNightVision);
                TouchControls.Instance.crouchButton.onClick.RemoveListener(SwitchCamera);
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (pv.IsMine && changedProps.ContainsKey("money"))
            if (activePlayerController!= null && targetPlayer == activePlayerController.pv.Owner)
                GameplayManager.Instance.SetMoney((int) targetPlayer.CustomProperties["money"]);
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
    }
}
