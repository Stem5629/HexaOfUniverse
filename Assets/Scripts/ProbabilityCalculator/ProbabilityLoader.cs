using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Linq ����� ���� �߰�


public class ProbabilityLoader : MonoBehaviour
{
    // �ҷ��� Ȯ�� �����͸� ������ ��ø ��ųʸ�
    // [Key: �ֻ��� ����, Value: [Key: �� �̸�, Value: Ȯ��]]
    public Dictionary<int, Dictionary<BaseTreeEnum, float>> loadedProbabilities;

    // �ٸ� ��ũ��Ʈ���� ���� ������ �� �ֵ��� �̱��� �ν��Ͻ��� ����ϴ�.
    public static ProbabilityLoader Instance { get; private set; }

    void Awake()
    {
        // �̱��� ���� ����
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٲ� �ı����� �ʵ��� ����
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ������ ���۵� �� CSV ���Ͽ��� �����͸� �ҷ��ɴϴ�.
        LoadProbabilityData();
    }

    /// <summary>
    /// Assets/ProbabilityResults.csv ���Ͽ��� Ȯ�� �����͸� �ҷ��ɴϴ�.
    /// </summary>
    public void LoadProbabilityData()
    {
        // ������ ���� �ʱ�ȭ
        loadedProbabilities = new Dictionary<int, Dictionary<BaseTreeEnum, float>>();
        string filePath = Path.Combine(Application.dataPath, "ProbabilityResults.csv");

        if (!File.Exists(filePath))
        {
            Debug.LogError("������ ã�� �� �����ϴ�: " + filePath);
            return;
        }

        try
        {
            // StreamReader�� ����Ͽ� ������ �� ������ �н��ϴ�.
            using (var reader = new StreamReader(filePath))
            {
                // 1. ��� �б� (�ֻ��� ���� ������ ��� ����)
                string headerLine = reader.ReadLine();
                if (headerLine == null) return;

                // ����� ��ǥ�� �и� (ex: "�� �̸�", "7�� Ǯ (%)", "10�� Ǯ (%)", ...)
                string[] headers = headerLine.Split(',');

                // "7�� Ǯ (%)"���� ���� 7�� �����Ͽ� �ֻ��� ���� ����Ʈ�� ����ϴ�.
                List<int> diceCountsInFile = headers
                    .Skip(1) // ù ��° "�� �̸�" ���� �ǳʶݴϴ�.
                    .Select(header => int.Parse(header.Split('��')[0].Trim()))
                    .ToList();

                // �� �ֻ��� ������ ���� ���� ��żŸ��� �ʱ�ȭ�մϴ�.
                foreach (int count in diceCountsInFile)
                {
                    loadedProbabilities[count] = new Dictionary<BaseTreeEnum, float>();
                }

                // 2. ������ �� �б�
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');

                    // ù ��° ��(�� �̸�)�� BaseTreeEnum Ÿ������ ��ȯ�մϴ�.
                    BaseTreeEnum yeok = (BaseTreeEnum)System.Enum.Parse(typeof(BaseTreeEnum), values[0]);

                    // �� �ֻ��� ������ �ش��ϴ� Ȯ�� ���� �����մϴ�.
                    for (int i = 0; i < diceCountsInFile.Count; i++)
                    {
                        int currentDiceCount = diceCountsInFile[i]; // ex: 7, 10, 13, ...
                        float probability = float.Parse(values[i + 1]); // ex: 99.9800, 100.0000, ...

                        // ���� ������ ������ ����
                        loadedProbabilities[currentDiceCount][yeok] = probability;
                    }
                }
            }

            Debug.Log("CSV Ȯ�� �����͸� ���������� �ҷ��Խ��ϴ�!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("CSV ���� ó�� �� ���� �߻�: " + e.Message);
        }
    }

    /// <summary>
    /// ����� �����Ϳ��� Ư�� Ȯ�� ���� �������� ���� �Լ�
    /// </summary>
    /// <param name="diceCount">�������� ���� �ֻ��� ����</param>
    /// <param name="yeok">�������� ���� ���� ����</param>
    /// <returns>����� Ȯ�� ��. �����Ͱ� ������ -1�� ��ȯ�մϴ�.</returns>
    public float GetProbability(int diceCount, BaseTreeEnum yeok)
    {
        if (loadedProbabilities != null && loadedProbabilities.ContainsKey(diceCount))
        {
            if (loadedProbabilities[diceCount].ContainsKey(yeok))
            {
                return loadedProbabilities[diceCount][yeok];
            }
        }

        // ��û�� �����Ͱ� ���� ���
        Debug.LogWarning($"Ȯ�� �����͸� ã�� �� �����ϴ�: �ֻ��� {diceCount}��, �� {yeok}");
        return -1f; // ������ �ǹ��ϴ� ��
    }
}