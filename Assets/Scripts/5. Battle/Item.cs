using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemBase itemBase;

    private bool isGotten = false;

    public ItemBase ItemBase => itemBase;

    public bool IsGotten { get => isGotten; set { isGotten = value; } }
}
