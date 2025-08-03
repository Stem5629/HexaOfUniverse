using System.Collections.Generic;
using System.Linq; // LINQ를 사용하기 위해 추가
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{

    [Header("UI (Enemy)")]
    [SerializeField] private Transform enemyParent; // 부모 트랜스폼으로 변경
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

    // --- 새로 추가할 변수 ---
    [Header("Prefabs")]
    [SerializeField] private GameObject dicePrefab; // 인스펙터에서 Dice 프리팹을 연결해주세요.

    private List<Dice> enemyDices = new List<Dice>();
    private List<Dice> playerDices = new List<Dice>();


    private BattleManager battleManager;

    private BaseTree baseTreeEvaluator = new BaseTree();
    private AddTree addTreeEvaluator = new AddTree();
    private BaseTreeScore baseTreeScores = new BaseTreeScore();
    private AddTreeScore addTreeScores = new AddTreeScore();

    // --- 상태 관리를 위한 새 변수 추가 ---
    private bool isBattleConfirmed = false;
    private int finalPlayerScore = 0;
    private int finalEnemyScore = 0;
    private int finalPlayerBaseScore = 0; // 플레이어의 기본 점수 저장
    private int enemyBaseScore = 0;       // 적의 기본 점수 저장

    void Start()
    {
        // Start 함수에서 버튼 리스너를 연결합니다.
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
            Debug.Log("총점 동점! 기본 점수 비교...");
            if (finalPlayerBaseScore > enemyBaseScore)
            {
                playerWins = true;
            }
            else if (finalPlayerBaseScore == enemyBaseScore)
            {
                // 기본 점수까지 동점일 경우, 새로운 상세 비교 함수를 호출합니다.
                Debug.Log("기본 점수 동점! 주사위 상세 비교...");
                playerWins = DecideTieBreaker();
            }
        }

        // 최종 결과 처리
        if (playerWins)
        {
            int scoreDifference = finalPlayerScore - finalEnemyScore;
            Debug.Log($"승리! 점수 차이: {scoreDifference}, 기본 점수: {finalPlayerBaseScore}");
            battleManager.RecordVictory(scoreDifference);
        }
        else
        {
            Debug.LogWarning($"패배... (플레이어: {finalPlayerScore} vs 적: {finalEnemyScore})");
            battleManager.RecordVictory(0); // 패배 시에는 0점 전달
        }

        isBattleConfirmed = true;
        LockInteraction();
        battleManager.OnEnemyDefeated();
    }

    /// <summary>
    /// 총점과 기본 점수까지 모두 동점일 때, 주사위 구성을 상세히 비교하여 승패를 결정합니다.
    /// 플레이어가 이기면 true, 지거나 비기면 false를 반환합니다.
    /// </summary>
    private bool DecideTieBreaker()
    {
        // 1. 각자의 주사위 구성을 '숫자:개수' 형태의 딕셔너리(빈도 맵)로 변환합니다.
        var playerFreq = playerDices.GroupBy(d => d.DiceNumber)
                                    .ToDictionary(g => g.Key, g => g.Count());
        var enemyFreq = enemyDices.GroupBy(d => d.DiceNumber)
                                  .ToDictionary(g => g.Key, g => g.Count());

        // 2. 주사위 숫자가 높은 순서대로 비교하기 위해 각 딕셔너리를 정렬합니다.
        var playerSorted = playerFreq.OrderByDescending(p => p.Key).ToList();
        var enemySorted = enemyFreq.OrderByDescending(p => p.Key).ToList();

        // 3. 더 적은 종류의 주사위를 가진 쪽을 기준으로 반복 횟수를 정합니다.
        int loopCount = Mathf.Min(playerSorted.Count, enemySorted.Count);

        for (int i = 0; i < loopCount; i++)
        {
            var playerPair = playerSorted[i]; // 플레이어의 i번째로 높은 주사위와 개수
            var enemyPair = enemySorted[i];   // 적의 i번째로 높은 주사위와 개수

            // 비교 1: 주사위 숫자(값)를 비교합니다.
            if (playerPair.Key > enemyPair.Key) return true; // 플레이어 승
            if (playerPair.Key < enemyPair.Key) return false; // 플레이어 패

            // 주사위 숫자가 같다면, 비교 2: 해당 주사위의 개수를 비교합니다.
            if (playerPair.Value > enemyPair.Value) return true; // 플레이어 승
            if (playerPair.Value < enemyPair.Value) return false; // 플레이어 패
        }

        // 4. 루프가 끝났다는 것은, 한쪽이 가진 주사위 종류를 모두 비교했다는 의미입니다.
        // 만약 플레이어가 더 다양한 종류의 주사위를 가지고 있다면, 그 다음 높은 주사위가 있으므로 승리입니다.
        if (playerSorted.Count > enemySorted.Count) return true;
        if (playerSorted.Count < enemySorted.Count) return false;

        // 5. 모든 주사위의 종류, 숫자, 개수가 완벽하게 똑같다면, 규칙에 따라 적이 승리합니다.
        return false;
    }

    /// <summary>
    /// 이 적과의 모든 UI 상호작용을 비활성화합니다.
    /// </summary>
    private void LockInteraction()
    {
        // 플레이어가 낸 주사위를 더 이상 클릭(회수)할 수 없게 만듭니다.
        foreach (Transform child in playerParent)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        // 전투 확정 버튼도 비활성화합니다.
        button_ConfirmBattle.interactable = false;

        // 시각적 피드백: 패널을 약간 어둡게 만들어 끝났음을 표시
        GetComponent<Image>().color = Color.gray;
    }


    public void Setup(YeokData data, BattleManager manager)
    {
        this.battleManager = manager;
        this.enemyBaseScore = data.baseScore; // 적의 기본 점수 저장

        // --- 기존 정보 표시 로직 (변경 없음) ---
        // 1. 기본 역, 총점 정보 표시
        TMP_EnemyBaseTree.text = data.foundationYeok.ToString();
        TMP_EnemyScore.text = data.totalScore.ToString();

        // 2. 추가 역 정보 문자열 생성 및 표시
        int[] comboArray = data.combination.ToArray();
        List<string> addTreeNames = new List<string>();

        if (comboArray.Length >= 4)
        {
            if (addTreeEvaluator.AllNumber(comboArray)) addTreeNames.Add("올넘버");
            if (addTreeEvaluator.AllSymbol(comboArray)) addTreeNames.Add("올심볼");
            if (addTreeEvaluator.Pure(comboArray)) addTreeNames.Add("질서");
            if (addTreeEvaluator.Mix(comboArray)) addTreeNames.Add("혼돈");
            if (addTreeEvaluator.IncludeStar(comboArray)) addTreeNames.Add("별");
            if (addTreeEvaluator.IncludeMoon(comboArray)) addTreeNames.Add("달");
            if (addTreeEvaluator.IncludeSun(comboArray)) addTreeNames.Add("해");
            if (addTreeEvaluator.FullDice(comboArray)) addTreeNames.Add("풀다이스");
        }

        if (addTreeNames.Any())
        {
            TMP_EnemyAddTree.text = $" {string.Join(", ", addTreeNames)}";
        }
        else
        {
            TMP_EnemyAddTree.text = "추가 역: 없음";
        }

        // --- 여기에 새로운 기능 추가 ---
        // 3. 이전에 생성된 주사위가 있다면 모두 파괴
        foreach (Transform child in enemyParent)
        {
            Destroy(child.gameObject);
        }
        enemyDices.Clear(); // 리스트도 비워줍니다.

        // 4. 데이터에 따라 적의 주사위 조합을 실제로 생성
        if (dicePrefab != null)
        {
            foreach (int diceValue in data.combination)
            {
                // 주사위 오브젝트 생성
                GameObject diceObject = Instantiate(dicePrefab, enemyParent);

                // 1. 생성된 주사위에서 Button 컴포넌트를 가져옵니다.
                Button diceButton = diceObject.GetComponent<Button>();

                // 2. Button 컴포넌트가 존재하면 interactable 속성을 false로 설정합니다.
                if (diceButton != null)
                {
                    diceButton.interactable = false;
                }

                Dice newDice = diceObject.GetComponent<Dice>();

                // Dice.cs 스크립트에 데이터 설정
                newDice.DiceNumber = diceValue; //
                // DiceManager의 스프라이트 배열에서 올바른 스프라이트를 찾아 할당
                newDice.DiceSprite = battleManager.DiceSprites[diceValue];

                // 이미지 업데이트
                newDice.DiceSpriteInstance(); //

                // 생성된 주사위를 리스트에 추가 (관리용)
                enemyDices.Add(newDice);
            }
        }
        else
        {
            Debug.LogError("Enemy 스크립트에 Dice Prefab이 할당되지 않았습니다!");
        }

        Debug.Log($"적 생성 완료: {data.foundationYeok} / {string.Join(", ", data.combination)} / 총점 {data.totalScore}");
    }


    public void ReceivePlayerDice(Dice diceData)
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Enemy 스크립트에 Dice Prefab이 할당되지 않았습니다!");
            return;
        }

        // 1. 플레이어의 주사위를 자신의 playerParent 아래에 생성
        GameObject diceObject = Instantiate(dicePrefab, playerParent);
        Dice newDice = diceObject.GetComponent<Dice>();

        // 2. 전달받은 데이터로 정보 설정
        newDice.DiceNumber = diceData.DiceNumber;
        newDice.DiceSprite = battleManager.DiceSprites[newDice.DiceNumber];
        newDice.DiceSpriteInstance();

        // 3. 새로 생긴 주사위를 클릭해서 다시 손으로 되돌리는 기능 추가를 위해 리스너 연결
        Button diceButton = diceObject.GetComponent<Button>();
        if (diceButton != null)
        {
            diceButton.onClick.AddListener(() => ReturnDiceToHand(newDice));
        }

        // 4. 생성된 주사위를 내부 리스트에 추가하여 관리
        playerDices.Add(newDice);

        // --- 여기에 판정 및 UI 업데이트 함수 호출 추가 ---
        UpdatePlayerYeokDisplay();
    }


    /// <summary>
    /// 플레이어가 낸 주사위 목록을 바탕으로 족보와 점수를 계산하고 UI를 업데이트합니다.
    /// </summary>
    private void UpdatePlayerYeokDisplay()
    {
        // 1. 판정을 위해 List<Dice>를 int[] 배열로 변환합니다.
        int[] comboArray = playerDices.Select(d => d.DiceNumber).ToArray();

        // 만약 낸 주사위가 없다면 UI를 초기화합니다.
        if (comboArray.Length == 0)
        {
            TMP_PlayerBaseTree.text = "-";
            TMP_PlayerAddTree.text = "-";
            TMP_PlayerScore.text = "0";
            return;
        }

        // 2. 기본 족보 판정 (가장 높은 우선순위의 족보 하나만 찾기)
        BaseTreeEnum foundBaseYeok = default;
        int baseScore = 0;
        bool yeokFound = false;

        // Enum 순서를 역으로 뒤집어 우선순위가 높은 것부터 판정
        var yeokOrder = System.Enum.GetValues(typeof(BaseTreeEnum)).Cast<BaseTreeEnum>().Reverse().ToArray();

        foreach (var yeok in yeokOrder)
        {
            if (IsYeokMatch(yeok, comboArray))
            {
                foundBaseYeok = yeok;
                baseScore = GetBaseScore(yeok);
                yeokFound = true;
                break; // 가장 높은 족보를 찾았으므로 중단
            }
        }

        this.finalPlayerBaseScore = baseScore; // 계산된 기본 점수를 멤버 변수에 저장

        // 3. 추가 족보 판정 (해당하는 모든 추가 족보 점수를 합산)
        int bonusScore = 0;
        List<string> addTreeNames = new List<string>(); // 항상 비어있는 리스트로 초기화

        if (comboArray.Length >= 4)
        {
            // 이 계산 로직을 if문 안으로 이동합니다.
            bonusScore = CalculateBonusScore(comboArray);
            addTreeNames = GetBonusYeokNames(comboArray);
        }

        // 4. 최종 점수 계산 및 UI 텍스트 업데이트
        int totalScore = baseScore + bonusScore;

        TMP_PlayerBaseTree.text = yeokFound ? $"{foundBaseYeok} ({baseScore}점)" : "족보 없음";
        TMP_PlayerAddTree.text = addTreeNames.Any() ? $"{string.Join(", ", addTreeNames)} ({bonusScore}점)" : "추가 족보 없음";
        TMP_PlayerScore.text = totalScore.ToString();
    }

    /// <summary>
    /// 플레이어가 낸 주사위를 클릭했을 때, 손패로 되돌립니다.
    /// </summary>
    public void ReturnDiceToHand(Dice diceToReturn)
    {
        if (diceToReturn == null) return;

        // 1. BattleManager에게 이 주사위를 되돌려달라고 요청
        battleManager.ReclaimDiceFromPlayerPanel(diceToReturn);

        // 2. 이 Enemy 인스턴스가 관리하던 주사위 목록에서 제거
        playerDices.Remove(diceToReturn);

        // 3. 주사위가 하나 줄었으므로, 남은 주사위로 족보와 점수를 다시 계산
        UpdatePlayerYeokDisplay();
    }


    // --- 아래는 YeokDatabaseGenerator에서 가져온 판정 헬퍼 함수들입니다 ---

    private List<string> GetBonusYeokNames(int[] combo)
    {
        var names = new List<string>();
        if (addTreeEvaluator.AllNumber(combo)) names.Add("올넘버");
        if (addTreeEvaluator.AllSymbol(combo)) names.Add("올심볼");
        if (addTreeEvaluator.Pure(combo)) names.Add("질서");
        if (addTreeEvaluator.Mix(combo)) names.Add("혼돈");
        if (addTreeEvaluator.IncludeStar(combo)) names.Add("별");
        if (addTreeEvaluator.IncludeMoon(combo)) names.Add("달");
        if (addTreeEvaluator.IncludeSun(combo)) names.Add("해");
        if (addTreeEvaluator.FullDice(combo)) names.Add("풀다이스");
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