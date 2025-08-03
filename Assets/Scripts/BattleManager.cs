using System.Collections.Generic;
using System.Linq; // LINQ�� ����ϱ� ���� �߰��ؾ� �մϴ�.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EnemyLevelEnum
{
    Level0 = 0,
    Level1 = 13,
    Level2 = 26,
    Level3 = 31,
    Level4 = 36,
    Max = 45
}

public class BattleManager : MonoBehaviour
{
    // --- ���� ������ ---
    [SerializeField] private Image characterImage;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform mainPlayerDiceParent;
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform selectButtonParent;
    [SerializeField] private GameObject selectButtonPrefab;

    [SerializeField] private Sprite[] diceSprites = new Sprite[6];

    // --- ���� �߰��� ���� ---
    [Header("�����ͺ��̽� ����")]
    [SerializeField] private YeokDatabase yeokDatabase; // �ν����Ϳ��� YeokDatabase.asset ������ �������ּ���.
    public Sprite[] DiceSprites => diceSprites;

    private List<Dice> selectedPlayerDices = new List<Dice>();

    private void Start()
    {
        // �׽�Ʈ�� ���� ���̵� 0�� ���� ��ȯ�մϴ�.
        // ���� ���ӿ����� ������ ������ ȣ���ؾ� �մϴ�.

        SetPlayerDice();

        InstantiateEnemyByDifficulty(1);
    }

    /// <summary>
    /// ���̵��� �´� ���� ������ ���� �����ͺ��̽����� ã�� �������� ��ȯ�մϴ�.
    /// </summary>
    /// <param name="difficulty">���� ���̵� (0~4)</param>
    public void InstantiateEnemyByDifficulty(int difficulty)
    {
        // 1. ���̵��� ���� ���� ���� ����
        int minScore;
        int maxScore;

        switch (difficulty)
        {
            case 0:
                minScore = (int)EnemyLevelEnum.Level0;
                maxScore = (int)EnemyLevelEnum.Level1;
                break;
            case 1:
                minScore = (int)EnemyLevelEnum.Level1;
                maxScore = (int)EnemyLevelEnum.Level2;
                break;
            case 2:
                minScore = (int)EnemyLevelEnum.Level2;
                maxScore = (int)EnemyLevelEnum.Level3;
                break;
            case 3:
                minScore = (int)EnemyLevelEnum.Level3;
                maxScore = (int)EnemyLevelEnum.Level4;
                break;
            case 4:
                minScore = (int)EnemyLevelEnum.Level4;
                maxScore = (int)EnemyLevelEnum.Max;
                break;
            default:
                Debug.LogError($"�߸��� ���̵� ���Դϴ�: {difficulty}");
                return;
        }

        // 2. �����ͺ��̽����� ���ǿ� �´� ��� ��(Yeok) ���͸�
        List<YeokData> possibleEnemies = yeokDatabase.allYeokData
            .Where(data => data.totalScore >= minScore && data.totalScore < maxScore)
            .ToList();

        // 3. ���͸��� ����Ʈ���� �������� �ϳ� ����
        if (possibleEnemies.Count > 0)
        {
            int randomIndex = Random.Range(0, possibleEnemies.Count);
            YeokData selectedYeok = possibleEnemies[randomIndex];

            // 4. ���õ� �� ������ �������� �� ������Ʈ ���� �� ����
            GameObject enemyObject = Instantiate(enemyPrefab, enemyParent);
            Enemy newEnemy = enemyObject.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                newEnemy.Setup(selectedYeok, this); // Enemy ��ũ��Ʈ�� ������ ����
            }
            GameObject buttonObject = Instantiate(selectButtonPrefab, selectButtonParent);

            // ��ư ��ȣ ����
            TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                // ���� ������ ���� ����ŭ ��ȣ�� �ű�
                buttonText.text = enemyParent.childCount.ToString();
            }

            // ��ư Ŭ�� �̺�Ʈ�� CommitDiceToEnemy �޼��� ����
            Button selectButton = buttonObject.GetComponent<Button>();
            if (selectButton != null)
            {
                // ��ư�� ������ 'newEnemy'�� Ÿ������ �ֻ����� �����ϵ��� ����
                selectButton.onClick.AddListener(() => CommitDiceToEnemy(newEnemy));
            }
        }
        else
        {
            Debug.LogWarning($"���̵� {difficulty} (���� {minScore}~{maxScore - 1})�� �ش��ϴ� ���� �����ͺ��̽����� ã�� �� �����ϴ�.");
        }
    }

    // --- �Ʒ��� ������ �ִ� �Լ����Դϴ� ---
    private void SetPlayerDice()
    {
        // 1. YeokDatabase.dices�� ����� �ֻ��� '������' ����Ʈ�� �����ɴϴ�.
        List<Dice> savedDiceData = YeokDatabase.dices;

        // 2. ����� �� �ֻ��� '������'�� ������� ���� ���� �ֻ��� '������Ʈ'�� �����մϴ�.
        foreach (Dice data in savedDiceData)
        {
            // a. ���ο� �ֻ��� ���� ������Ʈ�� �����մϴ�.
            GameObject diceObject = Instantiate(dicePrefab, mainPlayerDiceParent);
            Dice newDice = diceObject.GetComponent<Dice>();

            // b. [�ٽ�] ����� ������(data)�� ���� ���ο� �ֻ���(newDice)�� �����մϴ�.
            newDice.DiceNumber = data.DiceNumber;
            newDice.DiceSprite = diceSprites[newDice.DiceNumber];

            // c. ������ ���� �������� �̹����� ������Ʈ�մϴ�.
            newDice.DiceSpriteInstance();


            Button diceButton = newDice.GetComponent<Button>();
            if (diceButton != null)
            {
                // �ֻ����� Ŭ���ϸ� ToggleDiceSelection �޼��尡 ȣ��ǵ��� ����
                diceButton.onClick.AddListener(() => ToggleDiceSelection(newDice));
            }
        }
    }

    private void RollDice(Dice dice)
    {
        int r = Random.Range(0, diceSprites.Length);

        dice.DiceNumber = r;
        dice.DiceSprite = diceSprites[r];

        dice.DiceSpriteInstance();
    }
    private Dice LoadDice(Transform parent)
    {
        GameObject diceObject = Instantiate(dicePrefab, parent);
        Dice dice = diceObject.GetComponent<Dice>();

        return dice;
    }

    // ���ӿ�����Ʈ ����� + ����Ʈ�� �߰�
    // Dice ��ȯ
    private Dice CreateDice(Transform parent)
    {
        GameObject diceObject = Instantiate(dicePrefab, parent);
        Dice dice = diceObject.GetComponent<Dice>();
        YeokDatabase.dices.Add(dice);

        return dice;
    }


    // ���ӿ�����Ʈ + ����Ʈ���� ����
    // => ���̽� Ŭ���ϸ� dice ��ȯ�ϴ� �޼��� �����
    // List<Dice> ��ȯ
    private List<Dice> RemoveDice(List<Dice> dices, Dice dice)
    {
        for (int i = 0; i < dices.Count; i++)
        {
            if (dices[i] == dice)
            {
                Destroy(dice.gameObject);
                dices.RemoveAt(i);
                return dices;
            }
        }
        return dices;
    }
    private void ArrangeDicePool()
    {
        // 1. �ӽ� ����Ʈ�� ���� ������� �ֻ����� ��� �����մϴ�.
        List<Dice> tempDices = new List<Dice>(); // �ӽ� ����Ʈ�� ���⼭ �����մϴ�.
        int diceIndexCount = 6;

        for (int i = 0; i < diceIndexCount; i++)
        {
            for (int j = 0; j < YeokDatabase.dices.Count; j++)
            {
                if (YeokDatabase.dices[j].DiceNumber == i)
                {
                    tempDices.Add(YeokDatabase.dices[j]);
                }
            }
        }

        // 2. ���� dices ����Ʈ�� ���ĵ� �ӽ� ����Ʈ�� '���纻'���� ��ü�մϴ�.
        // (���� ����: �׳� �����ϸ� ���� ����Ʈ�� �����ϰ� �Ǿ� �����Ͱ� ����� �� �ֽ��ϴ�)
        YeokDatabase.dices = new List<Dice>(tempDices);

        // 3. ���ĵ� dices ����Ʈ ������� ���� ���� ������Ʈ�� ������ ���ġ�մϴ�.
        // SetAsLastSibling()�� ������� ȣ���ϸ� Hierarchy�󿡼� ������Ʈ�� ���ʷ� �� �ڷ� �̵��ϸ� ���ĵ˴ϴ�.
        for (int i = 0; i < YeokDatabase.dices.Count; i++)
        {
            YeokDatabase.dices[i].transform.SetAsLastSibling();
        }
    }

    // BattleManager.cs (Ŭ���� ���ο� �߰�)

    // �÷��̾ �ڽ��� �ֻ����� Ŭ������ �� ȣ��� �޼���
    public void ToggleDiceSelection(Dice dice)
    {
        if (selectedPlayerDices.Contains(dice))
        {
            // �̹� ���õ� �ֻ����� ����Ʈ���� ���� (���� ����)
            selectedPlayerDices.Remove(dice);
            dice.transform.localScale = Vector3.one; // (�ð��� �ǵ��) ũ�⸦ �������
        }
        else
        {
            // ���õ��� ���� �ֻ����� ����Ʈ�� �߰� (����)
            selectedPlayerDices.Add(dice);
            dice.transform.localScale = Vector3.one * 1.2f; // (�ð��� �ǵ��) ũ�⸦ ��¦ Ű��
        }
    }

    // �� ���� ��ư�� ������ ��, ���õ� �ֻ������� �ش� ������ �����ϴ� �޼���
    public void CommitDiceToEnemy(Enemy targetEnemy)
    {
        if (selectedPlayerDices.Count == 0)
        {
            Debug.Log("������ �ֻ����� ���� �����ϼ���.");
            return;
        }

        // ���õ� �ֻ������� �ϳ��� ó��
        foreach (Dice diceToCommit in selectedPlayerDices)
        {
            // Enemy.cs�� �ִ� ReceivePlayerDice �Լ��� ȣ�� (2�ܰ迡�� ����)
            targetEnemy.ReceivePlayerDice(diceToCommit);

            // �÷��̾� �տ� �ִ� ���� �ֻ��� ������Ʈ�� �ı�
            Destroy(diceToCommit.gameObject);
        }

        // ������ �������Ƿ� ���� ����Ʈ�� ���
        selectedPlayerDices.Clear();
    }
}