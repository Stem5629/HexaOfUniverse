using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHand : MonoBehaviour
{
    [Header("UI ����")]
    [SerializeField] private Button[] diceButtons; // 6���� �ֻ��� ��ư (0��=1��, 1��=2��...)
    [SerializeField] private TextMeshProUGUI[] diceCountTexts; // 6���� �ֻ��� ���� �ؽ�Ʈ

    private int[] diceCounts = new int[6];

    void Start()
    {
        // 6���� �� ��ư��, Ŭ�� �� GameManager�� �Լ��� ȣ���ϵ��� �̺�Ʈ�� �����մϴ�.
        for (int i = 0; i < diceButtons.Length; i++)
        {
            int diceNumber = i; // Ŭ���� ���� ������ ���� �ε��� ����
            diceButtons[i].onClick.AddListener(() => GameManager.Instance.SelectDiceFromHand(diceNumber));
        }
    }

    // ���� ���� �� UI ������Ʈ �Լ����� ������ ����
    public void AddDice(int diceNumber, int count = 1)
    {
        diceCounts[diceNumber] += count;
        UpdateUI();
    }

    public bool HasDice(int diceNumber) => diceCounts[diceNumber] > 0;

    public void UseDice(int diceNumber)
    {
        diceCounts[diceNumber]--;
        UpdateUI();
    }

    public void ReclaimDice(int diceNumber)
    {
        diceCounts[diceNumber]++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < diceCounts.Length; i++)
        {
            diceCountTexts[i].text = diceCounts[i].ToString();
            // �ֻ����� ������ ��ư�� ���� �� ���� ��Ȱ��ȭ
            diceButtons[i].interactable = diceCounts[i] > 0;
        }
    }
}