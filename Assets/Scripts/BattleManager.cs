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

        // 모든 조합을 담을 하나의 거대한 '마스터 리스트'를 생성합니다.
        List<List<int>> totalCombinations = new List<List<int>>();

        // 주사위 2개부터 6개까지 반복합니다.
        for (int k = 2; k <= 6; k++)
        {
            // k개 조합의 리스트를 생성합니다.
            List<List<int>> combinationsForK = generator.Generate(6, k);

            // 생성된 리스트를 마스터 리스트에 통째로 추가합니다.
            totalCombinations.AddRange(combinationsForK);
        }

    }

    // 방의 난이도에 따른 Enemy 생성
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
