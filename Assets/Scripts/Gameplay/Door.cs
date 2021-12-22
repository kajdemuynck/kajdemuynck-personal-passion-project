using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    PhotonView pv;
    [SerializeField] GameObject key;

    [SerializeField] AudioSource doorAudio;
    [SerializeField] AudioSource lockAudio;

    [SerializeField] AudioClip openAudioClip;
    [SerializeField] AudioClip closeAudioClip;
    [SerializeField] AudioClip lockAudioClip;
    [SerializeField] AudioClip unlockAudioClip;

    public bool isKey;
    public bool isOpen
        ;
    private float interactionDistance = 2f;

    private Animator anim;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        anim = GetComponent<Animator>();
        SetKey(isKey);
        SyncDoor(isOpen);
    }

    public void SetKey(bool _isKey)
    {
        isKey = _isKey;
        key.SetActive(_isKey);
        if (isKey)
        {
            lockAudio.clip = unlockAudioClip;
            lockAudio.Play();
        }
        else
        {
            lockAudio.clip = lockAudioClip;
            lockAudio.Play();
        }
    }

    bool isPlaying(string stateName)
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
                anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;
        else
            return false;
    }

    public bool Interact(RaycastHit hit, bool isInteracting)
    {
        if (hit.distance <= interactionDistance && !isPlaying(isOpen ? "DoorOpen" : "DoorClose"))
        {
            if (hit.collider.gameObject.name == "door" || hit.collider.gameObject.name == "doorlocked")
            {
                if (!isOpen)
                {
                    if (isKey)
                    {
                        GameplayManager.Instance.ShowDescription("Open door");

                        if (isInteracting)
                        {
                            SyncDoor(true);
                        }
                    }
                    else
                    {
                        GameplayManager.Instance.ShowDescription("Need key");
                    }
                }
                else
                {
                    GameplayManager.Instance.ShowDescription("Close door");

                    if (isInteracting)
                    {
                        SyncDoor(false);
                    }
                }
            }
            else if (hit.collider.gameObject.name == "lock")
            {
                if (isKey)
                {
                    if (Inventory.Instance.CheckInventory("key"))
                    {
                        GameplayManager.Instance.ShowDescription("Already have a key");
                    }
                    else
                    {
                        GameplayManager.Instance.ShowDescription("Take key");

                        if (isInteracting)
                        {
                            Inventory.Instance.SetItemInInventory("key");
                            SyncKey(false);
                        }
                    }
                }
                else
                {
                    if (Inventory.Instance.CheckInventory("key"))
                    {
                        GameplayManager.Instance.ShowDescription("Place key");

                        if (isInteracting)
                        {
                            Inventory.Instance.SetItemInInventory("key");
                            SyncKey(true);
                        }
                    }
                    else
                    {
                        GameplayManager.Instance.ShowDescription("Need key");
                    }
                }
            }

            return true;
        }
        else
            return false;
    }

    public void SyncDoor(bool _isOpen)
    {
        pv.RPC("RPC_SyncDoor", RpcTarget.All, _isOpen);
    }

    [PunRPC]
    private void RPC_SyncDoor(bool _isOpen)
    {
        if (_isOpen)
        {
            anim.Play("DoorOpen", 0, 0.0f);
            doorAudio.clip = openAudioClip;
            doorAudio.Play();
        }
        else
        {
            anim.Play("DoorClose", 0, 0.0f);
            doorAudio.clip = closeAudioClip;
            doorAudio.Play();
        }

        isOpen = _isOpen;
    }

    public void SyncKey(bool _isKey)
    {
        pv.RPC("RPC_SyncKey", RpcTarget.All, _isKey);
    }

    [PunRPC]
    private void RPC_SyncKey(bool _isKey)
    {
        SetKey(_isKey);
    }
}
