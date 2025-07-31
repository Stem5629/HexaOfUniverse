using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class YeokDatabaseGenerator
{
    // ����Ƽ ��� �޴��� "BabelDice/Generate Yeok Database"��� ��ư�� ����ϴ�.
    [MenuItem("BabelDice/Generate Yeok Database")]
    private static void GenerateDatabase()
    {
        // 1. �����ͺ��̽� ���� ���� �Ǵ� ã��
        string path = "Assets/YeokDatabase.asset";
        YeokDatabase database = AssetDatabase.LoadAssetAtPath<YeokDatabase>(path);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<YeokDatabase>();
            AssetDatabase.CreateAsset(database, path);
        }
        database.allYeokData = new List<YeokData>();

        // 2. �м��� �ʿ��� ��� ������ ���� �غ�
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

        // 3. ��� ������ �м��Ͽ� ������ ����
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

        // 4. ������� ���� �� �Ϸ� �޽���
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ���� Debug.Log ��� �� �Լ��� ȣ��
        PrintSummary(database.allYeokData, allCombinations.Count);

        Debug.Log($"����! {database.allYeokData.Count}���� �ǹ��ִ� �� ������ 'YeokDatabase.asset'�� �����߽��ϴ�.");
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

    // ��� enum ���̽��� �����ϵ��� �ϼ��� GetBaseScore �Լ�
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

    // ���� ����� �����Ͽ� �ֿܼ� ����ϴ� �Լ�
    private static void PrintSummary(List<YeokData> results, int totalCombinations)
    {
        // --- ���� ��� �κ� (���� ����) ---
        var groupedResults = results.GroupBy(r => r.foundationYeok)
                                    .ToDictionary(g => g.Key, g => g.ToList());

        Debug.Log("--- ���� ���� �м� ��� ��� (Sieve Method) ---");

        foreach (BaseTreeEnum yeok in System.Enum.GetValues(typeof(BaseTreeEnum)))
        {
            int count = groupedResults.ContainsKey(yeok) ? groupedResults[yeok].Count : 0;
            Debug.Log($"[{yeok.ToString().PadRight(15)}]: {count} ��");
        }

        int junkCount = totalCombinations - results.Count;
        Debug.Log($"[{"Junk Hand".PadRight(15)}]: {junkCount} ��");
        Debug.Log("-------------------------------------------");


        // =======================================================================
        // --- Total Score ���� �м� (5�� ����) ---

        // 1. 0~59���� 5�� ���� ������ ������ ��ųʸ� �غ�
        var scoreDistribution = new Dictionary<int, int>();
        for (int i = 0; i <= 55; i += 5) // 0, 5, 10, ..., 55���� Ű�� �̸� ����
        {
            scoreDistribution[i] = 0;
        }
        int over60Count = 0; // 60�� �̻��� ������ ���� ����

        // 2. ��� ��� �����͸� ��ȸ�ϸ� ���� ������ ���� ���� ����
        foreach (var data in results)
        {
            int score = data.totalScore;
            if (score >= 60)
            {
                over60Count++;
            }
            else if (score >= 0)
            {
                // ������ 5�� ���� �� 5�� ���� ������ ������ ã�� (��: 17 -> 15, 4 -> 0)
                int rangeStart = (score / 5) * 5;
                if (scoreDistribution.ContainsKey(rangeStart))
                {
                    scoreDistribution[rangeStart]++;
                }
            }
        }

        // 3. �м� ��� ���
        Debug.Log("--- Total Score ���� �м� (5�� ����) ---");
        foreach (var entry in scoreDistribution.OrderBy(e => e.Key))
        {
            int rangeStart = entry.Key;
            int rangeEnd = rangeStart + 4; // ������ ����
            int count = entry.Value;
            Debug.Log($"���� [{rangeStart.ToString().PadLeft(3)} ~ {rangeEnd.ToString().PadLeft(3)}]: {count} ��");
        }

        // 60�� �̻��� ������ ������ ���
        if (over60Count > 0)
        {
            Debug.Log($"���� [ 60 �̻�      ]: {over60Count} ��");
        }
        Debug.Log("-------------------------------------------");
        // =======================================================================
    }
}