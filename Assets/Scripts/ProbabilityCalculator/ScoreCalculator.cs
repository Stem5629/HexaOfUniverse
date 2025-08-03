using UnityEngine;
using System.Text;

public class ScoreCalculator : MonoBehaviour
{
    [Header("���� ���� ���� ����")]
    [Tooltip("���� ����ġ�� �����ϴ� ��� t���Դϴ�.")]
    public float t_constant = 0.5f;

    [Tooltip("Ȯ���� �������� ��� m���Դϴ�. ������� �մϴ�.")]
    public float m_constant = 1.0f; // ���� �߰��� m ���

    [Tooltip("������ ����� ���� �ֻ��� ���� k���Դϴ�.")]
    public int k_diceCount = 13;

    void Start()
    {
        CalculateAndPrintAllScores();
    }

    /// <summary>
    /// ��� ���� ���� ������ ����ϰ� �ֿܼ� ����մϴ�.
    /// </summary>
    public void CalculateAndPrintAllScores()
    {
        if (ProbabilityLoader.Instance == null)
        {
            Debug.LogError("ProbabilityLoader�� ���� �����ϴ�! ProbabilityManager ������Ʈ�� Ȯ�����ּ���.");
            return;
        }

        var resultBuilder = new StringBuilder();
        resultBuilder.AppendLine($"--- ���� ��� ��� (t={t_constant}, m={m_constant}, k={k_diceCount}) ---");

        foreach (BaseTreeEnum yeok in System.Enum.GetValues(typeof(BaseTreeEnum)))
        {
            float probabilityPercent = ProbabilityLoader.Instance.GetProbability(k_diceCount, yeok);

            if (probabilityPercent == -1f)
            {
                resultBuilder.AppendLine($"[ {yeok.ToString().PadRight(15)} ] Ȯ�� �����͸� ã�� �� �����ϴ�.");
                continue;
            }

            float score = CalculateScore(probabilityPercent);
            resultBuilder.AppendLine($"[ {yeok.ToString().PadRight(15)} ] Ȯ��: {probabilityPercent:F4}%\t=>\t����: {score:F2}");
        }

        Debug.Log(resultBuilder.ToString());
    }

    /// <summary>
    /// �־��� ���Ŀ� ���� ������ ����մϴ�.
    /// </summary>
    /// <param name="p_percent">���� Ȯ�� (0~100 ������ % ����)</param>
    /// <returns>���� ����</returns>
    private float CalculateScore(float p_percent)
    {
        if (p_percent <= 0)
        {
            return 1000f;
        }

        // Ȯ��(p)�� %���� 0~1 ������ ������ ��ȯ
        float p_ratio = p_percent / 100.0f;

        // �α׿� �� �� (m * p) ���
        float log_argument = m_constant * p_ratio;

        // �α��� �μ��� �׻� 0���� Ŀ�� �ϹǷ�, ���� ó��
        if (log_argument <= 0)
        {
            // �� ���� m�� �����̰ų� 0�� �� �߻� ����
            Debug.LogWarning($"[ {name} ] �α� ��� �Ұ� (m * p <= 0). m�� ������� �մϴ�. ������ ������ ���� ������ �����մϴ�.");
            return 1000f;
        }

        // s = -t * k * log(m * p)
        float rawScore = -t_constant * k_diceCount * Mathf.Log(log_argument);

        // ���� ������ 1���� ������ 1��, �ƴϸ� ���� ���� ���
        float finalScore = Mathf.Max(1.0f, rawScore);

        return finalScore;
    }
}