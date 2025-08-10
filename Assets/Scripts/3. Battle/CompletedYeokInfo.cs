using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BoardManager�� ��ĵ�� �Ϸ��� ��, � ���� ��� �ϼ��Ǿ����� �˷��ִ� ���� Ŭ�����Դϴ�.
public class CompletedYeokInfo
{
    public BaseTreeEnum YeokType;
    public int BaseScore;
    public int BonusScore;
    public int LineIndex; // 0~5 (���� �Ǵ� �������� �ε���)
    public bool IsHorizontal; // �������ΰ�? (false�� ������)
    public System.Collections.Generic.List<Vector2Int> DicePositions; // ���� ������ �ֻ������� ��ǥ
    public string CombinationString;
}
