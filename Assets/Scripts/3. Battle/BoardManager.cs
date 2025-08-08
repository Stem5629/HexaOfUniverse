using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI; // Image 컴포넌트 사용을 위해 추가

/// <summary>
/// 6x6 그리드 보드의 데이터를 관리하고, '역'을 스캔하며, 주사위 배치/제거를 처리합니다.
/// FieldInitializer에 의해 초기화되고, GameManager에 의해 제어됩니다.
/// </summary>
public class BoardManager : MonoBehaviour
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
    [SerializeField] private Sprite[] diceSprites; // 주사위 이미지를 표시하기 위한 스프라이트 배열 (인스펙터에서 할당)
                                                   //[SerializeField] private Sprite emptyTileSprite; // 빈 타일 이미지 (인스펙터에서 할당)


    // BoardManager.cs 의 Initialize 함수

    public void Initialize(GameObject[,] tiles)
    {
        this.gridTiles = tiles;

        // 타일 클릭 이벤트 연결 로직
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                // --- 아래 if문 추가 ---
                // 이 칸이 null이 아닌, 실제 타일 오브젝트일 경우에만 로직을 실행합니다.
                if (gridTiles[x, y] != null)
                {
                    Tile tile = gridTiles[x, y].GetComponent<Tile>();
                    int coordX = x;
                    int coordY = y;
                    tile.button.onClick.AddListener(() => GameManager.Instance.OnGridTileClicked(coordX, coordY));
                }
                else
                {
                    // 이 로그가 콘솔에 찍힌다면, 씬 설정에 문제가 있다는 의미입니다.
                    Debug.LogError($"BoardManager Error: gridTiles[{x},{y}]가 null입니다. 씬 설정을 확인하세요.");
                }
            }
        }
    }

    /// <summary>
    /// 플레이어가 그리드의 타일을 클릭했을 때 호출될 함수
    /// </summary>
    private void OnTileClicked(int x, int y)
    {
        // GameManager에게 좌표를 전달하여, 주사위 배치 또는 회수를 요청
        GameManager.Instance.OnGridTileClicked(x, y);
    }


    /// <summary>
    /// 플레이어의 손패에 있는 주사위를 그리드의 특정 위치에 배치합니다.
    /// </summary>
    public void PlaceDiceOnGrid(int diceNumber, int x, int y)
    {
        if (gridData[x, y] != null)
        {
            Debug.Log("이미 주사위가 놓인 자리입니다.");
            return;
        }

        if (currentTurnPlayerHand != null && currentTurnPlayerHand.HasDice(diceNumber))
        {
            currentTurnPlayerHand.UseDice(diceNumber);
            gridData[x, y] = new DiceData { DiceNumber = diceNumber };
            UpdateTileUI(x, y); // UI 업데이트
        }
        else
        {
            Debug.LogError("현재 턴의 PlayerHand가 없거나, 해당 주사위를 가지고 있지 않습니다.");
        }
    }

    /// <summary>
    /// 그리드에 배치된 주사위를 다시 플레이어의 손패로 되돌립니다.
    /// </summary>
    public void PickUpDiceFromGrid(int x, int y)
    {
        if (gridData[x, y] == null)
        {
            Debug.Log("주사위가 없는 빈 자리입니다.");
            return;
        }

        DiceData diceToReclaim = gridData[x, y];
        currentTurnPlayerHand.ReclaimDice(diceToReclaim.DiceNumber);
        gridData[x, y] = null;
        UpdateTileUI(x, y); // UI 업데이트
    }

    /// <summary>
    /// '소환' 시 GameManager가 호출. 그리드의 모든 줄을 스캔하여 완성된 역 목록을 반환합니다.
    /// </summary>
    public List<CompletedYeokInfo> ScanAllLines()
    {
        List<CompletedYeokInfo> foundYeoks = new List<CompletedYeokInfo>();
        var yeokOrder = System.Enum.GetValues(typeof(BaseTreeEnum)).Cast<BaseTreeEnum>().Reverse().ToArray();

        // 가로줄(Row) 스캔
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

                    foundYeoks.Add(new CompletedYeokInfo
                    {
                        YeokType = yeok,
                        TotalScore = baseScore + bonusScore,
                        LineIndex = y,
                        IsHorizontal = true,
                        DicePositions = dicePositionsInLine
                    });
                    break;
                }
            }
        }

        // 세로줄(Column) 스캔
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

                    foundYeoks.Add(new CompletedYeokInfo
                    {
                        YeokType = yeok,
                        TotalScore = baseScore + bonusScore,
                        LineIndex = x,
                        IsHorizontal = false,
                        DicePositions = dicePositionsInLine
                    });
                    break;
                }
            }
        }

        return foundYeoks;
    }

    /// <summary>
    /// 유닛 소환에 사용된 주사위들을 그리드에서 제거합니다.
    /// </summary>
    public void ClearUsedDice(List<CompletedYeokInfo> yeoksToClear)
    {
        foreach (var yeokInfo in yeoksToClear)
        {
            foreach (var pos in yeokInfo.DicePositions)
            {
                gridData[pos.x, pos.y] = null;
                UpdateTileUI(pos.x, pos.y); // UI 업데이트
            }
        }
    }

    /// <summary>
    /// 특정 타일의 UI를 현재 gridData 상태에 맞게 업데이트합니다.
    /// </summary>
    private void UpdateTileUI(int x, int y)
    {
        Image tileImage = gridTiles[x, y].GetComponent<Image>();
        if (gridData[x, y] != null)
        {
            tileImage.sprite = diceSprites[gridData[x, y].DiceNumber];
        }
        else
        {
            //tileImage.sprite = emptyTileSprite;
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