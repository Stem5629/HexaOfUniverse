using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [SerializeField] private GameObject enemyParent;
    [SerializeField] private TextMeshProUGUI TMP_EnemyBaseTree;
    [SerializeField] private TextMeshProUGUI TMP_EnemyAddTree;
    [SerializeField] private TextMeshProUGUI TMP_EnemyScore;

    [SerializeField] private GameObject playerParent;
    [SerializeField] private TextMeshProUGUI TMP_PlayerBaseTree;
    [SerializeField] private TextMeshProUGUI TMP_PlayerAddTree;
    [SerializeField] private TextMeshProUGUI TMP_PlayerScore;

    [SerializeField] private Button button_ConfirmBattle;

    private List<Dice> enemyDices = new List<Dice>();
    private List<Dice> playerDices = new List<Dice>();
}
