using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPlaceholder : MonoBehaviour
{
    public int id;
    public string type;
    public string description = "[ITEM]";
    public bool isPickedUp = false;
    public float interactionDistance = 2f;
}
