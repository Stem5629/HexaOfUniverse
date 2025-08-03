using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO; // 파일 입출력을 위해 추가해야 합니다.

public class ProbabilitySimulator : MonoBehaviour
{
    [Header("시뮬레이션 설정")]
    [Tooltip("테스트하고 싶은 주사위 풀의 크기들을 입력하세요.")]
    public List<int> poolSizesToTest = new List<int> { 7, 10, 13, 15 };

    [Tooltip("시뮬레이션 반복 횟수. 높을수록 정확하지만 오래 걸립니다.")]
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

        // ===== 중요: 시뮬레이션이 끝난 후 결과를 CSV 파일로 저장하는 함수 호출 =====
        SaveResultsToCSV(results);
    }

    private void PrintResults(Dictionary<BaseTreeEnum, List<float>> results)
    {
        var resultBuilder = new StringBuilder();
        resultBuilder.AppendLine("--- 주사위 풀 크기별 '실전 확률' 비교 분석 (정밀도 향상) ---");

        int columnWidth = 15;
        string header = "역 이름".PadRight(17) + "|";
        foreach (int poolSize in poolSizesToTest)
        {
            header += $" {poolSize}개 풀 (%)".PadRight(columnWidth) + "|";
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
    // ===== 새로 추가된 CSV 저장 메서드 =====
    // =======================================================================
    private void SaveResultsToCSV(Dictionary<BaseTreeEnum, List<float>> results)
    {
        // 파일 경로를 프로젝트의 Assets 폴더로 지정합니다.
        // 이렇게 하면 어떤 PC에서든 프로젝트 폴더 내에 파일이 생성됩니다.
        string filePath = Path.Combine(Application.dataPath, "ProbabilityResults.csv");

        // StringBuilder를 사용하여 CSV 파일에 쓸 내용을 효율적으로 만듭니다.
        var csvBuilder = new StringBuilder();

        // 1. 헤더(첫 번째 줄)를 만듭니다.
        // ex: "역 이름,7개 풀 (%),10개 풀 (%),13개 풀 (%),15개 풀 (%)"
        List<string> headerElements = new List<string> { "역 이름" };
        foreach (int poolSize in poolSizesToTest)
        {
            headerElements.Add($"{poolSize}개 풀 (%)");
        }
        csvBuilder.AppendLine(string.Join(",", headerElements));

        // 2. 데이터 행들을 만듭니다.
        // Enum 순서대로 결과를 정렬하여 기록합니다.
        foreach (var result in results.OrderBy(r => r.Key))
        {
            List<string> rowElements = new List<string> { result.Key.ToString() };
            foreach (float prob in result.Value)
            {
                // 소수점 4자리까지 기록합니다.
                rowElements.Add(prob.ToString("F4"));
            }
            csvBuilder.AppendLine(string.Join(",", rowElements));
        }

        // 3. 파일에 내용을 씁니다.
        // StreamWriter를 사용하여 한글이 깨지지 않도록 UTF-8 인코딩을 사용합니다.
        using (StreamWriter streamWriter = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)))
        {
            streamWriter.Write(csvBuilder.ToString());
        }

        Debug.Log($"CSV 파일 저장 완료: {filePath}");
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

    // BaseTree와 BaseTreeEnum은 여기에 포함되어 있다고 가정합니다.
}