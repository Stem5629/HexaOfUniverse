using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// �������̽� : �̸� �ʿ��� ����� �����س��� ����, Ŭ������ �޸� ���� ����� ����, �ݵ�� �ش� ��� �Լ��� �����ؾ� �Ѵ�.
public class Slot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image itemImage;
    [SerializeField] private GameObject itemCountImage;
    [SerializeField] private TextMeshProUGUI tmpItemCount;

    private ItemBase itemBase;
    private int itemCount;
    private Vector3 originalPosition;


    public Image ItemImage { get => itemImage; set => itemImage = value; }
    public ItemBase ItemBase { get => itemBase; set => itemBase = value; }
    public int ItemCount { get => itemCount; set => itemCount = value; }

    public void AddItem(ItemBase item, int count = 1)
    {
        itemBase = item;
        itemCount = count;
        itemImage.sprite = item.itemImage;


        // ������ �� �ִ� �������� �������� ������ ǥ�����ش�.
        if (itemBase.itemType != ItemType.Equipment)
        {
            itemCountImage.SetActive(true);
            tmpItemCount.text = itemCount.ToString();
        }
        else
        {
            tmpItemCount.text = string.Empty;
            itemCountImage.SetActive(false);
        }
        SetAlpha(1);
    }

    // �巡�� ���� �� �� �� ȣ��
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemBase != null)
        {
            originalPosition = transform.position;
            TempSlot.Instance.GetTempSlot = this;
            TempSlot.Instance.SetDiceImage(itemImage);

            TempSlot.Instance.transform.position = eventData.position;
        }
    }
    // �巡�� �߿� ȣ��
    public void OnDrag(PointerEventData eventData)
    {
        if (itemBase != null)
        {
            TempSlot.Instance.transform.position = eventData.position;
        }

    }
    // �巡�� ������ �� ȣ�� -> �巡�װ� ������ ������̵� ȣ��ȴ�.
    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("OnEndDrag");

        TempSlot.Instance.SetAlpha(0);
        TempSlot.Instance.GetTempSlot = null;
    }
    // ���콺 ������ �� ȣ�� -> �巡�װ� ���� ���� �� ��ũ��Ʈ�� �Ҵ�� ������Ʈ�� ���� ȣ��ȴ�.
    public void OnDrop(PointerEventData eventData)
    {
        //Debug.Log("OnDrop");

        // �󽽷��� �ƴ� �� ���� ü����
        if (TempSlot.Instance.GetTempSlot != null) ChangeSlot();

    }


    // ���콺 Ŭ���� ���� ��, �� ������ eventData ������ �������ش�.
    // eventData ����ڰ� �Է��� ����
    public void OnPointerClick(PointerEventData eventData)
    {
        // �޼��� ������ �������� �ʾ��� �� ����ó�� �ض�.
        //throw new System.NotImplementedException(); -> ����� ����Ѵ�.

        // ��Ŭ���� �ߴ���?
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (itemBase != null)
            {
                if (itemBase.itemType == ItemType.Equipment)
                {
                    Debug.Log("������ ����");
                }
                else if (itemBase.itemType == ItemType.Usable)
                {
                    Debug.Log(itemBase.itemName + "��(��) ����Ͽ����ϴ�.");
                    SetItemCount();
                }

            }
        }
    }


    private void ChangeSlot()
    {
        // swap
        // t = a;
        // a = b;
        // b = t;

        // t = a;
        ItemBase tempItem = itemBase;
        int tempItemCount = itemCount;

        // a = b;
        AddItem(TempSlot.Instance.GetTempSlot.itemBase, TempSlot.Instance.GetTempSlot.itemCount);

        // b = t;
        if (tempItem != null) // ���콺 ������ �� �� �ڸ��� �������� �ִٸ�
        {
            TempSlot.Instance.GetTempSlot.AddItem(tempItem, tempItemCount);
        }
        else
        {
            TempSlot.Instance.GetTempSlot.ClearSlot();
        }
    }

    // �������� ���� ����
    public void SetItemCount(int count = -1)
    {
        itemCount += count;
        tmpItemCount.text = itemCount.ToString();

        // �������� ��� �Ҹ��Ͽ��ٸ�
        if (itemCount <= 0) ClearSlot();
    }

    // ���� ����
    private void ClearSlot()
    {
        itemBase = null;
        itemCount = 0;
        itemImage.sprite = null;
        SetAlpha(0);

        tmpItemCount.text = string.Empty;
        itemCountImage.SetActive(false);

    }

    // �� ���԰� ä���� ������ ���� ����
    private void SetAlpha(float alpha)
    {
        Color color = itemImage.color;
        color.a = alpha;
        itemImage.color = color;
    }
}
