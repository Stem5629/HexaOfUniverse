using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TempSlot : MonoBehaviour
{
    private static TempSlot instance = null;
    public static TempSlot Instance => instance;

    public Slot GetTempSlot { get; set; }

    [SerializeField] private Image diceImage;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void SetDiceImage(Image image)
    {
        diceImage.sprite = image.sprite;
        SetAlpha(1);
    }
    public void SetAlpha(float alpha)
    {
        Color color = diceImage.color;
        color.a = alpha;
        diceImage.color = color;
    }
}
