using System.Collections.Generic;

// 이 스크립트는 게임 오브젝트에 붙일 필요가 없습니다.
// 데이터 구조를 정의하는 역할만 합니다.

[System.Serializable]
public class MonsterSpawnInfo
{
    public int difficulty;
    public int count;
}

[System.Serializable]
public class FloorData
{
    // public string floorName;
    public int floorNumber;
    public List<MonsterSpawnInfo> monsterRoster;
}