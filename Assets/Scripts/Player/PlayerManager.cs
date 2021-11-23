using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public class PlayerManager : MonoBehaviour
{
    PhotonView pv;

    GameObject controller;

    private string[] roles = new string[] { "robber", "agent" };
    private int[] divisions = new int[] { 0, 1, 0, 1, 0 };

    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (pv.IsMine)
            CreateController();
    }

    void Start()
    {
        if (pv.IsMine && PhotonNetwork.IsMasterClient)
        {
            //List<Player> players = new List<Player>(PhotonNetwork.CurrentRoom.Players);
            Dictionary<int, Player> playerList = PhotonNetwork.CurrentRoom.Players;

            int[] division = new ArraySegment<int>(divisions, 0, playerList.Count).ToArray();
            List<int> rolesTaken = new List<int>();

            foreach (KeyValuePair<int, Player> player in playerList)
            {
                //Debug.Log(string.Format("{0}: {1}", player.Key, player.Value.NickName));

                int randomRole;
                do randomRole = Random.Range(0, division.Length);
                while (rolesTaken.Contains(randomRole));
                rolesTaken.Add(randomRole);

                if (player.Value != PhotonNetwork.LocalPlayer)
                    AssignRole(player.Value, roles[randomRole]);
                else
                    RPC_AssignRole(roles[randomRole]);
            }

            //List<int> rolesTaken = new List<int>();
            //for (int i = 0; i < players.Length; i++)
            //{
            //    int randomRole;
            //    do randomRole = Random.Range(0, division.Length);
            //    while (!rolesTaken.Contains(randomRole));
            //    rolesTaken.Add(randomRole);
            //    if (players[i] != PhotonNetwork.LocalPlayer)
            //        Debug.Log(roles[randomRole]);
            //    //AssignRole(players[i], roles[randomRole]);
            //    else
            //        Debug.Log(roles[randomRole]);
            //    //RPC_AssignRole(roles[randomRole]);
            //}
        }
    }

    private void CreateController()
    {
        //Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
        //controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), Vector3.zero, Quaternion.identity, 0, new object[] { pv.ViewID });
    }

    public void AssignRole(Player player, string role)
    {
        pv.RPC("RPC_AssignRole", player, role);
    }

    [PunRPC]
    private void RPC_AssignRole(string role)
    {
        controller.GetComponent<PlayerController>().SetRole(role);
    }

    public void Die()
    {
        // Die
        PhotonNetwork.Destroy(controller);
        // Respawn
        CreateController();
    }
}
