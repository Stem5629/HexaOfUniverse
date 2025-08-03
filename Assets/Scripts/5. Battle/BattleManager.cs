using System.Collections.Generic;
using System.Linq; // LINQ�� ����ϱ� ���� �߰��ؾ� �մϴ�.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EnemyLevelEnum
{
    Level0 = 0,
    Level1 = 5,
    Level2 = 10,
    Level3 = 15,
    Level4 = 25,
    Level5 = 30,
    Level6 = 36,
    Level7 = 44,
    Max = 45
}

public class BattleManager : MonoBehaviour
{
    [Header("�����ͺ��̽� ����")]
    [SerializeField] private FloorDatabase floorDatabase; // �ν����Ϳ��� FloorDatabase ������ ����

    private int currentFloor = 1;

    // --- ���� ������ ---
    [SerializeField] private Image characterImage;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform mainPlayerDiceParent;
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform selectButtonParent;
    [SerializeField] private GameObject selectButtonPrefab;

    [SerializeField] private Sprite[] diceSprites = new Sprite[6];
    [SerializeField] private Button arrangeButton; // ���� ��ư ���� �߰�

    // --- ���� �߰��� ���� ---
    [Header("�����ͺ��̽� ����")]
    [SerializeField] private YeokDatabase yeokDatabase; // �ν����Ϳ��� YeokDatabase.asset ������ �������ּ���.

    [Header("UI ��ư")]
    [SerializeField] private Button nextSceneButton; // ���� ������ ���� ��ư

    // --- ���� ������ ���� �� ���� �߰� ---
    private List<Enemy> spawnedEnemies = new List<Enemy>();
    private int defeatedEnemyCount = 0;
    private int totalRewardScore = 0;

    public Sprite[] DiceSprites => diceSprites;

    private List<Dice> selectedPlayerDices = new List<Dice>();

    private void Start()
    {
        // �׽�Ʈ�� ���� ���̵� 0�� ���� ��ȯ�մϴ�.
        // ���� ���ӿ����� ������ ������ ȣ���ؾ� �մϴ�.

        SetPlayerDice();

        if (arrangeButton != null)
        {
            arrangeButton.onClick.AddListener(ArrangePlayerHand);
        }
        if (nextSceneButton != null)
        {
            nextSceneButton.gameObject.SetActive(false);
        }

        SpawnEnemiesForCurrentFloor(currentFloor);
    }

    /// <summary>
    /// Enemy�κ��� �ֻ����� �ǵ����޾� �÷��̾��� ���з� �ű�ϴ�.
    /// </summary>
    public void ReclaimDiceFromPlayerPanel(Dice diceToReclaim)
    {
        if (diceToReclaim == null) return;

        // 1. �ֻ����� �θ� �ٽ� �÷��̾��� ���� ���� ��ġ�� ����
        diceToReclaim.transform.SetParent(mainPlayerDiceParent, false);

        // 2. �ٽ� �����ؼ� �� �� �ֵ���, Ŭ�� �̺�Ʈ�� '����/����' ������� �ǵ���
        Button diceButton = diceToReclaim.GetComponent<Button>();
        if (diceButton != null)
        {
            diceButton.onClick.RemoveAllListeners(); // ������ '�з� �ǵ�����' �����ʴ� ��� ����
            diceButton.onClick.AddListener(() => ToggleDiceSelection(diceToReclaim));
        }
    }

    /// <summary>
    /// ���� ��(currentFloor)�� �´� ���͵��� �����ͺ��̽����� ã�� ��ȯ�մϴ�.
    /// </summary>
    private void SpawnEnemiesForCurrentFloor(int currentFloor)
    {
        // 1. �����ͺ��̽����� ���� �� ��ȣ�� �´� �� �����͸� ã���ϴ�.
        FloorData data = floorDatabase.allFloors.FirstOrDefault(f => f.floorNumber == currentFloor);

        // 2. �ش� �� �����Ͱ� �����ϴ��� Ȯ���մϴ�.
        if (data == null)
        {
            Debug.LogError($"{currentFloor}���� ���� �����͸� FloorDatabase���� ã�� �� �����ϴ�!");
            return;
        }

        Debug.Log($"--- {data.floorNumber}�� ���� ��ȯ ���� ---");

        // 3. �� �������� ���� ���(Roster)�� ��ȸ�մϴ�.
        foreach (MonsterSpawnInfo info in data.monsterRoster)
        {
            // 4. �� ����(info)�� ��õ� ������(count)��ŭ ���� ������ �ݺ��մϴ�.
            for (int i = 0; i < info.count; i++)
            {
                // ������ ������ ���� ���� �Լ��� ��Ȱ���մϴ�.
                InstantiateEnemyByDifficulty(info.difficulty);
            }
        }
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
                maxScore = (int)EnemyLevelEnum.Level5;
                break;
            case 5:
                minScore = (int)EnemyLevelEnum.Level5;
                maxScore = (int)EnemyLevelEnum.Level6;
                break;
            case 6:
                minScore = (int)EnemyLevelEnum.Level6;
                maxScore = (int)EnemyLevelEnum.Level7;
                break;
            case 7:
                minScore = (int)EnemyLevelEnum.Level7;
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
                spawnedEnemies.Add(newEnemy); // ������ ���� ����Ʈ�� �߰�
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

    /// <summary>
    /// Enemy�� ���� Ȯ�� �� ȣ���ϴ� �Լ�
    /// </summary>
    public void OnEnemyDefeated()
    {
        defeatedEnemyCount++;

        // 5. ��� ������ ������ �������� Ȯ���մϴ�.
        if (defeatedEnemyCount >= spawnedEnemies.Count)
        {
            Debug.Log("��� ������ ������ �������ϴ�! ���� ������ ������ �� �ֽ��ϴ�.");
            if (nextSceneButton != null)
            {
                // ���� ������ ���� ��ư�� Ȱ��ȭ�մϴ�.
                nextSceneButton.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// �¸� �� ���� ���̸� ����մϴ�.
    /// </summary>
    public void RecordVictory(int scoreDifference)
    {
        totalRewardScore += scoreDifference;
        Debug.Log($"������� ���� ���� ����: {totalRewardScore}");
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

    /// <summary>
    /// �÷��̾��� ���п� �ִ� �ֻ������� ��ȣ ������� �����մϴ�.
    /// </summary>
    public void ArrangePlayerHand()
    {
        // 1. ���� ���п� �ִ� ��� �ֻ��� ������Ʈ�� Dice ������Ʈ�� ����Ʈ�� ����ϴ�.
        List<Dice> dicesInHand = new List<Dice>();
        foreach (Transform diceTransform in mainPlayerDiceParent)
        {
            Dice dice = diceTransform.GetComponent<Dice>();
            if (dice != null)
            {
                dicesInHand.Add(dice);
            }
        }

        // 2. �ֻ��� ��ȣ(DiceNumber)�� �������� ����Ʈ�� �������� �����մϴ�.
        List<Dice> sortedDices = dicesInHand.OrderBy(d => d.DiceNumber).ToList();

        // 3. ���ĵ� ������� UI�󿡼� ���� ���� ������Ʈ�� ������ ���ġ�մϴ�.
        // SetAsLastSibling()�� ������Ʈ�� �θ��� �ڽ� ��� �� ���� ���������� ������ �Լ��Դϴ�.
        // ���ĵ� ����Ʈ ������� �� �Լ��� ȣ���ϸ�, ������Ʈ���� ������� �ڷ� ���� ���ĵ˴ϴ�.
        foreach (Dice dice in sortedDices)
        {
            dice.transform.SetAsLastSibling();
        }

        Debug.Log("�÷��̾��� �ֻ����� �����߽��ϴ�.");
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