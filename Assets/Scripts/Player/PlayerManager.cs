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

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public PhotonView pv;
    public GameObject controller;

    public string role;
    private bool hasAssigned = false;
    public bool isArrested = false;
    public bool hasFinishedSpree = false;
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
    private int[] allSpots = new int[] { 0, 1, 0, 1, 0 };

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (pv.IsMine)
        {
            GameplayManager.Instance.SetPlayerManager(pv.ViewID);
            ItemManager.Instance.SetPlayerManager(pv.ViewID);

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
            Debug.Log(string.Format("Players: {0}", playerList.Length));
            Debug.Log(string.Format("PlayerManagers: {0}", playerManagers.Length));

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
            hash.Add("arrests", 0);
            hash.Add("firstarrest", 0);
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { pv.ViewID, role });
    }

    public void AssignRole(string _role, int _spawnpointId)
    {
        pv.RPC("RPC_AssignRole", RpcTarget.All, _role, _spawnpointId);
    }

    [PunRPC]
    private void RPC_AssignRole(string _role, int _spawnpointId)
    {
        Debug.Log(string.Format("Role received: {0}", _role));
        role = _role;

        if (pv.IsMine)
        {
            GameplayManager.Instance.startTime = (float) PhotonNetwork.CurrentRoom.CustomProperties["startTime"];
            GameplayManager.Instance.SwitchToMainCamera(false);

            CreateController(SpawnManager.Instance.GetSpawnpointById(_spawnpointId));
            GameplayManager.Instance.SwitchToMainCamera(false);
            controller.GetComponent<PlayerController>().cameraContainer.GetComponentInChildren<Camera>().enabled = true;

            if (_role == "robber")
            {
                GameplayManager.Instance.ShowRobberOverlay();
            }
            else if (_role == "agent")
            {
                GameplayManager.Instance.ShowAgentOverlay();
            }
        }

        Debug.Log(string.Format("{0}: {1}", pv.Owner, role));
    }

    public void SetArrested(bool _isArrested)
    {
        pv.RPC("RPC_SetArrested", RpcTarget.All, _isArrested);
    }

    [PunRPC]
    private void RPC_SetArrested(bool _isArrested)
    {
        isArrested = _isArrested;
        GameplayManager.Instance.CheckIfMatchIsFinished();
    }

    public void FinishSpree()
    {
        pv.RPC("RPC_FinishSpree", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_FinishSpree()
    {
        hasFinishedSpree = true;

        int moneyCollected = (int)PhotonNetwork.LocalPlayer.CustomProperties["money"];
        int previousTotal = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalmoney"];
        int totalMoney = previousTotal + moneyCollected;

        Hashtable hash = new Hashtable();
        hash.Add("totalmoney", totalMoney);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        //Debug.Log(string.Format("Total money collected: {0}", totalMoney));
        //GameplayManager.Instance.CheckIfMatchIsFinished();
    }

    public void Die()
    {
        // Die
        PhotonNetwork.Destroy(controller);
        // Respawn
        CreateController(SpawnManager.Instance.GetSpawnpointById(SpawnManager.Instance.GetSpawnpointIdByRole(role)));
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (pv.IsMine && targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey("money"))
            GameplayManager.Instance.SetMoney((int) PhotonNetwork.LocalPlayer.CustomProperties["money"]);
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
    }
}
