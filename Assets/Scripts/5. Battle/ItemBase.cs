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


// 우클릭 시 생성 메뉴에 추가
[CreateAssetMenu(fileName = "New Item Base", menuName = "Create Item/Item Base")]
public class ItemBase : ScriptableObject
{
    public ItemType itemType;
    public string itemName;
    public string itemDescription;
    public Sprite itemImage;
    public GameObject itemPrefab;
}
