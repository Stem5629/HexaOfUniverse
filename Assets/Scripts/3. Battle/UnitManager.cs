using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// 24���� �ܰ� ������ �����ϰ�, ������ ��ȯ, ����, �ı��� ó���մϴ�.
/// GameManager�� ���� �ʱ�ȭ�ǰ� ����˴ϴ�.
/// </summary>
public class UnitManager : MonoBehaviourPunCallbacks
{
    [Header("UI �� �ð� ȿ�� ����")]
    [SerializeField] private Sprite[] unitSpritesByYeok;
    [SerializeField] private Color myColor = Color.red;
    [SerializeField] private Color versusColor = Color.blue;

    [Header("���� ����â UI")]
    public GameObject infoPanel;
    public TextMeshProUGUI unitTreeText;
    public TextMeshProUGUI treeCombinationText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI beginDamageText;
    public TextMeshProUGUI keepDamageText;

    // --- ������ ---
    private GameObject[] perimeterSlots;
    private Dictionary<GameObject, UnitData> unitDataOnBoard = new Dictionary<GameObject, UnitData>();

    /// <summary>
    /// GameManager�� ȣ���Ͽ� ���� ������ �����ϰ� Ŭ�� �����ϰ� ����� �ʱ�ȭ �Լ�
    /// </summary>
    public void Initialize(GameObject[] slots)
    {
        this.perimeterSlots = slots;
        unitDataOnBoard.Clear();
        if (infoPanel != null) infoPanel.SetActive(false);

        foreach (var slot in perimeterSlots)
        {
            Button button = slot.GetComponent<Button>();
            if (button == null) button = slot.AddComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnUnitSlotClicked(slot));
        }
    }

    /// <summary>
    /// GameManager�� ȣ��. ��� Ŭ���̾�Ʈ���� ������ ��ȯ�϶�� RPC ����� �����ϴ�.
    /// </summary>
    public void SummonUnitsViaRPC(List<CompletedYeokInfo> yeoks, Player owner)
    {
        int[] yeokTypes = new int[yeoks.Count];
        int[] lineIndices = new int[yeoks.Count];
        bool[] isHorizontals = new bool[yeoks.Count];
        string[] combinationStrings = new string[yeoks.Count];
        int[] baseScores = new int[yeoks.Count];
        int[] bonusScores = new int[yeoks.Count];

        for (int i = 0; i < yeoks.Count; i++)
        {
            yeokTypes[i] = (int)yeoks[i].YeokType;
            lineIndices[i] = yeoks[i].LineIndex;
            isHorizontals[i] = yeoks[i].IsHorizontal;
            combinationStrings[i] = yeoks[i].CombinationString;
            baseScores[i] = yeoks[i].BaseScore;
            bonusScores[i] = yeoks[i].BonusScore;
        }

        photonView.RPC("SummonUnitsRPC", RpcTarget.All, owner, yeokTypes, lineIndices, isHorizontals, combinationStrings, baseScores, bonusScores);
    }

    /// <summary>
    /// [RPC] ��� Ŭ���̾�Ʈ���� ����Ǿ� ������ ������ ��ȯ�ϴ� �Լ�
    /// </summary>
    [PunRPC]
    private void SummonUnitsRPC(Player owner, int[] yeokTypes, int[] lineIndices, bool[] isHorizontals, string[] combinationStrings, int[] baseScores, int[] bonusScores)
    {
        for (int i = 0; i < yeokTypes.Length; i++)
        {
            CompletedYeokInfo yeokInfo = new CompletedYeokInfo
            {
                YeokType = (BaseTreeEnum)yeokTypes[i],
                LineIndex = lineIndices[i],
                IsHorizontal = isHorizontals[i],
                CombinationString = combinationStrings[i],
                BaseScore = baseScores[i],
                BonusScore = bonusScores[i]
            };

            int[] slotIndices = GetPerimeterIndices(yeokInfo);

            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[0]]);
            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[1]]);
        }
    }

    /// <summary>
    /// ������ ���Կ� ���� ��ġ�� �õ��մϴ�. (�� �����̸� ������, �� ������ ������ ����)
    /// </summary>
    private void AttemptToPlaceUnit(UnitData attackerData, GameObject targetSlot)
    {
        if (unitDataOnBoard.TryGetValue(targetSlot, out UnitData defenderData))
        {
            if (defenderData.Owner != attackerData.Owner)
            {
                ResolveCombat(attackerData, defenderData, targetSlot);
            }
        }
        else // ������ ����ִ� ���
        {
            // ������ Ŭ���̾�Ʈ�� ������ ��� �� ������ ����մϴ�.
            if (PhotonNetwork.IsMasterClient)
            {
                // ���� ������ ������ �������� ����(Ÿ��)�� ã���ϴ�.
                Player targetPlayer = null;
                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    if (p != attackerData.Owner)
                    {
                        targetPlayer = p;
                        break;
                    }
                }

                // Ÿ���� �����ϸ�, ���� ������ '�ʱ� ������'�� �����ϴ�.
                if (targetPlayer != null)
                {
                    HealthManager.Instance.DealDamage(targetPlayer, attackerData.InitialDamage);
                }
            }

            // ��� Ŭ���̾�Ʈ���� ������ ��ġ�մϴ�.
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
            if (overkillDamage > 0 && PhotonNetwork.IsMasterClient)
            {
                HealthManager.Instance.DealDamage(defender.Owner, overkillDamage);
            }
            PlaceUnit(attacker, defenderSlot);
        }
    }

    private void PlaceUnit(UnitData unit, GameObject slot)
    {
        unitDataOnBoard[slot] = unit;

        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];
        slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
    }

    /// <summary>
    /// GameManager�� ���� ���� �� ȣ��. ��� ������ ���� �������� ó���մϴ�.
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

    private void OnUnitSlotClicked(GameObject clickedSlot)
    {
        if (unitDataOnBoard.TryGetValue(clickedSlot, out UnitData unitData))
        {
            infoPanel.SetActive(true);

            unitTreeText.text = unitData.YeokType.ToString();
            treeCombinationText.text = unitData.CombinationString;
            hpText.text = $"HP : {unitData.HP}";
            beginDamageText.text = $"�ʱⵥ���� : {unitData.InitialDamage}";
            keepDamageText.text = $"���ӵ����� : {unitData.ContinuousDamage}";
        }
        else
        {
            infoPanel.SetActive(false);
        }
    }

    private UnitData CreateUnitDataFromYeok(CompletedYeokInfo yeok, Player owner)
    {
        UnitData unit = new UnitData { Owner = owner, YeokType = yeok.YeokType };

        int baseStat = yeok.BaseScore;
        int continuousStat = yeok.BonusScore;

        unit.HP = baseStat;
        unit.InitialDamage = baseStat;
        unit.ContinuousDamage = continuousStat;

        unit.CombinationString = yeok.CombinationString;
        return unit;
    }

    private int[] GetPerimeterIndices(CompletedYeokInfo yeokInfo)
    {
        int[] indices = new int[2];
        if (yeokInfo.IsHorizontal)
        {
            indices[0] = 12 + yeokInfo.LineIndex;
            indices[1] = 18 + yeokInfo.LineIndex;
        }
        else
        {
            indices[0] = yeokInfo.LineIndex;
            indices[1] = 6 + yeokInfo.LineIndex;
        }
        return indices;
    }
}