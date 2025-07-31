using UnityEngine;
using System.Text;

public class ScoreCalculator : MonoBehaviour
{
    [Header("점수 공식 변수 설정")]
    [Tooltip("점수 가중치를 조절하는 상수 t값입니다.")]
    public float t_constant = 0.5f;

    [Tooltip("확률에 곱해지는 상수 m값입니다. 양수여야 합니다.")]
    public float m_constant = 1.0f; // 새로 추가된 m 상수

    [Tooltip("점수를 계산할 기준 주사위 개수 k값입니다.")]
    public int k_diceCount = 13;

    void Start()
    {
        CalculateAndPrintAllScores();
    }

    /// <summary>
    /// 모든 역에 대해 점수를 계산하고 콘솔에 출력합니다.
    /// </summary>
    public void CalculateAndPrintAllScores()
    {
        if (ProbabilityLoader.Instance == null)
        {
            Debug.LogError("ProbabilityLoader가 씬에 없습니다! ProbabilityManager 오브젝트를 확인해주세요.");
            return;
        }

        var resultBuilder = new StringBuilder();
        resultBuilder.AppendLine($"--- 점수 계산 결과 (t={t_constant}, m={m_constant}, k={k_diceCount}) ---");

        foreach (BaseTreeEnum yeok in System.Enum.GetValues(typeof(BaseTreeEnum)))
        {
            float probabilityPercent = ProbabilityLoader.Instance.GetProbability(k_diceCount, yeok);

            if (probabilityPercent == -1f)
            {
                resultBuilder.AppendLine($"[ {yeok.ToString().PadRight(15)} ] 확률 데이터를 찾을 수 없습니다.");
                continue;
            }

            float score = CalculateScore(probabilityPercent);
            resultBuilder.AppendLine($"[ {yeok.ToString().PadRight(15)} ] 확률: {probabilityPercent:F4}%\t=>\t점수: {score:F2}");
        }

        Debug.Log(resultBuilder.ToString());
    }

    /// <summary>
    /// 주어진 공식에 따라 점수를 계산합니다.
    /// </summary>
    /// <param name="p_percent">성공 확률 (0~100 사이의 % 단위)</param>
    /// <returns>계산된 점수</returns>
    private float CalculateScore(float p_percent)
    {
        if (p_percent <= 0)
        {
            return 1000f;
        }

        // 확률(p)을 %에서 0~1 사이의 값으로 변환
        float p_ratio = p_percent / 100.0f;

        // 로그에 들어갈 값 (m * p) 계산
        float log_argument = m_constant * p_ratio;

        // 로그의 인수는 항상 0보다 커야 하므로, 예외 처리
        if (log_argument <= 0)
        {
            // 이 경우는 m이 음수이거나 0일 때 발생 가능
            Debug.LogWarning($"[ {name} ] 로그 계산 불가 (m * p <= 0). m은 양수여야 합니다. 점수를 임의의 높은 값으로 설정합니다.");
            return 1000f;
        }

        // s = -t * k * log(m * p)
        float rawScore = -t_constant * k_diceCount * Mathf.Log(log_argument);

        // 계산된 점수가 1보다 작으면 1로, 아니면 계산된 값을 사용
        float finalScore = Mathf.Max(1.0f, rawScore);

        return finalScore;
    }
}