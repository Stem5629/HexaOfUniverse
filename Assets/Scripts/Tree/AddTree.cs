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

// 주사위 개수 4개 이상 일때부터 추가 점수 역 적용
public class AddTree
{
    private BaseTree baseTree = new BaseTree();
    public bool AllNumber(int[] dicenumbers)
    {
        foreach (var num in dicenumbers)
        {
            if (num >= 3) // 3('별') 이상인 것이 하나라도 있으면
            {
                return false; // 즉시 false 반환
            }
        }
        return true; // 루프가 끝까지 돌았다면 모두 숫자이므로 true 반환
    }
    public bool AllSymbol(int[] dicenumbers)
    {
        foreach (var num in dicenumbers)
        {
            if (num < 3) // 3('별') 미만인 것이 하나라도 있으면
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
