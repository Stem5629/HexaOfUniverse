using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

public class UnitData
{
    public Player Owner; // 이 유닛의 주인 (Photon 플레이어 정보)
    public BaseTreeEnum YeokType; // 어떤 역으로 소환되었는가
    public int HP;
    public int InitialDamage;
    public int ContinuousDamage;
    public string CombinationString;
    public bool IsTutorialEnemy = false;
}
