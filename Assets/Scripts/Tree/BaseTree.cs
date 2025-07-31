using System.Collections.Generic;
using System.Linq;

// 1. 점수 데이터를 담는 클래스
public class BaseTreeScore
{
    public int pair = 1;
    public int twoPair = 2;
    public int triple = 4;
    public int fullHouse = 6;
    public int threePair = 9;
    public int straight = 3;
    public int fourCard = 13;
    public int doubleTriple = 24;
    public int grandFullHouse = 20;
    public int doubleStraight = 19;
    public int fiveCard = 27;
    public int universe = 21;
    public int hexa = 42;
    public int genesis = 40;
}

// 2. 역의 종류와 우선순위를 나타내는 Enum
public enum BaseTreeEnum
{
    pair,
    twoPair,
    triple,
    fullHouse,
    threePair,
    straight,
    fourCard,
    doubleTriple,
    grandFullHouse,
    doubleStraight,
    fiveCard,
    universe,
    hexa,
    genesis
}

// 3. 역 해금 상태를 저장하는 구조체
public struct OpenedBaseTree
{
    public bool pair, straight, twoPair, triple, threePair, fullHouse, fourCard,
                doubleTriple, grandFullHouse, fiveCard, doubleStraight, genesis, universe, hexa;
}

// 4. 실제 판정 로직을 담고 있는 메인 클래스
public class BaseTree
{
    private OpenedBaseTree openedBaseTree;

    public BaseTree()
    {
        // 실제 게임에서는 '역 진화 트리'의 해금 상태에 따라 이 값을 설정해야 합니다.
        // 현재는 모든 역이 해금되었다고 가정합니다.
        openedBaseTree = new OpenedBaseTree
        {
            pair = true,
            straight = true,
            twoPair = true,
            triple = true,
            threePair = true,
            fullHouse = true,
            fourCard = true,
            doubleTriple = true,
            grandFullHouse = true,
            fiveCard = true,
            doubleStraight = true,
            genesis = true,
            universe = true,
            hexa = true
        };
    }

    // 모든 판별 로직의 기반이 되는 '빈도수 계산' 헬퍼 함수
    private Dictionary<int, int> GetFrequencyMap(int[] diceNumbers)
    {
        var counts = new Dictionary<int, int>();
        foreach (var num in diceNumbers)
        {
            if (!counts.ContainsKey(num))
            {
                counts[num] = 0;
            }
            counts[num]++;
        }
        return counts;
    }

    // --- 역 판별 함수 (n개 풀에서 재료 존재 여부 확인) ---

    public bool Pair(int[] d)
    {
        if (!openedBaseTree.pair) return false;
        return GetFrequencyMap(d).Values.Any(count => count >= 2);
    }

    public bool TwoPair(int[] d)
    {
        if (!openedBaseTree.twoPair) return false;
        // B 방식('관대한 정의')을 최종 채택: 풀 안에서 페어를 총 2번 이상 만들 수 있는가?
        int pairGroups = 0;
        foreach (var count in GetFrequencyMap(d).Values)
        {
            pairGroups += count / 2;
        }
        return pairGroups >= 2;
    }

    public bool ThreePair(int[] d)
    {
        if (!openedBaseTree.threePair) return false;
        if (d.Length < 6) return false;
        var counts = GetFrequencyMap(d);
        // 2개 이상인 눈의 종류가 3가지 이상인가?
        return counts.Values.Count(count => count >= 2) >= 3;
    }

    public bool Triple(int[] d)
    {
        if (!openedBaseTree.triple) return false;
        return GetFrequencyMap(d).Values.Any(count => count >= 3);
    }

    public bool FourCard(int[] d)
    {
        if (!openedBaseTree.fourCard) return false;
        return GetFrequencyMap(d).Values.Any(count => count >= 4);
    }

    public bool FiveCard(int[] d)
    {
        if (!openedBaseTree.fiveCard) return false;
        return GetFrequencyMap(d).Values.Any(count => count >= 5);
    }

    public bool Hexa(int[] d)
    {
        if (!openedBaseTree.hexa) return false;
        return GetFrequencyMap(d).Values.Any(count => count >= 6);
    }

    public bool FullHouse(int[] d)
    {
        if (!openedBaseTree.fullHouse) return false;
        var counts = GetFrequencyMap(d);
        var values = counts.Values.ToList();
        values.Sort((a, b) => b.CompareTo(a));
        // 3개 이상 그룹과, 2개 이상 그룹이 별개로 존재하는지 확인
        return values.Count >= 2 && values[0] >= 3 && values[1] >= 2;
    }

    public bool GrandFullHouse(int[] d)
    {
        if (!openedBaseTree.grandFullHouse) return false;
        var counts = GetFrequencyMap(d);
        var values = counts.Values.ToList();
        values.Sort((a, b) => b.CompareTo(a));
        // 4개 이상 그룹과, 2개 이상 그룹이 별개로 존재하는지 확인
        return values.Count >= 2 && values[0] >= 4 && values[1] >= 2;
    }

    public bool DoubleTriple(int[] d)
    {
        if (!openedBaseTree.doubleTriple) return false;
        if (d.Length < 6) return false;
        var counts = GetFrequencyMap(d);
        // 3개 이상인 눈의 종류가 2가지 이상인가?
        return counts.Values.Count(count => count >= 3) >= 2;
    }

    public bool Genesis(int[] d)
    {
        if (!openedBaseTree.genesis) return false;
        var counts = GetFrequencyMap(d);
        // '1'(0)이 3개 이상 있고, '해'(5)가 3개 이상 있는가?
        return counts.ContainsKey(0) && counts[0] >= 3 && counts.ContainsKey(5) && counts[5] >= 3;
    }

    public bool Straight(int[] d)
    {
        if (!openedBaseTree.straight) return false;
        var counts = GetFrequencyMap(d);
        bool hasNumeric = counts.ContainsKey(0) && counts.ContainsKey(1) && counts.ContainsKey(2);
        bool hasCelestial = counts.ContainsKey(3) && counts.ContainsKey(4) && counts.ContainsKey(5);
        return hasNumeric || hasCelestial;
    }

    public bool DoubleStraight(int[] d)
    {
        if (!openedBaseTree.doubleStraight) return false;
        var counts = GetFrequencyMap(d);
        bool isNumericDouble = counts.ContainsKey(0) && counts[0] >= 2 && counts.ContainsKey(1) && counts[1] >= 2 && counts.ContainsKey(2) && counts[2] >= 2;
        bool isCelestialDouble = counts.ContainsKey(3) && counts[3] >= 2 && counts.ContainsKey(4) && counts[4] >= 2 && counts.ContainsKey(5) && counts[5] >= 2;
        bool isUniverseLike = counts.ContainsKey(0) && counts.ContainsKey(1) && counts.ContainsKey(2) &&
                              counts.ContainsKey(3) && counts.ContainsKey(4) && counts.ContainsKey(5);
        return isNumericDouble || isCelestialDouble || isUniverseLike;
    }

    public bool Universe(int[] d)
    {
        if (!openedBaseTree.universe) return false;
        // 6종류의 눈이 모두 1개 이상씩 있는가?
        return GetFrequencyMap(d).Count >= 6;
    }
}