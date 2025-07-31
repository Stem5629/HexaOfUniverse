using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonNumber;
    public TextMeshProUGUI ButtonNumber { set { buttonNumber = value; } }
}
