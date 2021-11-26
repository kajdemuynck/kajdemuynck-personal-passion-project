using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using System.Linq;
using System;
using Random = UnityEngine.Random;
//using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    PhotonView pv;
    HUD hud;
    public GameObject controller;

    private string role;
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
                hud.ShowPauseMenu();
            else
                hud.HidePauseMenu();
        }
    }

    private string[] roles = new string[] { "robber", "agent" };
    private int[] divisions = new int[] { 0, 1, 0, 1, 0 };

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        hud = GameObject.Find("HUD").GetComponent<HUD>();
    }

    void Start()
    {
        if (pv.IsMine)
        {
            hud.SetPlayerManager(pv.ViewID);

            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(AssignRolesToPlayers());
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
            CreateController(SpawnManager.Instance.SelectSpawnpointById(_spawnpointId));
    }

    public void Die()
    {
        // Die
        PhotonNetwork.Destroy(controller);
        // Respawn
        CreateController(SpawnManager.Instance.SelectSpawnpointById(SpawnManager.Instance.GetSpawnpointIdByRole(role)));
    }
}
