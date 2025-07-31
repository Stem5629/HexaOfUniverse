using System.Collections.Generic;

// [System.Serializable]을 붙여야 유니티가 데이터를 저장하고 인스펙터에 표시할 수 있습니다.
[System.Serializable]
public class YeokData
{
    public List<int> combination;
    public BaseTreeEnum foundationYeok;
    public int baseScore;
    public int bonusScore;
    public int totalScore;
}