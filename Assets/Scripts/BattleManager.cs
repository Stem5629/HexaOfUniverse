using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform mainPlayerDiceParent;
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform selectButtonParent;
    [SerializeField] private GameObject selectButtonPrefab;

    private void Start()
    {
        SetPlayerDice(DiceManager.Instance.Dices);

        CombinationGenerator generator = new CombinationGenerator();

        // ��� ������ ���� �ϳ��� �Ŵ��� '������ ����Ʈ'�� �����մϴ�.
        List<List<int>> totalCombinations = new List<List<int>>();

        // �ֻ��� 2������ 6������ �ݺ��մϴ�.
        for (int k = 2; k <= 6; k++)
        {
            // k�� ������ ����Ʈ�� �����մϴ�.
            List<List<int>> combinationsForK = generator.Generate(6, k);

            // ������ ����Ʈ�� ������ ����Ʈ�� ��°�� �߰��մϴ�.
            totalCombinations.AddRange(combinationsForK);
        }

    }

    // ���� ���̵��� ���� Enemy ����
    private void InstantiateEnemies(Enemy enemy, int difficulty)
    {
        
    }

    private void SetPlayerDice(List<Dice> dices)
    {
        foreach(Dice item in dices)
        {
            CreateDice(item);
        }
    }

    private void CreateDice(Dice dice)
    {
        GameObject diceObject = Instantiate(dicePrefab, mainPlayerDiceParent.transform);
        dice = diceObject.GetComponent<Dice>();
    }

    private void InstantiateSelectButtons(int difficulty)
    {

    }
}
