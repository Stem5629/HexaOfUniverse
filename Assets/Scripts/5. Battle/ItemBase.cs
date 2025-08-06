using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Equipment,
    Usable,
    Quest,
    Gold,
    Etc
}


// ��Ŭ�� �� ���� �޴��� �߰�
[CreateAssetMenu(fileName = "New Item Base", menuName = "Create Item/Item Base")]
public class ItemBase : ScriptableObject
{
    public ItemType itemType;
    public string itemName;
    public string itemDescription;
    public Sprite itemImage;
    public GameObject itemPrefab;
}
