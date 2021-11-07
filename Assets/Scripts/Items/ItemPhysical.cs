using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemPhysical : MonoBehaviourPunCallbacks
{
    PhotonView pv;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (pv.Owner != PhotonNetwork.LocalPlayer)
            pv.RequestOwnership();
    }
}
