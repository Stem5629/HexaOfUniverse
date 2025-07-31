using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance { private set; get; }

    [SerializeField] private Sprite[] diceSprites = new Sprite[6];
    [SerializeField] private Transform diceParent;
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private int diceRollCount;
    [SerializeField] private Button rollButton;
    [SerializeField] private Button arrangeButton;
    [SerializeField] private TextMeshProUGUI TMPremainRollCount;
    

    private int diceIndexCount = 6; // 주사위 면의 수

    private List<Dice> dices = new List<Dice>();

    private int maxDiceRollLimitCount = 100;
    private int currentDiceRollLimitCount;
    public Sprite[] DiceSprites { get => diceSprites; set { diceSprites = value; } }
    public int DiceRollCount { get => diceRollCount; set { diceRollCount = value; } }
    public List<Dice> Dices { get => dices; set { dices = value; } }

    void Awake()
    {
        Instance = this;
        if (Instance != this) Destroy(gameObject); // 중복 오브젝트 방지

        // 씬이 로드되어도 오브젝트가 파괴되지 않음
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TMPremainRollCount.text = "남은 굴림 횟수 : " + maxDiceRollLimitCount;
        currentDiceRollLimitCount = maxDiceRollLimitCount;
        rollButton.onClick.AddListener(() => RollingDices(diceRollCount));
        arrangeButton.onClick.AddListener(ArrangeDicePool);
    }

    private void RollingDices(int rollCount)
    {
        if (currentDiceRollLimitCount == maxDiceRollLimitCount)
        {
            for (int i = 0; i < rollCount; i++)
            {
                RollDice(CreateDice());
            }
            currentDiceRollLimitCount--;
            TMPremainRollCount.text = "남은 굴림 횟수 : " + currentDiceRollLimitCount;
        }
        else if (0 < currentDiceRollLimitCount && currentDiceRollLimitCount < maxDiceRollLimitCount)
        {
            for (int i = 0; i < dices.Count; i++)
            {
                RollDice(dices[i]);
            }
            currentDiceRollLimitCount--;
            TMPremainRollCount.text = "남은 굴림 횟수 : " + currentDiceRollLimitCount;
        }
        else
        {
            TMPremainRollCount.text = "주사위 굴림 횟수를 모두 소모하였습니다..";
            return;
        }
        
    }

    // 한개만 굴리기 (이후에 추가할때 사용)
    private void RollDice(Dice dice) 
    {
        int r = Random.Range(0, diceSprites.Length);

        dice.DiceNumber = r;
        dice.DiceSprite = diceSprites[r];

        dice.DiceSpriteInstance();
    }

    // 게임오브젝트 만들기 + 리스트에 추가
    // Dice 반환
    private Dice CreateDice() 
    {
        GameObject diceObject = Instantiate(dicePrefab, diceParent);
        Dice dice = diceObject.GetComponent<Dice>();
        dices.Add(dice);

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

    // 다이스 정배열 => 정배열 버튼 누르면 실행
    private void ArrangeDicePool()
    {
        // 1. 임시 리스트에 숫자 순서대로 주사위를 담아 정렬합니다.
        List<Dice> tempDices = new List<Dice>(); // 임시 리스트를 여기서 생성합니다.
        for (int i = 0; i < diceIndexCount; i++)
        {
            for (int j = 0; j < dices.Count; j++)
            {
                if (dices[j].DiceNumber == i)
                {
                    tempDices.Add(dices[j]);
                }
            }
        }

        // 2. 기존 dices 리스트를 정렬된 임시 리스트의 '복사본'으로 교체합니다.
        // (버그 수정: 그냥 대입하면 같은 리스트를 참조하게 되어 데이터가 사라질 수 있습니다)
        dices = new List<Dice>(tempDices);

        // 3. 정렬된 dices 리스트 순서대로 실제 게임 오브젝트의 순서를 재배치합니다.
        // SetAsLastSibling()을 순서대로 호출하면 Hierarchy상에서 오브젝트가 차례로 맨 뒤로 이동하며 정렬됩니다.
        for (int i = 0; i < dices.Count; i++)
        {
            dices[i].transform.SetAsLastSibling();
        }
    }
}
