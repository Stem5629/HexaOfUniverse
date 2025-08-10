using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

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

    [Header("���� ����â UI")]
    public GameObject infoPanel; // ����â �г� ������Ʈ
    public TextMeshProUGUI unitTreeText;
    public TextMeshProUGUI treeCombinationText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI beginDamageText;
    public TextMeshProUGUI keepDamageText;

    // --- ������ ---
    private GameObject[] perimeterSlots; // 24���� �ܰ� ���� (FieldInitializer�� ����)
    private Dictionary<GameObject, UnitData> unitDataOnBoard = new Dictionary<GameObject, UnitData>();

    /*private void Start()
    {
        // ���� ������ GameManager�� ã�� �ڽ��� ���
        GameManager.Instance.SetUnitManager(this);
    }*/

    /// <summary>
    /// FieldInitializer�� ȣ���Ͽ� ���� ������ �����ϴ� �ʱ�ȭ �Լ�
    /// </summary>
    public void Initialize(GameObject[] slots)
    {
        this.perimeterSlots = slots;
        unitDataOnBoard.Clear();
        infoPanel.SetActive(false); // ���� �� ����â�� �׻� ��Ȱ��ȭ

        // �� ���Կ� Ŭ�� �̺�Ʈ�� �߰��մϴ�.
        foreach (var slot in perimeterSlots)
        {
            Button button = slot.GetComponent<Button>();
            if (button == null) button = slot.AddComponent<Button>(); // ��ư�� ������ ���� �߰�

            button.onClick.RemoveAllListeners();
            // Ŭ�� �� OnUnitSlotClicked �Լ��� ȣ��ǵ��� �����մϴ�.
            button.onClick.AddListener(() => OnUnitSlotClicked(slot));
        }
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
        string[] combinationStrings = new string[yeoks.Count];

        for (int i = 0; i < yeoks.Count; i++)
        {
            yeokTypes[i] = (int)yeoks[i].YeokType;
            lineIndices[i] = yeoks[i].LineIndex;
            isHorizontals[i] = yeoks[i].IsHorizontal;
            combinationStrings[i] = yeoks[i].CombinationString;
        }

        // ��� Ŭ���̾�Ʈ���� SummonUnitsRPC �Լ��� �����϶�� ����մϴ�.
        photonView.RPC("SummonUnitsRPC", RpcTarget.All, owner, yeokTypes, lineIndices, isHorizontals, combinationStrings);
    }

    /// <summary>
    /// [RPC] ��� Ŭ���̾�Ʈ���� ����Ǿ� ������ ������ ��ȯ�ϴ� �Լ�
    /// </summary>
    [PunRPC]
    private void SummonUnitsRPC(Player owner, int[] yeokTypes, int[] lineIndices, bool[] isHorizontals, string[] combinationStrings)
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
            yeokInfo.CombinationString = combinationStrings[i];
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

        // --- �뷱���� ���� ������ ---
        // �� ���鸸 �����ϸ� ��ü ������ �ɷ�ġ�� ����˴ϴ�.
        const float hpMultiplier = 2.5f;     // ���� ��� ü�� ����
        const float initialDamageMultiplier = 1.8f; // ���� ��� �ʱ� ������ ����
        const float continuousDamageMultiplier = 0.5f; // ���� ��� ���� ������ ����

        // --- 1. '��'�� �⺻ ���� �������� ---
        int baseScore = GetBaseScore(yeok.YeokType);

        // --- 2. ������ ������� �ɷ�ġ �ڵ� ��� ---
        unit.HP = Mathf.RoundToInt(baseScore * hpMultiplier);
        unit.InitialDamage = Mathf.RoundToInt(baseScore * initialDamageMultiplier);
        unit.ContinuousDamage = Mathf.RoundToInt(baseScore * continuousDamageMultiplier);

        // --- ���� ó��: �ּ� �ɷ�ġ ���� ---
        // ������ ���� ���̶� �ּ����� ������ ������ �����մϴ�.
        unit.HP = Mathf.Max(10, unit.HP);
        unit.InitialDamage = Mathf.Max(5, unit.InitialDamage);
        unit.ContinuousDamage = Mathf.Max(1, unit.ContinuousDamage);
        unit.CombinationString = yeok.CombinationString;

        return unit;
    }

    // 4. Ŭ�� �� ����� ���ο� �Լ� �߰�
    private void OnUnitSlotClicked(GameObject clickedSlot)
    {
        // Ŭ���� ���Կ� ���� �����Ͱ� �ִ��� Ȯ���մϴ�.
        if (unitDataOnBoard.TryGetValue(clickedSlot, out UnitData unitData))
        {
            // ������ �ִٸ�, ����â�� �׻� Ȱ��ȭ�ϰ� ������ ������Ʈ�մϴ�.
            infoPanel.SetActive(true);

            unitTreeText.text = unitData.YeokType.ToString();
            treeCombinationText.text = unitData.CombinationString;
            hpText.text = $"HP : {unitData.HP}";
            beginDamageText.text = $"�ʱⵥ���� : {unitData.InitialDamage}";
            keepDamageText.text = $"���ӵ����� : {unitData.ContinuousDamage}";
        }
        else // �� ������ Ŭ���� ���
        {
            // ������ ���ٸ� ����â�� ��Ȱ��ȭ�մϴ�.
            infoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// BaseTree���� �ش� ���� �⺻ ������ �������� ���� �Լ�
    /// </summary>
    private int GetBaseScore(BaseTreeEnum yeok)
    {
        BaseTreeScore scores = new BaseTreeScore();
        switch (yeok)
        {
            case BaseTreeEnum.pair: return scores.pair;
            case BaseTreeEnum.twoPair: return scores.twoPair;
            case BaseTreeEnum.triple: return scores.triple;
            case BaseTreeEnum.fullHouse: return scores.fullHouse;
            case BaseTreeEnum.threePair: return scores.threePair;
            case BaseTreeEnum.straight: return scores.straight;
            case BaseTreeEnum.fourCard: return scores.fourCard;
            case BaseTreeEnum.doubleTriple: return scores.doubleTriple;
            case BaseTreeEnum.grandFullHouse: return scores.grandFullHouse;
            case BaseTreeEnum.doubleStraight: return scores.doubleStraight;
            case BaseTreeEnum.fiveCard: return scores.fiveCard;
            case BaseTreeEnum.universe: return scores.universe;
            case BaseTreeEnum.hexa: return scores.hexa;
            case BaseTreeEnum.genesis: return scores.genesis;
            default: return 1; // �� �� ���� ���� �⺻ ���� 1
        }
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