using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private int value = 20;
    private HUD hud;

    private void Start()
    {
        hud = GameObject.Find("HUD").GetComponent<HUD>();
        Debug.Log(hud.name);
    }

    private void OnMouseOver()
    {
        Transform camera = Camera.main.transform;
        float dist = Vector3.Distance(camera.position, transform.position);
        if (dist <= 2)
            hud.ShowValue(value);
        else
            hud.HideValue();
    }

    private void OnMouseExit()
    {
        hud.HideValue();
    }

    public void SetValue(int _value)
    {
        value = _value;
    }
}
