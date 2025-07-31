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
    

    private int diceIndexCount = 6; // �ֻ��� ���� ��

    private List<Dice> dices = new List<Dice>();

    private int maxDiceRollLimitCount = 100;
    private int currentDiceRollLimitCount;
    public Sprite[] DiceSprites { get => diceSprites; set { diceSprites = value; } }
    public int DiceRollCount { get => diceRollCount; set { diceRollCount = value; } }
    public List<Dice> Dices { get => dices; set { dices = value; } }

    void Awake()
    {
        Instance = this;
        if (Instance != this) Destroy(gameObject); // �ߺ� ������Ʈ ����

        // ���� �ε�Ǿ ������Ʈ�� �ı����� ����
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TMPremainRollCount.text = "���� ���� Ƚ�� : " + maxDiceRollLimitCount;
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
            TMPremainRollCount.text = "���� ���� Ƚ�� : " + currentDiceRollLimitCount;
        }
        else if (0 < currentDiceRollLimitCount && currentDiceRollLimitCount < maxDiceRollLimitCount)
        {
            for (int i = 0; i < dices.Count; i++)
            {
                RollDice(dices[i]);
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
        dices.Add(dice);

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

    // ���̽� ���迭 => ���迭 ��ư ������ ����
    private void ArrangeDicePool()
    {
        // 1. �ӽ� ����Ʈ�� ���� ������� �ֻ����� ��� �����մϴ�.
        List<Dice> tempDices = new List<Dice>(); // �ӽ� ����Ʈ�� ���⼭ �����մϴ�.
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

        // 2. ���� dices ����Ʈ�� ���ĵ� �ӽ� ����Ʈ�� '���纻'���� ��ü�մϴ�.
        // (���� ����: �׳� �����ϸ� ���� ����Ʈ�� �����ϰ� �Ǿ� �����Ͱ� ����� �� �ֽ��ϴ�)
        dices = new List<Dice>(tempDices);

        // 3. ���ĵ� dices ����Ʈ ������� ���� ���� ������Ʈ�� ������ ���ġ�մϴ�.
        // SetAsLastSibling()�� ������� ȣ���ϸ� Hierarchy�󿡼� ������Ʈ�� ���ʷ� �� �ڷ� �̵��ϸ� ���ĵ˴ϴ�.
        for (int i = 0; i < dices.Count; i++)
        {
            dices[i].transform.SetAsLastSibling();
        }
    }
}
