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
        // --- 기존 요약 부분 (역별 개수) ---
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
        Debug.Log("-------------------------------------------");


        // =======================================================================
        // --- Total Score 분포 분석 (5점 단위) ---

        // 1. 0~59까지 5점 단위 구간을 저장할 딕셔너리 준비
        var scoreDistribution = new Dictionary<int, int>();
        for (int i = 0; i <= 55; i += 5) // 0, 5, 10, ..., 55까지 키를 미리 생성
        {
            scoreDistribution[i] = 0;
        }
        int over60Count = 0; // 60점 이상인 조합을 세는 변수

        // 2. 모든 결과 데이터를 순회하며 점수 구간에 따라 개수 증가
        foreach (var data in results)
        {
            int score = data.totalScore;
            if (score >= 60)
            {
                over60Count++;
            }
            else if (score >= 0)
            {
                // 점수를 5로 나눈 몫에 5를 곱해 구간의 시작점 찾기 (예: 17 -> 15, 4 -> 0)
                int rangeStart = (score / 5) * 5;
                if (scoreDistribution.ContainsKey(rangeStart))
                {
                    scoreDistribution[rangeStart]++;
                }
            }
        }

        // 3. 분석 결과 출력
        Debug.Log("--- Total Score 분포 분석 (5점 단위) ---");
        foreach (var entry in scoreDistribution.OrderBy(e => e.Key))
        {
            int rangeStart = entry.Key;
            int rangeEnd = rangeStart + 4; // 구간의 끝점
            int count = entry.Value;
            Debug.Log($"점수 [{rangeStart.ToString().PadLeft(3)} ~ {rangeEnd.ToString().PadLeft(3)}]: {count} 개");
        }

        // 60점 이상인 조합이 있으면 출력
        if (over60Count > 0)
        {
            Debug.Log($"점수 [ 60 이상      ]: {over60Count} 개");
        }
        Debug.Log("-------------------------------------------");
        // =======================================================================
    }
}