using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

public class UnitData
{
    public Player Owner; // �� ������ ���� (Photon �÷��̾� ����)
    public BaseTreeEnum YeokType; // � ������ ��ȯ�Ǿ��°�
    public int HP;
    public int InitialDamage;
    public int ContinuousDamage;
}
