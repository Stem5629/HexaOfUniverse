using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BoardManager가 스캔을 완료한 후, 어떤 역이 어디서 완성되었는지 알려주는 정보 클래스입니다.
public class CompletedYeokInfo
{
    public BaseTreeEnum YeokType;
    public int BaseScore;
    public int BonusScore;
    public int LineIndex; // 0~5 (가로 또는 세로줄의 인덱스)
    public bool IsHorizontal; // 가로줄인가? (false면 세로줄)
    public System.Collections.Generic.List<Vector2Int> DicePositions; // 역을 구성한 주사위들의 좌표
    public string CombinationString;
}
