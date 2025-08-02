using System.Collections.Generic;
using System.Linq; // LINQ�� ����ϱ� ���� �߰�
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{

    [Header("UI (Enemy)")]
    [SerializeField] private Transform enemyParent; // �θ� Ʈ���������� ����
    [SerializeField] private TextMeshProUGUI TMP_EnemyBaseTree;
    [SerializeField] private TextMeshProUGUI TMP_EnemyAddTree;
    [SerializeField] private TextMeshProUGUI TMP_EnemyScore;

    [Header("UI (Player)")]
    [SerializeField] private Transform playerParent;
    [SerializeField] private TextMeshProUGUI TMP_PlayerBaseTree;
    [SerializeField] private TextMeshProUGUI TMP_PlayerAddTree;
    [SerializeField] private TextMeshProUGUI TMP_PlayerScore;

    [Header("Buttons")]
    [SerializeField] private Button button_ConfirmBattle;

    // --- ���� �߰��� ���� ---
    [Header("Prefabs")]
    [SerializeField] private GameObject dicePrefab; // �ν����Ϳ��� Dice �������� �������ּ���.

    private List<Dice> enemyDices = new List<Dice>();
    private List<Dice> playerDices = new List<Dice>();

    private AddTree addTreeEvaluator = new AddTree();

    private BattleManager battleManager;

    public void Setup(YeokData data, BattleManager manager)
    {
        this.battleManager = manager;

        // --- ���� ���� ǥ�� ���� (���� ����) ---
        // 1. �⺻ ��, ���� ���� ǥ��
        TMP_EnemyBaseTree.text = data.foundationYeok.ToString();
        TMP_EnemyScore.text = data.totalScore.ToString();

        // 2. �߰� �� ���� ���ڿ� ���� �� ǥ��
        int[] comboArray = data.combination.ToArray();
        List<string> addTreeNames = new List<string>();

        if (addTreeEvaluator.AllNumber(comboArray)) addTreeNames.Add("�óѹ�");
        if (addTreeEvaluator.AllSymbol(comboArray)) addTreeNames.Add("�ýɺ�");
        if (addTreeEvaluator.Pure(comboArray)) addTreeNames.Add("����");
        if (addTreeEvaluator.Mix(comboArray)) addTreeNames.Add("ȥ��");
        if (addTreeEvaluator.IncludeStar(comboArray)) addTreeNames.Add("��");
        if (addTreeEvaluator.IncludeMoon(comboArray)) addTreeNames.Add("��");
        if (addTreeEvaluator.IncludeSun(comboArray)) addTreeNames.Add("��");
        if (addTreeEvaluator.FullDice(comboArray)) addTreeNames.Add("Ǯ���̽�");

        if (addTreeNames.Any())
        {
            TMP_EnemyAddTree.text = $" {string.Join(", ", addTreeNames)}";
        }
        else
        {
            TMP_EnemyAddTree.text = "�߰� ��: ����";
        }

        // --- ���⿡ ���ο� ��� �߰� ---
        // 3. ������ ������ �ֻ����� �ִٸ� ��� �ı�
        foreach (Transform child in enemyParent)
        {
            Destroy(child.gameObject);
        }
        enemyDices.Clear(); // ����Ʈ�� ����ݴϴ�.

        // 4. �����Ϳ� ���� ���� �ֻ��� ������ ������ ����
        if (dicePrefab != null)
        {
            foreach (int diceValue in data.combination)
            {
                // �ֻ��� ������Ʈ ����
                GameObject diceObject = Instantiate(dicePrefab, enemyParent);
                Dice newDice = diceObject.GetComponent<Dice>();

                // Dice.cs ��ũ��Ʈ�� ������ ����
                newDice.DiceNumber = diceValue; //
                // DiceManager�� ��������Ʈ �迭���� �ùٸ� ��������Ʈ�� ã�� �Ҵ�
                newDice.DiceSprite = battleManager.DiceSprites[diceValue];

                // �̹��� ������Ʈ
                newDice.DiceSpriteInstance(); //

                // ������ �ֻ����� ����Ʈ�� �߰� (������)
                enemyDices.Add(newDice);
            }
        }
        else
        {
            Debug.LogError("Enemy ��ũ��Ʈ�� Dice Prefab�� �Ҵ���� �ʾҽ��ϴ�!");
        }

        Debug.Log($"�� ���� �Ϸ�: {data.foundationYeok} / {string.Join(", ", data.combination)} / ���� {data.totalScore}");
    }
}