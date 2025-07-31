using System.Collections.Generic;
using UnityEngine;

// CreateAssetMenu를 사용하면 유니티 에디터에서 쉽게 이 데이터베이스 에셋을 생성할 수 있습니다.
[CreateAssetMenu(fileName = "YeokDatabase", menuName = "BabelDice/Yeok Database", order = 0)]
public class YeokDatabase : ScriptableObject
{
    public List<YeokData> allYeokData;
}