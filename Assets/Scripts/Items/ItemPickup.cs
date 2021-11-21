using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickup : MonoBehaviourPunCallbacks
{
    public int id;
    public string description = "[ITEM]";
    public bool isPickedUp = false;
    public float interactionDistance = 2f;

    private LayerMask EnvironmentLayer = 1 << 6;
    private Ray ray;
    private RaycastHit hit;

    ItemManager itemManager;

    protected void Awake()
    {
        id = CreateID();
        itemManager = GameObject.Find("ItemManager").GetComponent<ItemManager>();
        EnhancedTouchSupport.Enable();
    }

    public virtual void CheckInteraction(PlayerInput playerInput)
    {
        if (playerInput.actions["Interact"].ReadValue<float>() > 0 && !Application.isMobilePlatform)
        {
            itemManager.RemoveItem(gameObject);
        }
        else if (Application.isMobilePlatform && Touch.activeFingers.Count > 0)
        {
            foreach (Finger finger in Touch.activeFingers)
            {
                Ray ray = Camera.main.ScreenPointToRay(finger.screenPosition); // position in px
                ray.origin = Camera.main.transform.position;
                if (Physics.Raycast(ray, out hit, 10f, ~EnvironmentLayer))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        itemManager.RemoveItem(gameObject);
                    }
                    break;
                }
            }
        }
    }

    private int CreateID()
    {
        string idChild = gameObject.transform.GetSiblingIndex().ToString("000");
        string idParent = gameObject.transform.parent.GetSiblingIndex().ToString("000");
        return int.Parse(string.Format("{0}{1}", idChild, idParent));
    }

    //protected void OnMouseOver()
    //{
    //    if (GetDistanceFromCamera(transform.position) <= interactionDistance && !isPickedUp)
    //        hud.ShowDescription(description);
    //    else
    //        hud.HideDescription();
    //}

    //protected void OnMouseExit()
    //{
    //    hud.HideDescription();
    //}

    //protected virtual void OnMouseDown()
    //{
    //    if (GetDistanceFromCamera(transform.position) <= interactionDistance)
    //    {
    //        RemoveItem();
    //    }
    //}

    //protected float GetDistanceFromCamera(Vector3 pos)
    //{
    //    Transform camera = Camera.main.transform;
    //    float dist = Vector3.Distance(camera.position, pos);
    //    return dist;
    //}
}
