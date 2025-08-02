using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DiceManager : MonoBehaviour
{
    [SerializeField] private Sprite[] diceSprites = new Sprite[6];
    [SerializeField] private Transform diceParent;
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private int diceRollCount = 7;
    [SerializeField] private Button rollButton;
    [SerializeField] private Button arrangeButton;
    [SerializeField] private TextMeshProUGUI TMPremainRollCount;

    [SerializeField] private Button goButton;
    

    private int diceIndexCount = 6; // 주사위 면의 수

    private int maxDiceRollLimitCount = 100;
    private int currentDiceRollLimitCount;

    private void Start()
    {
        TMPremainRollCount.text = "남은 굴림 횟수 : " + maxDiceRollLimitCount;
        currentDiceRollLimitCount = maxDiceRollLimitCount;
        rollButton.onClick.AddListener(() => RollingDices(diceRollCount));
        arrangeButton.onClick.AddListener(ArrangeDicePool);
        goButton.onClick.AddListener(GoToBattleScene);
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
            for (int i = 0; i < YeokDatabase.dices.Count; i++)
            {
                RollDice(YeokDatabase.dices[i]);
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
        YeokDatabase.dices.Add(dice);

        return dice;
    }


    // 다이스 정배열 => 정배열 버튼 누르면 실행
    private void ArrangeDicePool()
    {
        // 1. 임시 리스트에 숫자 순서대로 주사위를 담아 정렬합니다.
        List<Dice> tempDices = new List<Dice>(); // 임시 리스트를 여기서 생성합니다.
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

    private void GoToBattleScene()
    {
        SceneManager.LoadScene((int)SceneEnum.Battle);
    }
}
