using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject key;

    public bool isKey = false;
    public bool isOpen = false;
    private float interactionDistance = 2f;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        SetKey(isKey);
    }

    public void SetKey(bool _isKey)
    {
        isKey = _isKey;
        key.SetActive(_isKey);
    }


    bool isPlaying(string stateName)
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
                anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;
        else
            return false;
    }

    public void Interact(RaycastHit hit, bool isInteracting)
    {
        if (hit.distance <= interactionDistance && !isPlaying(isOpen ? "DoorOpen" : "DoorClose"))
        {
            if (hit.collider.gameObject.name == "door" || hit.collider.gameObject.name == "doorlocked")
            {
                if (!isOpen)
                {
                    if (isKey)
                    {
                        HUD.Instance.ShowDescription("Open door");

                        if (isInteracting)
                        {
                            anim.Play("DoorOpen", 0, 0.0f);
                            isOpen = true;
                        }
                    }
                    else
                    {
                        HUD.Instance.ShowDescription("Need key");
                    }
                }
                else
                {
                    HUD.Instance.ShowDescription("Close door");

                    if (isInteracting)
                    {
                        anim.Play("DoorClose", 0, 0.0f);
                        isOpen = false;
                    }
                }
            }
            else if (hit.collider.gameObject.name == "lock")
            {
                if (isKey)
                {
                    if (Inventory.Instance.CheckInventory("key"))
                    {
                        HUD.Instance.ShowDescription("Already have a key");
                    }
                    else
                    {
                        HUD.Instance.ShowDescription("Take key");

                        if (isInteracting)
                        {
                            Inventory.Instance.SetItemInInventory("key");
                            SetKey(false);
                        }
                    }
                }
                else
                {
                    if (Inventory.Instance.CheckInventory("key"))
                    {
                        HUD.Instance.ShowDescription("Place key");

                        if (isInteracting)
                        {
                            Inventory.Instance.SetItemInInventory("key");
                            SetKey(true);
                        }
                    }
                    else
                    {
                        HUD.Instance.ShowDescription("Need key");
                    }
                }
            }
        }
    }
}
