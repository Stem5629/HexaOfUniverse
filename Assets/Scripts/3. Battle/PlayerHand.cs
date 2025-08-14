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
        for (int i = 0; i < diceButtons.Length; i++)
        {
            int diceNumber = i;

            // Ʃ�丮�� �Ŵ����� ������ Ʃ�丮�� �Ŵ�����, ������ ���� �Ŵ����� �����մϴ�.
            if (TutorialManager.Instance != null)
            {
                diceButtons[i].onClick.AddListener(() => TutorialManager.Instance.OnDiceSelectedFromHand(diceNumber));
            }
            else
            {
                diceButtons[i].onClick.AddListener(() => GameManager.Instance.SelectDiceFromHand(diceNumber));
            }
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

    public int GetDiceCount(int diceNumber)
    {
        return diceCounts[diceNumber];
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

    public void ClearAllDice()
    {
        for (int i = 0; i < diceCounts.Length; i++)
        {
            diceCounts[i] = 0;
        }
        UpdateUI();
    }

    public int GetTotalDiceCount()
    {
        int total = 0;
        for (int i = 0; i < diceCounts.Length; i++)
        {
            total += diceCounts[i];
        }
        return total;
    }
}