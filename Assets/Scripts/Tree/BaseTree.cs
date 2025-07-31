using System.Collections.Generic;
using System.Linq;

// 1. ���� �����͸� ��� Ŭ����
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

// 2. ���� ������ �켱������ ��Ÿ���� Enum
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

// 3. �� �ر� ���¸� �����ϴ� ����ü
public struct OpenedBaseTree
{
    public bool pair, straight, twoPair, triple, threePair, fullHouse, fourCard,
                doubleTriple, grandFullHouse, fiveCard, doubleStraight, genesis, universe, hexa;
}

// 4. ���� ���� ������ ��� �ִ� ���� Ŭ����
public class BaseTree
{
    private OpenedBaseTree openedBaseTree;

    public BaseTree()
    {
        // ���� ���ӿ����� '�� ��ȭ Ʈ��'�� �ر� ���¿� ���� �� ���� �����ؾ� �մϴ�.
        // ����� ��� ���� �رݵǾ��ٰ� �����մϴ�.
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

    // ��� �Ǻ� ������ ����� �Ǵ� '�󵵼� ���' ���� �Լ�
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

    // --- �� �Ǻ� �Լ� (n�� Ǯ���� ��� ���� ���� Ȯ��) ---

    public bool Pair(int[] d)
    {
        if (!openedBaseTree.pair) return false;
        return GetFrequencyMap(d).Values.Any(count => count >= 2);
    }

    public bool TwoPair(int[] d)
    {
        if (!openedBaseTree.twoPair) return false;
        // B ���('������ ����')�� ���� ä��: Ǯ �ȿ��� �� �� 2�� �̻� ���� �� �ִ°�?
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
        // 2�� �̻��� ���� ������ 3���� �̻��ΰ�?
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
        // 3�� �̻� �׷��, 2�� �̻� �׷��� ������ �����ϴ��� Ȯ��
        return values.Count >= 2 && values[0] >= 3 && values[1] >= 2;
    }

    public bool GrandFullHouse(int[] d)
    {
        if (!openedBaseTree.grandFullHouse) return false;
        var counts = GetFrequencyMap(d);
        var values = counts.Values.ToList();
        values.Sort((a, b) => b.CompareTo(a));
        // 4�� �̻� �׷��, 2�� �̻� �׷��� ������ �����ϴ��� Ȯ��
        return values.Count >= 2 && values[0] >= 4 && values[1] >= 2;
    }

    public bool DoubleTriple(int[] d)
    {
        if (!openedBaseTree.doubleTriple) return false;
        if (d.Length < 6) return false;
        var counts = GetFrequencyMap(d);
        // 3�� �̻��� ���� ������ 2���� �̻��ΰ�?
        return counts.Values.Count(count => count >= 3) >= 2;
    }

    public bool Genesis(int[] d)
    {
        if (!openedBaseTree.genesis) return false;
        var counts = GetFrequencyMap(d);
        // '1'(0)�� 3�� �̻� �ְ�, '��'(5)�� 3�� �̻� �ִ°�?
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
        // 6������ ���� ��� 1�� �̻� �ִ°�?
        return GetFrequencyMap(d).Count >= 6;
    }
}