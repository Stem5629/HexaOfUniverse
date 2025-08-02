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
    

    private int diceIndexCount = 6; // �ֻ��� ���� ��

    private int maxDiceRollLimitCount = 100;
    private int currentDiceRollLimitCount;

    private void Start()
    {
        TMPremainRollCount.text = "���� ���� Ƚ�� : " + maxDiceRollLimitCount;
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
            TMPremainRollCount.text = "���� ���� Ƚ�� : " + currentDiceRollLimitCount;
        }
        else if (0 < currentDiceRollLimitCount && currentDiceRollLimitCount < maxDiceRollLimitCount)
        {
            for (int i = 0; i < YeokDatabase.dices.Count; i++)
            {
                RollDice(YeokDatabase.dices[i]);
            }
            currentDiceRollLimitCount--;
            TMPremainRollCount.text = "���� ���� Ƚ�� : " + currentDiceRollLimitCount;
        }
        else
        {
            TMPremainRollCount.text = "�ֻ��� ���� Ƚ���� ��� �Ҹ��Ͽ����ϴ�..";
            return;
        }
        
    }

    // �Ѱ��� ������ (���Ŀ� �߰��Ҷ� ���)
    private void RollDice(Dice dice) 
    {
        int r = Random.Range(0, diceSprites.Length);

        dice.DiceNumber = r;
        dice.DiceSprite = diceSprites[r];

        dice.DiceSpriteInstance();
    }

    // ���ӿ�����Ʈ ����� + ����Ʈ�� �߰�
    // Dice ��ȯ
    private Dice CreateDice()
    {
        GameObject diceObject = Instantiate(dicePrefab, diceParent);
        Dice dice = diceObject.GetComponent<Dice>();
        YeokDatabase.dices.Add(dice);

        return dice;
    }


    // ���̽� ���迭 => ���迭 ��ư ������ ����
    private void ArrangeDicePool()
    {
        // 1. �ӽ� ����Ʈ�� ���� ������� �ֻ����� ��� �����մϴ�.
        List<Dice> tempDices = new List<Dice>(); // �ӽ� ����Ʈ�� ���⼭ �����մϴ�.
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

    private void GoToBattleScene()
    {
        SceneManager.LoadScene((int)SceneEnum.Battle);
    }
}
