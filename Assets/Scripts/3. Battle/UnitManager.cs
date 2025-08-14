using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// 24개의 외곽 전장을 관리하고, 유닛의 소환, 전투, 파괴를 처리합니다.
/// GameManager에 의해 초기화되고 제어됩니다.
/// </summary>
public class UnitManager : MonoBehaviourPunCallbacks
{
    [Header("UI 및 시각 효과 참조")]
    [SerializeField] private Sprite[] unitSpritesByYeok;
    [SerializeField] private Color myColor = Color.red;
    [SerializeField] private Color versusColor = Color.blue;

    [Header("유닛 정보창 UI")]
    public GameObject infoPanel;
    public TextMeshProUGUI unitTreeText;
    public TextMeshProUGUI treeCombinationText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI beginDamageText;
    public TextMeshProUGUI keepDamageText;

    // --- 데이터 ---
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

        // 만약 온라인 상태(InRoom)라면, 모든 플레이어에게 RPC를 보냅니다.
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            photonView.RPC("SummonUnitsRPC", Photon.Pun.RpcTarget.All, owner, yeokTypes, lineIndices, isHorizontals, combinationStrings, baseScores, bonusScores);
        }
        // 아니라면 (오프라인, 즉 튜토리얼 상태라면),
        else
        {
            // 네트워크 통신 없이 내 컴퓨터에서만 함수를 직접 실행합니다.
            SummonUnitsRPC(owner, yeokTypes, lineIndices, isHorizontals, combinationStrings, baseScores, bonusScores);
        }
    }

    [PunRPC]
    private void SummonUnitsRPC(Player owner, int[] yeokTypes, int[] lineIndices, bool[] isHorizontals, string[] combinationStrings, int[] baseScores, int[] bonusScores)
    {
        // --- 1. 마스터 클라이언트만 이번 턴에 발생할 모든 피해량을 미리 계산합니다. ---
        // owner가 null이 아닌, 진짜 온라인 대전일 때만 이 블록을 실행하도록 조건을 추가합니다.
        if (PhotonNetwork.IsMasterClient && owner != null)
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
                var simulatedBoard = new Dictionary<GameObject, UnitData>(unitDataOnBoard);


                for (int i = 0; i < yeokTypes.Length; i++)
                {
                    // 매개변수로 받은 올바른 데이터를 사용하여 yeokInfo를 채웁니다.
                    CompletedYeokInfo yeokInfo = new CompletedYeokInfo
                    {
                        YeokType = (BaseTreeEnum)yeokTypes[i],
                        LineIndex = lineIndices[i],
                        IsHorizontal = isHorizontals[i],
                        CombinationString = combinationStrings[i],
                        BaseScore = baseScores[i],
                        BonusScore = bonusScores[i]
                    };

                    // 이제 올바른 공격력을 가진 유닛으로 피해량을 계산합니다.
                    totalDamageToPlayer += CalculatePlacementDamage(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[GetPerimeterIndices(yeokInfo)[0]], simulatedBoard);
                    totalDamageToPlayer += CalculatePlacementDamage(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[GetPerimeterIndices(yeokInfo)[1]], simulatedBoard);
                }

                if (totalDamageToPlayer > 0)
                {
                    Debug.Log($"[최종 데미지 적용] 대상: {targetPlayer.NickName}, 총 데미지: {totalDamageToPlayer}");
                    HealthManager.Instance.DealDamage(targetPlayer, totalDamageToPlayer);
                }
            }
        }

        // --- 2. 모든 클라이언트에서 실제 유닛 배치 및 전투를 동일하게 실행합니다. ---
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
    /// 유닛 하나가 배치될 때 발생할 데미지를 '계산'만 하고, 가상 보드를 업데이트합니다. (마스터 클라이언트 전용)
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
                    // a >= b 인 경우
                    if (defender.HP < 0)
                    {
                        // a > b (공격자 승리), 오버킬 데미지 계산
                        damageToPlayer = -defender.HP;
                        board[slot] = attacker; // 가상 보드에서 유닛 교체
                    }
                    else // defender.HP == 0
                    {
                        // a = b (무승부), 둘 다 파괴
                        board.Remove(slot); // 가상 보드에서 방어 유닛 제거
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
    /// 지정된 슬롯에 유닛 배치를 '시도'합니다. (실제 데이터 변경)
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
    /// 실제 유닛 전투를 처리하고 모든 클라이언트에게 결과를 보여줍니다.
    /// </summary>
    private void ResolveCombat(UnitData attacker, UnitData defender, GameObject defenderSlot)
    {
        defender.HP -= attacker.InitialDamage;

        if (defender.HP <= 0) // 방어 유닛이 파괴되거나 무승부인 경우
        {
            // 방어 유닛은 항상 보드에서 제거됩니다.
            unitDataOnBoard.Remove(defenderSlot);

            if (defender.HP < 0) // 1. a > b (공격 유닛 승리)
            {
                // 공격 유닛이 빈 자리를 차지합니다. (오버킬 데미지는 마스터가 이미 계산했음)
                PlaceUnit(attacker, defenderSlot);
            }
            else // 2. a = b (둘 다 파괴)
            {
                // 공격 유닛을 배치하지 않고, 슬롯을 비워줍니다.
                int slotIndex = System.Array.IndexOf(perimeterSlots, defenderSlot);
                if (slotIndex > -1)
                {
                    photonView.RPC("ClearSlotDisplayRPC", RpcTarget.All, slotIndex);
                }
            }
        }
        else // 3. a < b (방어 유닛 승리)
        {
            // 공격 유닛은 배치되지 않고 소멸합니다.
            Debug.Log($"전투: 공격 실패! 방어 유닛의 남은 HP: {defender.HP}");
        }
    }

    private void PlaceUnit(UnitData unit, GameObject slot)
    {
        unitDataOnBoard[slot] = unit;
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];

        // 유닛의 주인이 있는지 먼저 확인합니다.
        if (unit.Owner != null)
        {
            // 온라인 대전처럼 주인이 있다면, 기존 로직대로 색상을 정합니다.
            slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
        }
        else
        {
            // 주인이 없다면 (튜토리얼), 내 유닛 색상(myColor)으로 기본 설정합니다.
            slotImage.color = myColor;
        }
    }

    public void ApplyContinuousDamage()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 1. 각 플레이어가 입을 총 데미지를 계산할 변수를 준비합니다.
        int totalDamageToP1 = 0; // 마스터 클라이언트(P1)가 입을 데미지
        int totalDamageToP2 = 0; // 다른 클라이언트(P2)가 입을 데미지

        Player player1 = PhotonNetwork.MasterClient;
        Player player2 = PhotonNetwork.PlayerListOthers.Length > 0 ? PhotonNetwork.PlayerListOthers[0] : null;

        if (player2 == null) return; // 상대가 없으면 실행 중지

        // 2. 보드의 모든 유닛을 순회하며 데미지를 '계산'만 합니다.
        foreach (var entry in unitDataOnBoard)
        {
            UnitData unit = entry.Value;
            if (unit.ContinuousDamage <= 0) continue;

            if (unit.Owner == player1)
            {
                // P1의 유닛은 P2에게 데미지를 줍니다.
                totalDamageToP2 += unit.ContinuousDamage;
            }
            else if (unit.Owner == player2)
            {
                // P2의 유닛은 P1에게 데미지를 줍니다.
                totalDamageToP1 += unit.ContinuousDamage;
            }
        }

        // 3. 계산이 모두 끝난 후, 계산된 총합 데미지를 '적용'합니다.
        if (totalDamageToP1 > 0)
        {
            Debug.Log($"[지속 데미지] {player1.NickName}에게 {totalDamageToP1} 데미지 적용");
            HealthManager.Instance.DealDamage(player1, totalDamageToP1);
        }

        if (totalDamageToP2 > 0)
        {
            Debug.Log($"[지속 데미지] {player2.NickName}에게 {totalDamageToP2} 데미지 적용");
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
            beginDamageText.text = $"초기데미지 : {unitData.InitialDamage}";
            keepDamageText.text = $"지속데미지 : {unitData.ContinuousDamage}";
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

    public void PlaceTutorialEnemyUnit(int slotIndex, BaseTreeEnum yeokType)
    {
        GameObject targetSlot = perimeterSlots[slotIndex];
        if (targetSlot == null) return;

        // 적 유닛 데이터 생성 (주인은 null, 상대방 색상으로 표시)
        UnitData enemyUnit = new UnitData
        {
            Owner = null, // 주인 없음
            YeokType = yeokType,
            HP = 1, // 테스트용으로 HP를 1로 설정
            InitialDamage = 1,
            ContinuousDamage = 0,
            CombinationString = "TUTORIAL"
        };

        unitDataOnBoard[targetSlot] = enemyUnit;
        Image slotImage = targetSlot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)yeokType];
        slotImage.color = versusColor; // 상대방 색상으로 표시
    }
}