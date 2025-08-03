using System.Collections.Generic;

// �� ��ũ��Ʈ�� ���� ������Ʈ�� ���� �ʿ䰡 �����ϴ�.
// ������ ������ �����ϴ� ���Ҹ� �մϴ�.

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