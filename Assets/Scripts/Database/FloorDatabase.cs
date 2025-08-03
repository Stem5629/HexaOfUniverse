using UnityEngine;
using System.Collections.Generic;

// [CreateAssetMenu]�� ���� ����Ƽ �����Ϳ��� �� ������ ������ ������ �� �ֽ��ϴ�.
[CreateAssetMenu(fileName = "FloorDatabase", menuName = "BabelDice/Floor Database", order = 1)]
public class FloorDatabase : ScriptableObject
{
    public List<FloorData> allFloors;
}