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
    PhotonView pv;
    public GameObject controller;

    public string role;
    public bool isArrested = false;
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
                HUD.Instance.ShowPauseMenu();
            else
                HUD.Instance.HidePauseMenu();
        }
    }

    private string[] roles = new string[] { "robber", "agent" };
    private int[] divisions = new int[] { 0, 1, 0, 1, 0 };

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (pv.IsMine)
        {
            HUD.Instance.SetPlayerManager(pv.ViewID);
            ItemManager.Instance.SetPlayerManager(pv.ViewID);

            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(AssignRolesToPlayers());

                Hashtable hash = new Hashtable();
                hash.Add("totalmoney", 0);
                PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
            }
        }
    }

    public IEnumerator AssignRolesToPlayers()
    {
        Player[] playerList;
        PlayerManager[] playerManagers;

        do
        {
            playerList = PhotonNetwork.CurrentRoom.Players.Values.ToArray();
            playerManagers = FindObjectsOfType<PlayerManager>();
            Debug.Log("Players: " + playerList.Length);
            Debug.Log("Managers: " + playerManagers.Length);

            yield return null;
        }
        while (playerList.Length > playerManagers.Length);

        Dictionary<Player, PlayerManager> playerManagersOfPlayers = new Dictionary<Player, PlayerManager>();

        foreach (PlayerManager pm in playerManagers)
        {
            int i = Array.IndexOf(playerList, pm.gameObject.GetComponent<PhotonView>().Owner);
            playerManagersOfPlayers.Add(playerList[i], pm);
        }

        int[] division = new ArraySegment<int>(divisions, 0, playerList.Length).ToArray();
        List<int> rolesTaken = new List<int>();
        List<int> spawnpointsTaken = new List<int>();

        foreach (Player player in playerList)
        {
            //Debug.Log(string.Format("{0}: {1}", player.Key, player.Value.NickName));
            int randomRole;
            do randomRole = Random.Range(0, division.Length);
            while (rolesTaken.Contains(randomRole));
            rolesTaken.Add(randomRole);
            string _role = roles[division[randomRole]];

            int randomSpawnpointId;
            do randomSpawnpointId = SpawnManager.Instance.GetSpawnpointIdByRole(_role);
            while (spawnpointsTaken.Contains(randomSpawnpointId));
            spawnpointsTaken.Add(randomSpawnpointId);

            Debug.Log(player.NickName);
            Debug.Log(roles[division[randomRole]]);

            playerManagersOfPlayers[player].AssignRole(_role, randomSpawnpointId);
        }
    }

    public void CreateController(Transform spawnpoint)
    {
        //Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
        //controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
        Hashtable hash = new Hashtable();
        hash.Add("money", 0);
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
        Debug.Log("Role received");
        Debug.Log(_role);
        role = _role;

        if (pv.IsMine)
        {
            CreateController(SpawnManager.Instance.SelectSpawnpointById(_spawnpointId));

            if (_role == "robber")
            {
                HUD.Instance.ShowRobberOverlay();
            }
        }
    }

    public void SetArrested(bool _isArrested)
    {
        pv.RPC("RPC_SetArrested", RpcTarget.All, _isArrested);
    }

    [PunRPC]
    private void RPC_SetArrested(bool _isArrested)
    {
        isArrested = _isArrested;
    }

    public void Die()
    {
        // Die
        PhotonNetwork.Destroy(controller);
        // Respawn
        CreateController(SpawnManager.Instance.SelectSpawnpointById(SpawnManager.Instance.GetSpawnpointIdByRole(role)));
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (pv.IsMine && targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey("money"))
            HUD.Instance.SetMoney((int) PhotonNetwork.LocalPlayer.CustomProperties["money"]);
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
    }
}
