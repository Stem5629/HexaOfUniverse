using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dice : MonoBehaviour
{
    [Header("characteristic")]
    [SerializeField] private int diceNumber;
    [SerializeField] private Sprite diceSprite;

    public int DiceNumber { get => diceNumber; set { diceNumber = value; } }
    public Sprite DiceSprite { get => diceSprite; set { diceSprite = value; } }

    public void DiceSpriteInstance()
    {
        Image diceImage = gameObject.GetComponent<Image>();
        diceImage.sprite = diceSprite;
    }
}
