using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    PhotonView pv;

    GameObject controller;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (pv.IsMine)
        {
            CreateController();
        }
    }

    private void CreateController()
    {
        //Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
        //controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), Vector3.zero, Quaternion.identity, 0, new object[] { pv.ViewID });
    }

    public void Die()
    {
        // Die
        PhotonNetwork.Destroy(controller);
        // Respawn
        CreateController();
    }
}
