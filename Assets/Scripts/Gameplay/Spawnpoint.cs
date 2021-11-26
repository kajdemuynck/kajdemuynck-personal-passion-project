using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawnpoint : MonoBehaviour
{
    [SerializeField] GameObject graphics;
    public string role;
    public int id;

    private void Awake()
    {
        id = transform.GetSiblingIndex();
    }

    private void Start()
    {
        graphics.SetActive(false);
    }
}
