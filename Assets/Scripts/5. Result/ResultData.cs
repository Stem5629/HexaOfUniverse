using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResultData
{
    public enum GameOutcome { Victory, Defeat, Draw }

    public static GameOutcome Outcome;
    public static string MyNickname;
    public static string OpponentNickname;

    // (����) �߰��ϰ� ���� ��� ������ ������
    // public static string MyBestYeok;
    // public static string OpponentBestYeok;
}
