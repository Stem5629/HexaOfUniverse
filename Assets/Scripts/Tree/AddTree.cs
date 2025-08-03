using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddTreeScore
{
    public int allNumber = 3;
    public int allSymbol = 2;
    public int pure = 4;
    public int mix = 3;
    public int includeStar = 1;
    public int includeMoon = 2;
    public int includeSun = 3;
    public int fullDice = 4;
}

// �ֻ��� ���� 4�� �̻� �϶����� �߰� ���� �� ����
public class AddTree
{
    private BaseTree baseTree = new BaseTree();
    public bool AllNumber(int[] dicenumbers)
    {
        foreach (var num in dicenumbers)
        {
            if (num >= 3) // 3('��') �̻��� ���� �ϳ��� ������
            {
                return false; // ��� false ��ȯ
            }
        }
        return true; // ������ ������ ���Ҵٸ� ��� �����̹Ƿ� true ��ȯ
    }
    public bool AllSymbol(int[] dicenumbers)
    {
        foreach (var num in dicenumbers)
        {
            if (num < 3) // 3('��') �̸��� ���� �ϳ��� ������
            {
                return false;
            }
        }
        return true;
    }
    public bool Pure(int[] dicenumbers)
    {
        bool b = true;

        for (int i = 1; i < dicenumbers.Length; i++)
        {
            b &= (dicenumbers[0] == dicenumbers[i]);
        }

        return b;
    }

    public bool Mix(int[] dicenumbers)
    {
        return !baseTree.Pair(dicenumbers);
    }


    public bool IncludedNumber(int[] dicenumbers, int number)
    {
        bool b = false;
        for (int i = 0; i < dicenumbers.Length; i++)
        {
            if (dicenumbers[i] == number) return true;
        }
        return b;
    }
    public bool IncludeStar(int[] dicenumbers)
    {
        return IncludedNumber(dicenumbers, 3);
    }
    public bool IncludeMoon(int[] dicenumbers)
    {
        return IncludedNumber(dicenumbers, 4);
    }
    public bool IncludeSun(int[] dicenumbers)
    {
        return IncludedNumber(dicenumbers, 5);
    }

    public bool FullDice(int[] dicenumbers)
    {
        return (dicenumbers.Length == 6);
    }
}
