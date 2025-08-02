using System.Collections.Generic;
using System.Linq; // LINQ를 사용하기 위해 추가해야 합니다.
using UnityEngine;
using UnityEngine.UI;

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
    // --- 기존 변수들 ---
    [SerializeField] private Image characterImage;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform mainPlayerDiceParent;
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform selectButtonParent;
    [SerializeField] private GameObject selectButtonPrefab;

    [SerializeField] private Sprite[] diceSprites = new Sprite[6];

    // --- 새로 추가할 변수 ---
    [Header("데이터베이스 참조")]
    [SerializeField] private YeokDatabase yeokDatabase; // 인스펙터에서 YeokDatabase.asset 파일을 연결해주세요.
    public Sprite[] DiceSprites => diceSprites;

    private void Start()
    {
        // 테스트를 위해 난이도 0의 적을 소환합니다.
        // 실제 게임에서는 적절한 시점에 호출해야 합니다.

        SetPlayerDice();

        InstantiateEnemyByDifficulty(1);
        InstantiateEnemyByDifficulty(2);
        InstantiateEnemyByDifficulty(3);
    }

    /// <summary>
    /// 난이도에 맞는 점수 범위의 적을 데이터베이스에서 찾아 무작위로 소환합니다.
    /// </summary>
    /// <param name="difficulty">적의 난이도 (0~4)</param>
    public void InstantiateEnemyByDifficulty(int difficulty)
    {
        // 1. 난이도에 따른 점수 범위 설정
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
                Debug.LogError($"잘못된 난이도 값입니다: {difficulty}");
                return;
        }

        // 2. 데이터베이스에서 조건에 맞는 모든 역(Yeok) 필터링
        List<YeokData> possibleEnemies = yeokDatabase.allYeokData
            .Where(data => data.totalScore >= minScore && data.totalScore < maxScore)
            .ToList();

        // 3. 필터링된 리스트에서 무작위로 하나 선택
        if (possibleEnemies.Count > 0)
        {
            int randomIndex = Random.Range(0, possibleEnemies.Count);
            YeokData selectedYeok = possibleEnemies[randomIndex];

            // 4. 선택된 역 정보를 바탕으로 적 오브젝트 생성 및 설정
            GameObject enemyObject = Instantiate(enemyPrefab, enemyParent);
            Enemy newEnemy = enemyObject.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                newEnemy.Setup(selectedYeok, this); // Enemy 스크립트에 데이터 전달
            }
        }
        else
        {
            Debug.LogWarning($"난이도 {difficulty} (점수 {minScore}~{maxScore - 1})에 해당하는 적을 데이터베이스에서 찾을 수 없습니다.");
        }
    }

    // --- 아래는 기존에 있던 함수들입니다 ---
    private void SetPlayerDice()
    {
        // 1. YeokDatabase.dices에 저장된 주사위 '데이터' 리스트를 가져옵니다.
        List<Dice> savedDiceData = YeokDatabase.dices;

        // 2. 저장된 각 주사위 '데이터'를 기반으로 씬에 실제 주사위 '오브젝트'를 생성합니다.
        foreach (Dice data in savedDiceData)
        {
            // a. 새로운 주사위 게임 오브젝트를 생성합니다.
            GameObject diceObject = Instantiate(dicePrefab, mainPlayerDiceParent);
            Dice newDice = diceObject.GetComponent<Dice>();

            // b. [핵심] 저장된 데이터(data)의 값을 새로운 주사위(newDice)에 복사합니다.
            newDice.DiceNumber = data.DiceNumber; // <-- 이 부분이 누락되었습니다!
            newDice.DiceSprite = diceSprites[newDice.DiceNumber];

            // c. 복사한 값을 바탕으로 이미지를 업데이트합니다.
            newDice.DiceSpriteInstance();
        }
    }

    private void InstantiateSelectButtons(int difficulty)
    {

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

    // 게임오브젝트 만들기 + 리스트에 추가
    // Dice 반환
    private Dice CreateDice(Transform parent)
    {
        GameObject diceObject = Instantiate(dicePrefab, parent);
        Dice dice = diceObject.GetComponent<Dice>();
        YeokDatabase.dices.Add(dice);

        return dice;
    }


    // 게임오브젝트 + 리스트에서 없엠
    // => 다이스 클릭하면 dice 반환하는 메서드 만들기
    // List<Dice> 반환
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
        // 1. 임시 리스트에 숫자 순서대로 주사위를 담아 정렬합니다.
        List<Dice> tempDices = new List<Dice>(); // 임시 리스트를 여기서 생성합니다.
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

        // 2. 기존 dices 리스트를 정렬된 임시 리스트의 '복사본'으로 교체합니다.
        // (버그 수정: 그냥 대입하면 같은 리스트를 참조하게 되어 데이터가 사라질 수 있습니다)
        YeokDatabase.dices = new List<Dice>(tempDices);

        // 3. 정렬된 dices 리스트 순서대로 실제 게임 오브젝트의 순서를 재배치합니다.
        // SetAsLastSibling()을 순서대로 호출하면 Hierarchy상에서 오브젝트가 차례로 맨 뒤로 이동하며 정렬됩니다.
        for (int i = 0; i < YeokDatabase.dices.Count; i++)
        {
            YeokDatabase.dices[i].transform.SetAsLastSibling();
        }
    }
}