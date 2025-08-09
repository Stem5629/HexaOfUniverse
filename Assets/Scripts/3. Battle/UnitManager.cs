using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 24���� �ܰ� ������ �����ϰ�, ������ ��ȯ, ����, �ı��� ó���մϴ�.
/// FieldInitializer�� ���� �ʱ�ȭ�ǰ�, GameManager�� ���� ����˴ϴ�.
/// </summary>
public class UnitManager : MonoBehaviourPunCallbacks
{
    [Header("UI �� �ð� ȿ�� ����")]
    // �� ��(Yeok)�� �ش��ϴ� ������ 2D ��������Ʈ�� ������� �Ҵ��ؾ� �մϴ�.
    // (BaseTreeEnum�� ������ ��ġ�ؾ� ��: pair, twoPair, triple ...)
    [SerializeField] private Sprite[] unitSpritesByYeok;
    [SerializeField] private Color myColor = Color.red; // �� ������ ����
    [SerializeField] private Color versusColor = Color.blue; // ��� ������ ����

    // --- ������ ---
    private GameObject[] perimeterSlots; // 24���� �ܰ� ���� (FieldInitializer�� ����)
    private Dictionary<GameObject, UnitData> unitDataOnBoard = new Dictionary<GameObject, UnitData>();

    private void Start()
    {
        // ���� ������ GameManager�� ã�� �ڽ��� ���
        GameManager.Instance.SetUnitManager(this);
    }

    /// <summary>
    /// FieldInitializer�� ȣ���Ͽ� ���� ������ �����ϴ� �ʱ�ȭ �Լ�
    /// </summary>
    public void Initialize(GameObject[] slots)
    {
        this.perimeterSlots = slots;
        unitDataOnBoard.Clear();
    }

    /// <summary>
    /// GameManager�� ȣ��. ��� Ŭ���̾�Ʈ���� ������ ��ȯ�϶�� RPC ����� �����ϴ�.
    /// </summary>
    public void SummonUnitsViaRPC(List<CompletedYeokInfo> yeoks, Player owner)
    {
        // Photon RPC�� �����ϱ� ���� �����͸� ������ �迭 ���·� ��ȯ�մϴ�.
        int[] yeokTypes = new int[yeoks.Count];
        int[] lineIndices = new int[yeoks.Count];
        bool[] isHorizontals = new bool[yeoks.Count];

        for (int i = 0; i < yeoks.Count; i++)
        {
            yeokTypes[i] = (int)yeoks[i].YeokType;
            lineIndices[i] = yeoks[i].LineIndex;
            isHorizontals[i] = yeoks[i].IsHorizontal;
        }

        // ��� Ŭ���̾�Ʈ���� SummonUnitsRPC �Լ��� �����϶�� ����մϴ�.
        photonView.RPC("SummonUnitsRPC", RpcTarget.All, owner, yeokTypes, lineIndices, isHorizontals);
    }

    /// <summary>
    /// [RPC] ��� Ŭ���̾�Ʈ���� ����Ǿ� ������ ������ ��ȯ�ϴ� �Լ�
    /// </summary>
    [PunRPC]
    private void SummonUnitsRPC(Player owner, int[] yeokTypes, int[] lineIndices, bool[] isHorizontals)
    {
        Debug.Log($"{owner.NickName}���� {yeokTypes.Length}���� ���� �ϼ��Ͽ� ������ ��ȯ�մϴ�!");

        for (int i = 0; i < yeokTypes.Length; i++)
        {
            CompletedYeokInfo yeokInfo = new CompletedYeokInfo
            {
                YeokType = (BaseTreeEnum)yeokTypes[i],
                LineIndex = lineIndices[i],
                IsHorizontal = isHorizontals[i]
            };

            // UnitData newUnitData = CreateUnitDataFromYeok(yeokInfo, owner); <-- ���� �ڵ�� ����

            int[] slotIndices = GetPerimeterIndices(yeokInfo);

            // �� ���Կ� ��ġ�� ������ ���ο� ���� �����͸� �����մϴ�.
            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[0]]);
            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[1]]);
        }
    }

    private void AttemptToPlaceUnit(UnitData attackerData, GameObject targetSlot)
    {
        if (unitDataOnBoard.TryGetValue(targetSlot, out UnitData defenderData))
        {
            if (defenderData.Owner != attackerData.Owner)
            {
                ResolveCombat(attackerData, defenderData, targetSlot);
            }
        }
        else
        {
            PlaceUnit(attackerData, targetSlot);
        }
    }

    private void ResolveCombat(UnitData attacker, UnitData defender, GameObject defenderSlot)
    {
        int damageDealt = attacker.InitialDamage;
        defender.HP -= damageDealt;

        if (defender.HP <= 0)
        {
            unitDataOnBoard.Remove(defenderSlot);
            int overkillDamage = -defender.HP;
            if (overkillDamage > 0 && PhotonNetwork.IsMasterClient) // ����ų �������� ���常 ����Ͽ� ����ȭ
            {
                HealthManager.Instance.DealDamage(defender.Owner, overkillDamage);
            }
            PlaceUnit(attacker, defenderSlot);
        }
    }

    private void PlaceUnit(UnitData unit, GameObject slot)
    {
        unitDataOnBoard[slot] = unit;

        // --- ���� UI ������Ʈ ---
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];
        slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
    }

    /// <summary>
    /// GameManager�� �� ���� �� ȣ��. ��� ������ ���� �������� ó���մϴ�.
    /// (������ Ŭ���̾�Ʈ������ ȣ��Ǿ�� �մϴ�.)
    /// </summary>
    public void ApplyContinuousDamage()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var entry in unitDataOnBoard)
        {
            UnitData unit = entry.Value;
            Player targetPlayer = (unit.Owner == PhotonNetwork.MasterClient) ? PhotonNetwork.PlayerListOthers[0] : PhotonNetwork.MasterClient;

            if (unit.ContinuousDamage > 0)
            {
                HealthManager.Instance.DealDamage(targetPlayer, unit.ContinuousDamage);
            }
        }
    }

    // �� �����κ��� UnitData�� �����ϴ� ���丮 �޼���
    private UnitData CreateUnitDataFromYeok(CompletedYeokInfo yeok, Player owner)
    {
        UnitData unit = new UnitData { Owner = owner, YeokType = yeok.YeokType };

        // TODO: �� ���� �´� �ɷ�ġ�� ���� ��ȹ�Ͽ� ä���� �մϴ�. (�뷱�� �ٽ�)
        switch (yeok.YeokType)
        {
            case BaseTreeEnum.pair: unit.HP = 20; unit.InitialDamage = 10; unit.ContinuousDamage = 2; break;
            case BaseTreeEnum.triple: unit.HP = 30; unit.InitialDamage = 25; unit.ContinuousDamage = 4; break;
            case BaseTreeEnum.straight: unit.HP = 15; unit.InitialDamage = 15; unit.ContinuousDamage = 5; break;
            // ... �ٸ� ��� ���� ���� �ɷ�ġ ���� ...
            default: unit.HP = 10; unit.InitialDamage = 5; unit.ContinuousDamage = 1; break;
        }
        return unit;
    }

    private int[] GetPerimeterIndices(CompletedYeokInfo yeokInfo)
    {
        int[] indices = new int[2];
        if (yeokInfo.IsHorizontal)
        {
            // ���� Ÿ�� �ε��� ��� ������ �����մϴ�.
            indices[0] = 12 + yeokInfo.LineIndex; // 17 - yeokInfo.LineIndex -> 12 + yeokInfo.LineIndex �� ����
            indices[1] = 18 + yeokInfo.LineIndex; // �������� �����̹Ƿ� �״�� �Ӵϴ�.
        }
        else
        {
            // ������ ����� �����̹Ƿ� �״�� �Ӵϴ�.
            indices[0] = yeokInfo.LineIndex;
            indices[1] = 6 + yeokInfo.LineIndex;
        }
        return indices;
    }
}