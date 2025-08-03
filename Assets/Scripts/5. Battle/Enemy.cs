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


    private BattleManager battleManager;

    private BaseTree baseTreeEvaluator = new BaseTree();
    private AddTree addTreeEvaluator = new AddTree();
    private BaseTreeScore baseTreeScores = new BaseTreeScore();
    private AddTreeScore addTreeScores = new AddTreeScore();

    // --- ���� ������ ���� �� ���� �߰� ---
    private bool isBattleConfirmed = false;
    private int finalPlayerScore = 0;
    private int finalEnemyScore = 0;
    private int finalPlayerBaseScore = 0; // �÷��̾��� �⺻ ���� ����
    private int enemyBaseScore = 0;       // ���� �⺻ ���� ����

    void Start()
    {
        // Start �Լ����� ��ư �����ʸ� �����մϴ�.
        if (button_ConfirmBattle != null)
        {
            button_ConfirmBattle.onClick.AddListener(ConfirmBattle);
        }
    }
    

    public void ConfirmBattle()
    {
        if (isBattleConfirmed) return;

        finalPlayerScore = int.Parse(TMP_PlayerScore.text);
        finalEnemyScore = int.Parse(TMP_EnemyScore.text);

        bool playerWins = false;

        if (finalPlayerScore > finalEnemyScore)
        {
            playerWins = true;
        }
        else if (finalPlayerScore == finalEnemyScore)
        {
            Debug.Log("���� ����! �⺻ ���� ��...");
            if (finalPlayerBaseScore > enemyBaseScore)
            {
                playerWins = true;
            }
            else if (finalPlayerBaseScore == enemyBaseScore)
            {
                // �⺻ �������� ������ ���, ���ο� �� �� �Լ��� ȣ���մϴ�.
                Debug.Log("�⺻ ���� ����! �ֻ��� �� ��...");
                playerWins = DecideTieBreaker();
            }
        }

        // ���� ��� ó��
        if (playerWins)
        {
            int scoreDifference = finalPlayerScore - finalEnemyScore;
            Debug.Log($"�¸�! ���� ����: {scoreDifference}, �⺻ ����: {finalPlayerBaseScore}");
            battleManager.RecordVictory(scoreDifference);
        }
        else
        {
            Debug.LogWarning($"�й�... (�÷��̾�: {finalPlayerScore} vs ��: {finalEnemyScore})");
            battleManager.RecordVictory(0); // �й� �ÿ��� 0�� ����
        }

        isBattleConfirmed = true;
        LockInteraction();
        battleManager.OnEnemyDefeated();
    }

    /// <summary>
    /// ������ �⺻ �������� ��� ������ ��, �ֻ��� ������ ���� ���Ͽ� ���и� �����մϴ�.
    /// �÷��̾ �̱�� true, ���ų� ���� false�� ��ȯ�մϴ�.
    /// </summary>
    private bool DecideTieBreaker()
    {
        // 1. ������ �ֻ��� ������ '����:����' ������ ��ųʸ�(�� ��)�� ��ȯ�մϴ�.
        var playerFreq = playerDices.GroupBy(d => d.DiceNumber)
                                    .ToDictionary(g => g.Key, g => g.Count());
        var enemyFreq = enemyDices.GroupBy(d => d.DiceNumber)
                                  .ToDictionary(g => g.Key, g => g.Count());

        // 2. �ֻ��� ���ڰ� ���� ������� ���ϱ� ���� �� ��ųʸ��� �����մϴ�.
        var playerSorted = playerFreq.OrderByDescending(p => p.Key).ToList();
        var enemySorted = enemyFreq.OrderByDescending(p => p.Key).ToList();

        // 3. �� ���� ������ �ֻ����� ���� ���� �������� �ݺ� Ƚ���� ���մϴ�.
        int loopCount = Mathf.Min(playerSorted.Count, enemySorted.Count);

        for (int i = 0; i < loopCount; i++)
        {
            var playerPair = playerSorted[i]; // �÷��̾��� i��°�� ���� �ֻ����� ����
            var enemyPair = enemySorted[i];   // ���� i��°�� ���� �ֻ����� ����

            // �� 1: �ֻ��� ����(��)�� ���մϴ�.
            if (playerPair.Key > enemyPair.Key) return true; // �÷��̾� ��
            if (playerPair.Key < enemyPair.Key) return false; // �÷��̾� ��

            // �ֻ��� ���ڰ� ���ٸ�, �� 2: �ش� �ֻ����� ������ ���մϴ�.
            if (playerPair.Value > enemyPair.Value) return true; // �÷��̾� ��
            if (playerPair.Value < enemyPair.Value) return false; // �÷��̾� ��
        }

        // 4. ������ �����ٴ� ����, ������ ���� �ֻ��� ������ ��� ���ߴٴ� �ǹ��Դϴ�.
        // ���� �÷��̾ �� �پ��� ������ �ֻ����� ������ �ִٸ�, �� ���� ���� �ֻ����� �����Ƿ� �¸��Դϴ�.
        if (playerSorted.Count > enemySorted.Count) return true;
        if (playerSorted.Count < enemySorted.Count) return false;

        // 5. ��� �ֻ����� ����, ����, ������ �Ϻ��ϰ� �Ȱ��ٸ�, ��Ģ�� ���� ���� �¸��մϴ�.
        return false;
    }

    /// <summary>
    /// �� ������ ��� UI ��ȣ�ۿ��� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    private void LockInteraction()
    {
        // �÷��̾ �� �ֻ����� �� �̻� Ŭ��(ȸ��)�� �� ���� ����ϴ�.
        foreach (Transform child in playerParent)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        // ���� Ȯ�� ��ư�� ��Ȱ��ȭ�մϴ�.
        button_ConfirmBattle.interactable = false;

        // �ð��� �ǵ��: �г��� �ణ ��Ӱ� ����� �������� ǥ��
        GetComponent<Image>().color = Color.gray;
    }


    public void Setup(YeokData data, BattleManager manager)
    {
        this.battleManager = manager;
        this.enemyBaseScore = data.baseScore; // ���� �⺻ ���� ����

        // --- ���� ���� ǥ�� ���� (���� ����) ---
        // 1. �⺻ ��, ���� ���� ǥ��
        TMP_EnemyBaseTree.text = data.foundationYeok.ToString();
        TMP_EnemyScore.text = data.totalScore.ToString();

        // 2. �߰� �� ���� ���ڿ� ���� �� ǥ��
        int[] comboArray = data.combination.ToArray();
        List<string> addTreeNames = new List<string>();

        if (comboArray.Length >= 4)
        {
            if (addTreeEvaluator.AllNumber(comboArray)) addTreeNames.Add("�óѹ�");
            if (addTreeEvaluator.AllSymbol(comboArray)) addTreeNames.Add("�ýɺ�");
            if (addTreeEvaluator.Pure(comboArray)) addTreeNames.Add("����");
            if (addTreeEvaluator.Mix(comboArray)) addTreeNames.Add("ȥ��");
            if (addTreeEvaluator.IncludeStar(comboArray)) addTreeNames.Add("��");
            if (addTreeEvaluator.IncludeMoon(comboArray)) addTreeNames.Add("��");
            if (addTreeEvaluator.IncludeSun(comboArray)) addTreeNames.Add("��");
            if (addTreeEvaluator.FullDice(comboArray)) addTreeNames.Add("Ǯ���̽�");
        }

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

                // 1. ������ �ֻ������� Button ������Ʈ�� �����ɴϴ�.
                Button diceButton = diceObject.GetComponent<Button>();

                // 2. Button ������Ʈ�� �����ϸ� interactable �Ӽ��� false�� �����մϴ�.
                if (diceButton != null)
                {
                    diceButton.interactable = false;
                }

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


    public void ReceivePlayerDice(Dice diceData)
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Enemy ��ũ��Ʈ�� Dice Prefab�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }

        // 1. �÷��̾��� �ֻ����� �ڽ��� playerParent �Ʒ��� ����
        GameObject diceObject = Instantiate(dicePrefab, playerParent);
        Dice newDice = diceObject.GetComponent<Dice>();

        // 2. ���޹��� �����ͷ� ���� ����
        newDice.DiceNumber = diceData.DiceNumber;
        newDice.DiceSprite = battleManager.DiceSprites[newDice.DiceNumber];
        newDice.DiceSpriteInstance();

        // 3. ���� ���� �ֻ����� Ŭ���ؼ� �ٽ� ������ �ǵ����� ��� �߰��� ���� ������ ����
        Button diceButton = diceObject.GetComponent<Button>();
        if (diceButton != null)
        {
            diceButton.onClick.AddListener(() => ReturnDiceToHand(newDice));
        }

        // 4. ������ �ֻ����� ���� ����Ʈ�� �߰��Ͽ� ����
        playerDices.Add(newDice);

        // --- ���⿡ ���� �� UI ������Ʈ �Լ� ȣ�� �߰� ---
        UpdatePlayerYeokDisplay();
    }


    /// <summary>
    /// �÷��̾ �� �ֻ��� ����� �������� ������ ������ ����ϰ� UI�� ������Ʈ�մϴ�.
    /// </summary>
    private void UpdatePlayerYeokDisplay()
    {
        // 1. ������ ���� List<Dice>�� int[] �迭�� ��ȯ�մϴ�.
        int[] comboArray = playerDices.Select(d => d.DiceNumber).ToArray();

        // ���� �� �ֻ����� ���ٸ� UI�� �ʱ�ȭ�մϴ�.
        if (comboArray.Length == 0)
        {
            TMP_PlayerBaseTree.text = "-";
            TMP_PlayerAddTree.text = "-";
            TMP_PlayerScore.text = "0";
            return;
        }

        // 2. �⺻ ���� ���� (���� ���� �켱������ ���� �ϳ��� ã��)
        BaseTreeEnum foundBaseYeok = default;
        int baseScore = 0;
        bool yeokFound = false;

        // Enum ������ ������ ������ �켱������ ���� �ͺ��� ����
        var yeokOrder = System.Enum.GetValues(typeof(BaseTreeEnum)).Cast<BaseTreeEnum>().Reverse().ToArray();

        foreach (var yeok in yeokOrder)
        {
            if (IsYeokMatch(yeok, comboArray))
            {
                foundBaseYeok = yeok;
                baseScore = GetBaseScore(yeok);
                yeokFound = true;
                break; // ���� ���� ������ ã�����Ƿ� �ߴ�
            }
        }

        this.finalPlayerBaseScore = baseScore; // ���� �⺻ ������ ��� ������ ����

        // 3. �߰� ���� ���� (�ش��ϴ� ��� �߰� ���� ������ �ջ�)
        int bonusScore = 0;
        List<string> addTreeNames = new List<string>(); // �׻� ����ִ� ����Ʈ�� �ʱ�ȭ

        if (comboArray.Length >= 4)
        {
            // �� ��� ������ if�� ������ �̵��մϴ�.
            bonusScore = CalculateBonusScore(comboArray);
            addTreeNames = GetBonusYeokNames(comboArray);
        }

        // 4. ���� ���� ��� �� UI �ؽ�Ʈ ������Ʈ
        int totalScore = baseScore + bonusScore;

        TMP_PlayerBaseTree.text = yeokFound ? $"{foundBaseYeok} ({baseScore}��)" : "���� ����";
        TMP_PlayerAddTree.text = addTreeNames.Any() ? $"{string.Join(", ", addTreeNames)} ({bonusScore}��)" : "�߰� ���� ����";
        TMP_PlayerScore.text = totalScore.ToString();
    }

    /// <summary>
    /// �÷��̾ �� �ֻ����� Ŭ������ ��, ���з� �ǵ����ϴ�.
    /// </summary>
    public void ReturnDiceToHand(Dice diceToReturn)
    {
        if (diceToReturn == null) return;

        // 1. BattleManager���� �� �ֻ����� �ǵ����޶�� ��û
        battleManager.ReclaimDiceFromPlayerPanel(diceToReturn);

        // 2. �� Enemy �ν��Ͻ��� �����ϴ� �ֻ��� ��Ͽ��� ����
        playerDices.Remove(diceToReturn);

        // 3. �ֻ����� �ϳ� �پ����Ƿ�, ���� �ֻ����� ������ ������ �ٽ� ���
        UpdatePlayerYeokDisplay();
    }


    // --- �Ʒ��� YeokDatabaseGenerator���� ������ ���� ���� �Լ����Դϴ� ---

    private List<string> GetBonusYeokNames(int[] combo)
    {
        var names = new List<string>();
        if (addTreeEvaluator.AllNumber(combo)) names.Add("�óѹ�");
        if (addTreeEvaluator.AllSymbol(combo)) names.Add("�ýɺ�");
        if (addTreeEvaluator.Pure(combo)) names.Add("����");
        if (addTreeEvaluator.Mix(combo)) names.Add("ȥ��");
        if (addTreeEvaluator.IncludeStar(combo)) names.Add("��");
        if (addTreeEvaluator.IncludeMoon(combo)) names.Add("��");
        if (addTreeEvaluator.IncludeSun(combo)) names.Add("��");
        if (addTreeEvaluator.FullDice(combo)) names.Add("Ǯ���̽�");
        return names;
    }

    private int CalculateBonusScore(int[] combo)
    {
        int score = 0;
        if (addTreeEvaluator.AllNumber(combo)) score += addTreeScores.allNumber;
        if (addTreeEvaluator.AllSymbol(combo)) score += addTreeScores.allSymbol;
        if (addTreeEvaluator.Pure(combo)) score += addTreeScores.pure;
        if (addTreeEvaluator.Mix(combo)) score += addTreeScores.mix;
        if (addTreeEvaluator.IncludeStar(combo)) score += addTreeScores.includeStar;
        if (addTreeEvaluator.IncludeMoon(combo)) score += addTreeScores.includeMoon;
        if (addTreeEvaluator.IncludeSun(combo)) score += addTreeScores.includeSun;
        if (addTreeEvaluator.FullDice(combo)) score += addTreeScores.fullDice;
        return score;
    }

    private bool IsYeokMatch(BaseTreeEnum yeok, int[] combo)
    {
        switch (yeok)
        {
            case BaseTreeEnum.pair: return baseTreeEvaluator.Pair(combo);
            case BaseTreeEnum.straight: return baseTreeEvaluator.Straight(combo);
            case BaseTreeEnum.twoPair: return baseTreeEvaluator.TwoPair(combo);
            case BaseTreeEnum.triple: return baseTreeEvaluator.Triple(combo);
            case BaseTreeEnum.threePair: return baseTreeEvaluator.ThreePair(combo);
            case BaseTreeEnum.fullHouse: return baseTreeEvaluator.FullHouse(combo);
            case BaseTreeEnum.fourCard: return baseTreeEvaluator.FourCard(combo);
            case BaseTreeEnum.doubleTriple: return baseTreeEvaluator.DoubleTriple(combo);
            case BaseTreeEnum.grandFullHouse: return baseTreeEvaluator.GrandFullHouse(combo);
            case BaseTreeEnum.fiveCard: return baseTreeEvaluator.FiveCard(combo);
            case BaseTreeEnum.doubleStraight: return baseTreeEvaluator.DoubleStraight(combo);
            case BaseTreeEnum.genesis: return baseTreeEvaluator.Genesis(combo);
            case BaseTreeEnum.universe: return baseTreeEvaluator.Universe(combo);
            case BaseTreeEnum.hexa: return baseTreeEvaluator.Hexa(combo);
            default: return false;
        }
    }

    private int GetBaseScore(BaseTreeEnum yeok)
    {
        switch (yeok)
        {
            case BaseTreeEnum.pair: return baseTreeScores.pair;
            case BaseTreeEnum.straight: return baseTreeScores.straight;
            case BaseTreeEnum.twoPair: return baseTreeScores.twoPair;
            case BaseTreeEnum.triple: return baseTreeScores.triple;
            case BaseTreeEnum.threePair: return baseTreeScores.threePair;
            case BaseTreeEnum.fullHouse: return baseTreeScores.fullHouse;
            case BaseTreeEnum.fourCard: return baseTreeScores.fourCard;
            case BaseTreeEnum.doubleTriple: return baseTreeScores.doubleTriple;
            case BaseTreeEnum.grandFullHouse: return baseTreeScores.grandFullHouse;
            case BaseTreeEnum.fiveCard: return baseTreeScores.fiveCard;
            case BaseTreeEnum.doubleStraight: return baseTreeScores.doubleStraight;
            case BaseTreeEnum.genesis: return baseTreeScores.genesis;
            case BaseTreeEnum.universe: return baseTreeScores.universe;
            case BaseTreeEnum.hexa: return baseTreeScores.hexa;
            default: return 0;
        }
    }
}