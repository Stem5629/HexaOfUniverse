using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private static Inventory instance = null;
    public static Inventory Instance => instance;

    [SerializeField] private GameObject inventoryBackground;

    private Slot[] slots;
    private Slot tempSlot;
    private bool toOpenInventory = false;

    public Slot TempSlot => tempSlot;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        // ���� �迭�� ��� ���� �Ҵ�
        slots = inventoryBackground.GetComponentsInChildren<Slot>();
        tempSlot = slots[0];
    }

    private void Update()
    {
        OnOffInventory();
    }

    public void GetItem(ItemBase item, int count = 1)
    {
        // ��� �������� �ƴ� ��쿡 ��� ������ ��ȸ�Ͽ� �̹� ���Կ� �����ϴ� �������̸� ������ ����

        if (item.itemType != ItemType.Equipment)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].ItemBase != null)
                {

                    if (slots[i].ItemBase != null && slots[i].ItemBase.itemName == item.itemName) // ����
                    {
                        slots[i].SetItemCount(count);
                        return;
                    }
                }
            }
        }
        FirstGetItem(item);
    }

    private void FirstGetItem(ItemBase item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].ItemBase == null)
            {
                slots[i].AddItem(item);
                return;
            }
        }
    }

    private void OnOffInventory()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            toOpenInventory = !toOpenInventory;
            inventoryBackground.SetActive(toOpenInventory);
        }
    }
}
