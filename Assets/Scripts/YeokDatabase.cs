using System.Collections.Generic;
using UnityEngine;

// CreateAssetMenu�� ����ϸ� ����Ƽ �����Ϳ��� ���� �� �����ͺ��̽� ������ ������ �� �ֽ��ϴ�.
[CreateAssetMenu(fileName = "YeokDatabase", menuName = "BabelDice/Yeok Database", order = 0)]
public class YeokDatabase : ScriptableObject
{
    public List<YeokData> allYeokData;
}