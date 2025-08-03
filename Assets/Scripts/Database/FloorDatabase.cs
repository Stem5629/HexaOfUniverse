using UnityEngine;
using System.Collections.Generic;

// [CreateAssetMenu]를 통해 유니티 에디터에서 이 데이터 파일을 생성할 수 있습니다.
[CreateAssetMenu(fileName = "FloorDatabase", menuName = "BabelDice/Floor Database", order = 1)]
public class FloorDatabase : ScriptableObject
{
    public List<FloorData> allFloors;
}