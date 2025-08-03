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

    private AddTree addTreeEvaluator = new AddTree();

    private BattleManager battleManager;

    public void Setup(YeokData data, BattleManager manager)
    {
        this.battleManager = manager;

        // --- 기존 정보 표시 로직 (변경 없음) ---
        // 1. 기본 역, 총점 정보 표시
        TMP_EnemyBaseTree.text = data.foundationYeok.ToString();
        TMP_EnemyScore.text = data.totalScore.ToString();

        // 2. 추가 역 정보 문자열 생성 및 표시
        int[] comboArray = data.combination.ToArray();
        List<string> addTreeNames = new List<string>();

        if (addTreeEvaluator.AllNumber(comboArray)) addTreeNames.Add("올넘버");
        if (addTreeEvaluator.AllSymbol(comboArray)) addTreeNames.Add("올심볼");
        if (addTreeEvaluator.Pure(comboArray)) addTreeNames.Add("질서");
        if (addTreeEvaluator.Mix(comboArray)) addTreeNames.Add("혼돈");
        if (addTreeEvaluator.IncludeStar(comboArray)) addTreeNames.Add("별");
        if (addTreeEvaluator.IncludeMoon(comboArray)) addTreeNames.Add("달");
        if (addTreeEvaluator.IncludeSun(comboArray)) addTreeNames.Add("해");
        if (addTreeEvaluator.FullDice(comboArray)) addTreeNames.Add("풀다이스");

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

        // 3. (선택) 새로 생긴 주사위를 클릭해서 다시 손으로 되돌리는 기능 추가를 위해 리스너 연결
        Button diceButton = diceObject.GetComponent<Button>();
        if (diceButton != null)
        {
            // 예: diceButton.onClick.AddListener(() => ReturnDiceToHand(newDice));
        }

        // 4. 생성된 주사위를 내부 리스트에 추가하여 관리
        playerDices.Add(newDice);
    }
}