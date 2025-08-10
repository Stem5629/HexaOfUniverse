using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 6x6 그리드 보드의 데이터를 관리하고, '역'을 스캔하며, 주사위 배치/제거를 처리합니다.
/// FieldInitializer에 의해 초기화되고, GameManager에 의해 제어됩니다.
/// </summary>
public class BoardManager : MonoBehaviourPunCallbacks
{
    // --- 데이터 ---
    private DiceData[,] gridData = new DiceData[6, 6]; // 6x6 그리드의 논리적 데이터
    private GameObject[,] gridTiles; // 6x6 그리드의 실제 타일 오브젝트 참조 (FieldInitializer가 설정)

    // --- 헬퍼 클래스 ---
    private BaseTree baseTreeEvaluator = new BaseTree();
    private AddTree addTreeEvaluator = new AddTree();
    private BaseTreeScore baseTreeScores = new BaseTreeScore();
    private AddTreeScore addTreeScores = new AddTreeScore();

    // --- 외부 참조 ---
    [HideInInspector] public PlayerHand currentTurnPlayerHand;
    [SerializeField] private Sprite[] diceSprites;
    [SerializeField] private Sprite emptyTileSprite;

    public void Initialize(GameObject[,] tiles)
    {
        this.gridTiles = tiles;
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                if (gridTiles[x, y] != null)
                {
                    Tile tile = gridTiles[x, y].GetComponent<Tile>();
                    int coordX = x;
                    int coordY = y;
                    tile.button.onClick.RemoveAllListeners();
                    tile.button.onClick.AddListener(() => GameManager.Instance.OnGridTileClicked(coordX, coordY));
                }
            }
        }
    }

    /// <summary>
    /// GameManager가 호출할 전체 보드 초기화 명령 함수
    /// </summary>
    public void ClearAllDiceViaRPC()
    {
        // 모든 클라이언트에게 보드를 비우라고 명령합니다.
        photonView.RPC("ClearAllDiceRPC", RpcTarget.All);
    }

    [PunRPC]
    private void ClearAllDiceRPC()
    {
        // 6x6 그리드를 모두 순회합니다.
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                // 칸에 주사위가 있다면
                if (gridData[x, y] != null)
                {
                    // 데이터와 UI를 모두 초기화합니다.
                    gridData[x, y] = null;
                    UpdateTileUI(x, y);
                }
            }
        }
    }

    public void PlaceDiceOnGrid(int diceNumber, int x, int y, Player placingPlayer)
    {
        if (gridData[x, y] != null) return;

        // 주사위를 놓은 플레이어가 "나"일 경우에만 내 손에서 주사위를 제거합니다.
        if (placingPlayer.IsLocal)
        {
            if (currentTurnPlayerHand != null && currentTurnPlayerHand.HasDice(diceNumber))
            {
                currentTurnPlayerHand.UseDice(diceNumber);
            }
            else return; // 손에 없는 주사위라면 무시
        }

        gridData[x, y] = new DiceData { DiceNumber = diceNumber };
        UpdateTileUI(x, y);
    }

    public void PickUpDiceFromGrid(int x, int y)
    {
        if (gridData[x, y] == null) return;
        DiceData diceToReclaim = gridData[x, y];

        // 주사위를 회수하는 턴의 플레이어가 "나"일 경우에만 내 손으로 주사위를 가져옵니다.
        if (GameManager.Instance.GetCurrentPlayer().IsLocal)
        {
            currentTurnPlayerHand.ReclaimDice(diceToReclaim.DiceNumber);
        }

        gridData[x, y] = null;
        UpdateTileUI(x, y);
    }

    /// <summary>
    /// '소환' 시 GameManager가 호출. 그리드의 모든 줄을 스캔하여 완성된 역 목록을 반환합니다.
    /// </summary>
    public List<CompletedYeokInfo> ScanAllLines()
    {
        List<CompletedYeokInfo> foundYeoks = new List<CompletedYeokInfo>();
        // 높은 등급의 역부터 판별하기 위해 역순으로 정렬합니다.
        var yeokOrder = System.Enum.GetValues(typeof(BaseTreeEnum)).Cast<BaseTreeEnum>().Reverse().ToArray();

        // --- 가로줄(Row) 스캔 ---
        for (int y = 0; y < 6; y++)
        {
            List<int> diceNumbersInLine = new List<int>();
            List<Vector2Int> dicePositionsInLine = new List<Vector2Int>();

            for (int x = 0; x < 6; x++)
            {
                if (gridData[x, y] != null)
                {
                    diceNumbersInLine.Add(gridData[x, y].DiceNumber);
                    dicePositionsInLine.Add(new Vector2Int(x, y));
                }
            }

            if (diceNumbersInLine.Count < 2) continue;

            foreach (var yeok in yeokOrder)
            {
                if (IsYeokMatch(yeok, diceNumbersInLine.ToArray()))
                {
                    int baseScore = GetBaseScore(yeok);
                    int bonusScore = (diceNumbersInLine.Count >= 4) ? CalculateBonusScore(diceNumbersInLine.ToArray()) : 0;

                    diceNumbersInLine.Sort();
                    string comboStr = string.Join("-", diceNumbersInLine);

                    foundYeoks.Add(new CompletedYeokInfo
                    {
                        YeokType = yeok,
                        BaseScore = baseScore,
                        BonusScore = bonusScore,
                        LineIndex = y,
                        IsHorizontal = true,
                        DicePositions = dicePositionsInLine,
                        CombinationString = comboStr
                    });
                    break;
                }
            }
        }

        // --- 세로줄(Column) 스캔 ---
        for (int x = 0; x < 6; x++)
        {
            List<int> diceNumbersInLine = new List<int>();
            List<Vector2Int> dicePositionsInLine = new List<Vector2Int>();

            for (int y = 0; y < 6; y++)
            {
                if (gridData[x, y] != null)
                {
                    diceNumbersInLine.Add(gridData[x, y].DiceNumber);
                    dicePositionsInLine.Add(new Vector2Int(x, y));
                }
            }

            if (diceNumbersInLine.Count < 2) continue;

            foreach (var yeok in yeokOrder)
            {
                if (IsYeokMatch(yeok, diceNumbersInLine.ToArray()))
                {
                    int baseScore = GetBaseScore(yeok);
                    int bonusScore = (diceNumbersInLine.Count >= 4) ? CalculateBonusScore(diceNumbersInLine.ToArray()) : 0;

                    diceNumbersInLine.Sort();
                    string comboStr = string.Join("-", diceNumbersInLine);

                    foundYeoks.Add(new CompletedYeokInfo
                    {
                        YeokType = yeok,
                        BaseScore = baseScore,
                        BonusScore = bonusScore,
                        LineIndex = x,
                        IsHorizontal = false, // <-- 버그 수정된 부분
                        DicePositions = dicePositionsInLine,
                        CombinationString = comboStr
                    });
                    break;
                }
            }
        }
        return foundYeoks;
    }

    private void UpdateTileUI(int x, int y)
    {
        Image tileImage = gridTiles[x, y].GetComponent<Image>();
        if (gridData[x, y] != null)
        {
            tileImage.sprite = diceSprites[gridData[x, y].DiceNumber];
        }
        else
        {
            tileImage.sprite = emptyTileSprite;
        }
    }

    #region 헬퍼 함수 (판정 로직)
    private int CalculateBonusScore(int[] combo)
    {
        int score = 0;
        if (addTreeEvaluator.AllNumber(combo)) score += addTreeScores.allNumber;
        if (addTreeEvaluator.AllSymbol(combo)) score += addTreeScores.allSymbol;
        if (addTreeEvaluator.Pure(combo)) score += addTreeScores.pure;
        if (addTreeEvaluator.Mix(combo)) score += addTreeScores.mix;
        if (addTreeEvaluator.IncludeStar(combo)) score += addTreeScores.includeStar;
        if (addTreeEvaluator.IncludeMoon(combo)) score += addTreeScores.includeMoon;
        if (addTreeEvaluator.IncludeSun(combo)) score += addTreeScores.includeSun;
        if (addTreeEvaluator.FullDice(combo)) score += addTreeScores.fullDice;
        return score;
    }

    private bool IsYeokMatch(BaseTreeEnum yeok, int[] combo)
    {
        switch (yeok)
        {
            case BaseTreeEnum.pair: return baseTreeEvaluator.Pair(combo);
            case BaseTreeEnum.straight: return baseTreeEvaluator.Straight(combo);
            case BaseTreeEnum.twoPair: return baseTreeEvaluator.TwoPair(combo);
            case BaseTreeEnum.triple: return baseTreeEvaluator.Triple(combo);
            case BaseTreeEnum.threePair: return baseTreeEvaluator.ThreePair(combo);
            case BaseTreeEnum.fullHouse: return baseTreeEvaluator.FullHouse(combo);
            case BaseTreeEnum.fourCard: return baseTreeEvaluator.FourCard(combo);
            case BaseTreeEnum.doubleTriple: return baseTreeEvaluator.DoubleTriple(combo);
            case BaseTreeEnum.grandFullHouse: return baseTreeEvaluator.GrandFullHouse(combo);
            case BaseTreeEnum.fiveCard: return baseTreeEvaluator.FiveCard(combo);
            case BaseTreeEnum.doubleStraight: return baseTreeEvaluator.DoubleStraight(combo);
            case BaseTreeEnum.genesis: return baseTreeEvaluator.Genesis(combo);
            case BaseTreeEnum.universe: return baseTreeEvaluator.Universe(combo);
            case BaseTreeEnum.hexa: return baseTreeEvaluator.Hexa(combo);
            default: return false;
        }
    }

    private int GetBaseScore(BaseTreeEnum yeok)
    {
        switch (yeok)
        {
            case BaseTreeEnum.pair: return baseTreeScores.pair;
            case BaseTreeEnum.straight: return baseTreeScores.straight;
            case BaseTreeEnum.twoPair: return baseTreeScores.twoPair;
            case BaseTreeEnum.triple: return baseTreeScores.triple;
            case BaseTreeEnum.threePair: return baseTreeScores.threePair;
            case BaseTreeEnum.fullHouse: return baseTreeScores.fullHouse;
            case BaseTreeEnum.fourCard: return baseTreeScores.fourCard;
            case BaseTreeEnum.doubleTriple: return baseTreeScores.doubleTriple;
            case BaseTreeEnum.grandFullHouse: return baseTreeScores.grandFullHouse;
            case BaseTreeEnum.fiveCard: return baseTreeScores.fiveCard;
            case BaseTreeEnum.doubleStraight: return baseTreeScores.doubleStraight;
            case BaseTreeEnum.genesis: return baseTreeScores.genesis;
            case BaseTreeEnum.universe: return baseTreeScores.universe;
            case BaseTreeEnum.hexa: return baseTreeScores.hexa;
            default: return 0;
        }
    }
    #endregion
}