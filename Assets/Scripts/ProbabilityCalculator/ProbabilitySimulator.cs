using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO; // ���� ������� ���� �߰��ؾ� �մϴ�.

public class ProbabilitySimulator : MonoBehaviour
{
    [Header("�ùķ��̼� ����")]
    [Tooltip("�׽�Ʈ�ϰ� ���� �ֻ��� Ǯ�� ũ����� �Է��ϼ���.")]
    public List<int> poolSizesToTest = new List<int> { 7, 10, 13, 15 };

    [Tooltip("�ùķ��̼� �ݺ� Ƚ��. �������� ��Ȯ������ ���� �ɸ��ϴ�.")]
    public int simulationRuns = 10000;

    void Start()
    {
        RunSimulation();
    }

    public void RunSimulation()
    {
        var baseTreeEvaluator = new BaseTree();
        var results = new Dictionary<BaseTreeEnum, List<float>>();
        var yeokOrder = System.Enum.GetValues(typeof(BaseTreeEnum)).Cast<BaseTreeEnum>().ToArray();

        foreach (var yeok in yeokOrder) { results[yeok] = new List<float>(); }

        foreach (int poolSize in poolSizesToTest)
        {
            var successCounts = new Dictionary<BaseTreeEnum, int>();
            foreach (var yeok in yeokOrder) { successCounts[yeok] = 0; }

            for (int i = 0; i < simulationRuns; i++)
            {
                int[] currentPool = new int[poolSize];
                for (int j = 0; j < poolSize; j++) { currentPool[j] = Random.Range(0, 6); }

                foreach (var yeok in yeokOrder)
                {
                    if (IsYeokMatch(baseTreeEvaluator, yeok, currentPool))
                    {
                        successCounts[yeok]++;
                    }
                }
            }
            foreach (var yeok in yeokOrder)
            {
                float probability = ((float)successCounts[yeok] / simulationRuns) * 100f;
                results[yeok].Add(probability);
            }
        }

        PrintResults(results);

        // ===== �߿�: �ùķ��̼��� ���� �� ����� CSV ���Ϸ� �����ϴ� �Լ� ȣ�� =====
        SaveResultsToCSV(results);
    }

    private void PrintResults(Dictionary<BaseTreeEnum, List<float>> results)
    {
        var resultBuilder = new StringBuilder();
        resultBuilder.AppendLine("--- �ֻ��� Ǯ ũ�⺰ '���� Ȯ��' �� �м� (���е� ���) ---");

        int columnWidth = 15;
        string header = "�� �̸�".PadRight(17) + "|";
        foreach (int poolSize in poolSizesToTest)
        {
            header += $" {poolSize}�� Ǯ (%)".PadRight(columnWidth) + "|";
        }
        resultBuilder.AppendLine(header);
        resultBuilder.AppendLine(new string('-', header.Length));

        foreach (var result in results.OrderBy(r => r.Key))
        {
            string row = result.Key.ToString().PadRight(15) + " |";
            foreach (float prob in result.Value)
            {
                row += $" {prob:F4}".PadLeft(columnWidth - 2) + " % |";
            }
            resultBuilder.AppendLine(row);
        }

        Debug.Log(resultBuilder.ToString());
    }

    // =======================================================================
    // ===== ���� �߰��� CSV ���� �޼��� =====
    // =======================================================================
    private void SaveResultsToCSV(Dictionary<BaseTreeEnum, List<float>> results)
    {
        // ���� ��θ� ������Ʈ�� Assets ������ �����մϴ�.
        // �̷��� �ϸ� � PC������ ������Ʈ ���� ���� ������ �����˴ϴ�.
        string filePath = Path.Combine(Application.dataPath, "ProbabilityResults.csv");

        // StringBuilder�� ����Ͽ� CSV ���Ͽ� �� ������ ȿ�������� ����ϴ�.
        var csvBuilder = new StringBuilder();

        // 1. ���(ù ��° ��)�� ����ϴ�.
        // ex: "�� �̸�,7�� Ǯ (%),10�� Ǯ (%),13�� Ǯ (%),15�� Ǯ (%)"
        List<string> headerElements = new List<string> { "�� �̸�" };
        foreach (int poolSize in poolSizesToTest)
        {
            headerElements.Add($"{poolSize}�� Ǯ (%)");
        }
        csvBuilder.AppendLine(string.Join(",", headerElements));

        // 2. ������ ����� ����ϴ�.
        // Enum ������� ����� �����Ͽ� ����մϴ�.
        foreach (var result in results.OrderBy(r => r.Key))
        {
            List<string> rowElements = new List<string> { result.Key.ToString() };
            foreach (float prob in result.Value)
            {
                // �Ҽ��� 4�ڸ����� ����մϴ�.
                rowElements.Add(prob.ToString("F4"));
            }
            csvBuilder.AppendLine(string.Join(",", rowElements));
        }

        // 3. ���Ͽ� ������ ���ϴ�.
        // StreamWriter�� ����Ͽ� �ѱ��� ������ �ʵ��� UTF-8 ���ڵ��� ����մϴ�.
        using (StreamWriter streamWriter = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)))
        {
            streamWriter.Write(csvBuilder.ToString());
        }

        Debug.Log($"CSV ���� ���� �Ϸ�: {filePath}");
    }

    private bool IsYeokMatch(BaseTree baseTree, BaseTreeEnum yeok, int[] combo)
    {
        switch (yeok)
        {
            case BaseTreeEnum.pair: return baseTree.Pair(combo);
            case BaseTreeEnum.straight: return baseTree.Straight(combo);
            case BaseTreeEnum.twoPair: return baseTree.TwoPair(combo);
            case BaseTreeEnum.triple: return baseTree.Triple(combo);
            case BaseTreeEnum.threePair: return baseTree.ThreePair(combo);
            case BaseTreeEnum.fullHouse: return baseTree.FullHouse(combo);
            case BaseTreeEnum.fourCard: return baseTree.FourCard(combo);
            case BaseTreeEnum.doubleTriple: return baseTree.DoubleTriple(combo);
            case BaseTreeEnum.grandFullHouse: return baseTree.GrandFullHouse(combo);
            case BaseTreeEnum.fiveCard: return baseTree.FiveCard(combo);
            case BaseTreeEnum.doubleStraight: return baseTree.DoubleStraight(combo);
            case BaseTreeEnum.genesis: return baseTree.Genesis(combo);
            case BaseTreeEnum.universe: return baseTree.Universe(combo);
            case BaseTreeEnum.hexa: return baseTree.Hexa(combo);
            default: return false;
        }
    }

    // BaseTree�� BaseTreeEnum�� ���⿡ ���ԵǾ� �ִٰ� �����մϴ�.
}