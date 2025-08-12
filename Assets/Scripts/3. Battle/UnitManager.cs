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

    [PunRPC]
    private void SummonUnitsRPC(Player owner, int[] yeokTypes, int[] lineIndices, bool[] isHorizontals, string[] combinationStrings, int[] baseScores, int[] bonusScores)
    {
        // --- 1. ������ Ŭ���̾�Ʈ�� �̹� �Ͽ� �߻��� ��� ���ط��� �̸� ����մϴ�. ---
        if (PhotonNetwork.IsMasterClient)
        {
            int totalDamageToPlayer = 0;
            Player targetPlayer = null;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p != owner)
                {
                    targetPlayer = p;
                    break;
                }
            }

            if (targetPlayer != null)
            {
                // ���� ���尡 �ƴ�, ����� ���� ���� ���带 ����ϴ�.
                var simulatedBoard = new Dictionary<GameObject, UnitData>(unitDataOnBoard);

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

                    // �� ���� ��ȯ �ùķ��̼��� ���� �߻��� �������� ����ϰ� �����մϴ�.
                    totalDamageToPlayer += CalculatePlacementDamage(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[0]], simulatedBoard);
                    totalDamageToPlayer += CalculatePlacementDamage(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[1]], simulatedBoard);
                }

                // ����� ��� ���� ��, �ջ�� �� �������� �� �� ���� �����մϴ�.
                if (totalDamageToPlayer > 0)
                {
                    Debug.Log($"[���� ������ ����] ���: {targetPlayer.NickName}, �� ������: {totalDamageToPlayer}");
                    HealthManager.Instance.DealDamage(targetPlayer, totalDamageToPlayer);
                }
            }
        }

        // --- 2. ��� Ŭ���̾�Ʈ���� ���� ���� ��ġ �� ������ �����ϰ� �����մϴ�. ---
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
    /// ���� �ϳ��� ��ġ�� �� �߻��� �������� '���'�� �ϰ�, ���� ���带 ������Ʈ�մϴ�. (������ Ŭ���̾�Ʈ ����)
    /// </summary>
    private int CalculatePlacementDamage(UnitData attacker, GameObject slot, Dictionary<GameObject, UnitData> board)
    {
        int damageToPlayer = 0;
        if (board.TryGetValue(slot, out UnitData defender))
        {
            if (defender.Owner != attacker.Owner)
            {
                defender.HP -= attacker.InitialDamage;
                if (defender.HP <= 0)
                {
                    // a >= b �� ���
                    if (defender.HP < 0)
                    {
                        // a > b (������ �¸�), ����ų ������ ���
                        damageToPlayer = -defender.HP;
                        board[slot] = attacker; // ���� ���忡�� ���� ��ü
                    }
                    else // defender.HP == 0
                    {
                        // a = b (���º�), �� �� �ı�
                        board.Remove(slot); // ���� ���忡�� ��� ���� ����
                    }
                }
            }
        }
        else
        {
            damageToPlayer = attacker.InitialDamage;
            board[slot] = attacker;
        }
        return damageToPlayer;
    }

    /// <summary>
    /// ������ ���Կ� ���� ��ġ�� '�õ�'�մϴ�. (���� ������ ����)
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
        else
        {
            PlaceUnit(attackerData, targetSlot);
        }
    }

    /// <summary>
    /// ���� ���� ������ ó���ϰ� ��� Ŭ���̾�Ʈ���� ����� �����ݴϴ�.
    /// </summary>
    private void ResolveCombat(UnitData attacker, UnitData defender, GameObject defenderSlot)
    {
        defender.HP -= attacker.InitialDamage;

        if (defender.HP <= 0) // ��� ������ �ı��ǰų� ���º��� ���
        {
            // ��� ������ �׻� ���忡�� ���ŵ˴ϴ�.
            unitDataOnBoard.Remove(defenderSlot);

            if (defender.HP < 0) // 1. a > b (���� ���� �¸�)
            {
                // ���� ������ �� �ڸ��� �����մϴ�. (����ų �������� �����Ͱ� �̹� �������)
                PlaceUnit(attacker, defenderSlot);
            }
            else // 2. a = b (�� �� �ı�)
            {
                // ���� ������ ��ġ���� �ʰ�, ������ ����ݴϴ�.
                int slotIndex = System.Array.IndexOf(perimeterSlots, defenderSlot);
                if (slotIndex > -1)
                {
                    photonView.RPC("ClearSlotDisplayRPC", RpcTarget.All, slotIndex);
                }
            }
        }
        else // 3. a < b (��� ���� �¸�)
        {
            // ���� ������ ��ġ���� �ʰ� �Ҹ��մϴ�.
            Debug.Log($"����: ���� ����! ��� ������ ���� HP: {defender.HP}");
        }
    }

    private void PlaceUnit(UnitData unit, GameObject slot)
    {
        unitDataOnBoard[slot] = unit;
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];
        slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
    }

    public void ApplyContinuousDamage()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 1. �� �÷��̾ ���� �� �������� ����� ������ �غ��մϴ�.
        int totalDamageToP1 = 0; // ������ Ŭ���̾�Ʈ(P1)�� ���� ������
        int totalDamageToP2 = 0; // �ٸ� Ŭ���̾�Ʈ(P2)�� ���� ������

        Player player1 = PhotonNetwork.MasterClient;
        Player player2 = PhotonNetwork.PlayerListOthers.Length > 0 ? PhotonNetwork.PlayerListOthers[0] : null;

        if (player2 == null) return; // ��밡 ������ ���� ����

        // 2. ������ ��� ������ ��ȸ�ϸ� �������� '���'�� �մϴ�.
        foreach (var entry in unitDataOnBoard)
        {
            UnitData unit = entry.Value;
            if (unit.ContinuousDamage <= 0) continue;

            if (unit.Owner == player1)
            {
                // P1�� ������ P2���� �������� �ݴϴ�.
                totalDamageToP2 += unit.ContinuousDamage;
            }
            else if (unit.Owner == player2)
            {
                // P2�� ������ P1���� �������� �ݴϴ�.
                totalDamageToP1 += unit.ContinuousDamage;
            }
        }

        // 3. ����� ��� ���� ��, ���� ���� �������� '����'�մϴ�.
        if (totalDamageToP1 > 0)
        {
            Debug.Log($"[���� ������] {player1.NickName}���� {totalDamageToP1} ������ ����");
            HealthManager.Instance.DealDamage(player1, totalDamageToP1);
        }

        if (totalDamageToP2 > 0)
        {
            Debug.Log($"[���� ������] {player2.NickName}���� {totalDamageToP2} ������ ����");
            HealthManager.Instance.DealDamage(player2, totalDamageToP2);
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