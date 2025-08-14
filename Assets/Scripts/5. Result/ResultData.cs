using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResultData
{
    public enum GameOutcome { Victory, Defeat, Draw }

    public static GameOutcome Outcome;
    public static string MyNickname;
    public static string OpponentNickname;

    // (선택) 추가하고 싶은 통계 데이터 변수들
    // public static string MyBestYeok;
    // public static string OpponentBestYeok;
}
