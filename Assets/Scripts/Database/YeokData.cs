using System.Collections.Generic;

// [System.Serializable]�� �ٿ��� ����Ƽ�� �����͸� �����ϰ� �ν����Ϳ� ǥ���� �� �ֽ��ϴ�.
[System.Serializable]
public class YeokData
{
    public List<int> combination;
    public BaseTreeEnum foundationYeok;
    public int baseScore;
    public int bonusScore;
    public int totalScore;
}