using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// 인터페이스 : 미리 필요한 기능을 구현해놓은 조각, 클래스와 달리 다중 상속이 가능, 반드시 해당 멤버 함수를 구현해야 한다.
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


        // 겹쳐질 수 있는 아이템은 아이템의 개수를 표시해준다.
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

    // 드래그 시작 시 한 번 호출
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
    // 드래그 중에 호출
    public void OnDrag(PointerEventData eventData)
    {
        if (itemBase != null)
        {
            TempSlot.Instance.transform.position = eventData.position;
        }

    }
    // 드래그 끝났을 때 호출 -> 드래그가 끝나면 어느곳이든 호출된다.
    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("OnEndDrag");

        TempSlot.Instance.SetAlpha(0);
        TempSlot.Instance.GetTempSlot = null;
    }
    // 마우스 놓았을 때 호출 -> 드래그가 끝난 곳이 이 스크립트가 할당된 오브젝트일 때만 호출된다.
    public void OnDrop(PointerEventData eventData)
    {
        //Debug.Log("OnDrop");

        // 빈슬롯이 아닐 때 슬롯 체인지
        if (TempSlot.Instance.GetTempSlot != null) ChangeSlot();

    }


    // 마우스 클릭을 했을 때, 그 정보를 eventData 변수에 전달해준다.
    // eventData 사용자가 입력한 정보
    public void OnPointerClick(PointerEventData eventData)
    {
        // 메서드 내용을 구현하지 않았을 때 예외처리 해라.
        //throw new System.NotImplementedException(); -> 지우고 사용한다.

        // 우클릭을 했는지?
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (itemBase != null)
            {
                if (itemBase.itemType == ItemType.Equipment)
                {
                    Debug.Log("아이템 장착");
                }
                else if (itemBase.itemType == ItemType.Usable)
                {
                    Debug.Log(itemBase.itemName + "을(를) 사용하였습니다.");
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
        if (tempItem != null) // 마우스 놓았을 때 그 자리에 아이템이 있다면
        {
            TempSlot.Instance.GetTempSlot.AddItem(tempItem, tempItemCount);
        }
        else
        {
            TempSlot.Instance.GetTempSlot.ClearSlot();
        }
    }

    // 아이템의 개수 설정
    public void SetItemCount(int count = -1)
    {
        itemCount += count;
        tmpItemCount.text = itemCount.ToString();

        // 아이템을 모두 소모하였다면
        if (itemCount <= 0) ClearSlot();
    }

    // 슬롯 비우기
    private void ClearSlot()
    {
        itemBase = null;
        itemCount = 0;
        itemImage.sprite = null;
        SetAlpha(0);

        tmpItemCount.text = string.Empty;
        itemCountImage.SetActive(false);

    }

    // 빈 슬롯과 채워진 슬롯의 알파 변경
    private void SetAlpha(float alpha)
    {
        Color color = itemImage.color;
        color.a = alpha;
        itemImage.color = color;
    }
}
