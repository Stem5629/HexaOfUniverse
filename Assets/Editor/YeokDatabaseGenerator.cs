using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class YeokDatabaseGenerator
{
    // 유니티 상단 메뉴에 "BabelDice/Generate Yeok Database"라는 버튼을 만듭니다.
    [MenuItem("BabelDice/Generate Yeok Database")]
    private static void GenerateDatabase()
    {
        // 1. 데이터베이스 에셋 생성 또는 찾기
        string path = "Assets/YeokDatabase.asset";
        YeokDatabase database = AssetDatabase.LoadAssetAtPath<YeokDatabase>(path);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<YeokDatabase>();
            AssetDatabase.CreateAsset(database, path);
        }
        database.allYeokData = new List<YeokData>();

        // 2. 분석에 필요한 모든 도구와 로직 준비
        var combinationGenerator = new CombinationGenerator();
        var baseTreeEvaluator = new BaseTree();
        var addTreeEvaluator = new AddTree();
        var baseTreeScores = new BaseTreeScore();
        var addTreeScores = new AddTreeScore();

        var allCombinations = new List<List<int>>();
        for (int k = 2; k <= 6; k++)
        {
            allCombinations.AddRange(combinationGenerator.Generate(6, k));
        }

        var yeokOrder = System.Enum.GetValues(typeof(BaseTreeEnum)).Cast<BaseTreeEnum>().Reverse().ToArray();

        // 3. 모든 조합을 분석하여 데이터 생성
        foreach (var combo in allCombinations)
        {
            int[] comboArray = combo.ToArray();
            bool isClassified = false;

            foreach (BaseTreeEnum yeok in yeokOrder)
            {
                if (IsYeokMatch(baseTreeEvaluator, yeok, comboArray))
                {
                    int bonusScore = CalculateBonusScore(addTreeEvaluator, addTreeScores, comboArray);
                    int baseScore = GetBaseScore(baseTreeScores, yeok);

                    YeokData data = new YeokData
                    {
                        combination = combo,
                        foundationYeok = yeok,
                        baseScore = baseScore,
                        bonusScore = bonusScore,
                        totalScore = baseScore + bonusScore
                    };
                    database.allYeokData.Add(data);

                    isClassified = true;
                    break;
                }
            }
        }

        // 4. 변경사항 저장 및 완료 메시지
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 기존 Debug.Log 대신 이 함수를 호출
        PrintSummary(database.allYeokData, allCombinations.Count);

        Debug.Log($"성공! {database.allYeokData.Count}개의 의미있는 역 조합을 'YeokDatabase.asset'에 저장했습니다.");
    }

    private static int CalculateBonusScore(AddTree addTree, AddTreeScore scores, int[] combo)
    {
        int score = 0;
        if (addTree.AllNumber(combo)) score += scores.allNumber;
        if (addTree.AllSymbol(combo)) score += scores.allSymbol;
        if (addTree.Pure(combo)) score += scores.pure;
        if (addTree.Mix(combo)) score += scores.mix;
        if (addTree.IncludeStar(combo)) score += scores.includeStar;
        if (addTree.IncludeMoon(combo)) score += scores.includeMoon;
        if (addTree.IncludeSun(combo)) score += scores.includeSun;
        if (addTree.FullDice(combo)) score += scores.fullDice;
        return score;
    }

    private static bool IsYeokMatch(BaseTree baseTree, BaseTreeEnum yeok, int[] combo)
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

    // 모든 enum 케이스를 포함하도록 완성된 GetBaseScore 함수
    private static int GetBaseScore(BaseTreeScore scores, BaseTreeEnum yeok)
    {
        switch (yeok)
        {
            case BaseTreeEnum.pair: return scores.pair;
            case BaseTreeEnum.straight: return scores.straight;
            case BaseTreeEnum.twoPair: return scores.twoPair;
            case BaseTreeEnum.triple: return scores.triple;
            case BaseTreeEnum.threePair: return scores.threePair;
            case BaseTreeEnum.fullHouse: return scores.fullHouse;
            case BaseTreeEnum.fourCard: return scores.fourCard;
            case BaseTreeEnum.doubleTriple: return scores.doubleTriple;
            case BaseTreeEnum.grandFullHouse: return scores.grandFullHouse;
            case BaseTreeEnum.fiveCard: return scores.fiveCard;
            case BaseTreeEnum.doubleStraight: return scores.doubleStraight;
            case BaseTreeEnum.genesis: return scores.genesis;
            case BaseTreeEnum.universe: return scores.universe;
            case BaseTreeEnum.hexa: return scores.hexa;
            default: return 0;
        }
    }

    // 최종 결과를 정리하여 콘솔에 출력하는 함수
    private static void PrintSummary(List<YeokData> results, int totalCombinations)
    {
        // --- 1. 역별 개수 요약 ---
        var groupedResults = results.GroupBy(r => r.foundationYeok)
                                    .ToDictionary(g => g.Key, g => g.ToList());

        Debug.Log("--- 조합 전수 분석 결과 요약 (Sieve Method) ---");

        foreach (BaseTreeEnum yeok in System.Enum.GetValues(typeof(BaseTreeEnum)))
        {
            int count = groupedResults.ContainsKey(yeok) ? groupedResults[yeok].Count : 0;
            Debug.Log($"[{yeok.ToString().PadRight(15)}]: {count} 개");
        }

        int junkCount = totalCombinations - results.Count;
        Debug.Log($"[{"Junk Hand".PadRight(15)}]: {junkCount} 개");


        // --- 2. 점수 분포 분석 (1, 5, 10점 단위로 각각 호출) ---
        AnalyzeAndPrintScoreDistribution(results, 1, 60);
        AnalyzeAndPrintScoreDistribution(results, 5, 60);
        AnalyzeAndPrintScoreDistribution(results, 10, 100); // 10점 단위는 100점 기준으로 분석
    }

    /// <summary>
    /// Total Score 분포를 주어진 간격(interval)으로 분석하고 출력하는 헬퍼 함수
    /// </summary>
    /// <param name="results">분석할 데이터 리스트</param>
    /// <param name="interval">점수 간격 (1, 5, 10 등)</param>
    /// <param name="upperBound">상세 분석할 점수 상한선</param>
    private static void AnalyzeAndPrintScoreDistribution(List<YeokData> results, int interval, int upperBound)
    {
        // 1. 구간별 개수 저장을 위한 딕셔너리 준비
        var scoreDistribution = new Dictionary<int, int>();
        for (int i = 0; i < upperBound; i += interval)
        {
            scoreDistribution[i] = 0;
        }
        int overBoundCount = 0; // 상한선 초과 점수 카운트

        // 2. 모든 데이터를 순회하며 점수 구간에 따라 개수 계산
        foreach (var data in results)
        {
            int score = data.totalScore;
            if (score >= upperBound)
            {
                overBoundCount++;
            }
            else if (score >= 0)
            {
                int rangeStart = (score / interval) * interval;
                if (scoreDistribution.ContainsKey(rangeStart))
                {
                    scoreDistribution[rangeStart]++;
                }
            }
        }

        // 3. 분석 결과 출력
        Debug.Log("-------------------------------------------");
        Debug.Log($"--- Total Score 분포 분석 ({interval}점 단위) ---");

        foreach (var entry in scoreDistribution.OrderBy(e => e.Key))
        {
            int rangeStart = entry.Key;
            int count = entry.Value;

            if (interval == 1)
            {
                // 1점 단위는 "0 ~ 0" 대신 "0"으로만 표시
                Debug.Log($"점수 [{rangeStart,3}]: {count} 개");
            }
            else
            {
                int rangeEnd = rangeStart + interval - 1;
                Debug.Log($"점수 [{rangeStart,3} ~ {rangeEnd,3}]: {count} 개");
            }
        }

        if (overBoundCount > 0)
        {
            Debug.Log($"점수 [{upperBound,3} 이상      ]: {overBoundCount} 개");
        }
    }
}