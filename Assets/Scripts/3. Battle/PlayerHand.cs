using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHand : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button[] diceButtons; // 6개의 주사위 버튼 (0번=1눈, 1번=2눈...)
    [SerializeField] private TextMeshProUGUI[] diceCountTexts; // 6개의 주사위 개수 텍스트

    private int[] diceCounts = new int[6];

    void Start()
    {
        for (int i = 0; i < diceButtons.Length; i++)
        {
            int diceNumber = i;

            // 튜토리얼 매니저가 있으면 튜토리얼 매니저에, 없으면 게임 매니저에 연결합니다.
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

    // 개수 관리 및 UI 업데이트 함수들은 이전과 동일
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
            // 주사위가 없으면 버튼을 누를 수 없게 비활성화
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