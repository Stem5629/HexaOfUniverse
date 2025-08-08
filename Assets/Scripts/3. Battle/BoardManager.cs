using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI; // Image ������Ʈ ����� ���� �߰�

/// <summary>
/// 6x6 �׸��� ������ �����͸� �����ϰ�, '��'�� ��ĵ�ϸ�, �ֻ��� ��ġ/���Ÿ� ó���մϴ�.
/// FieldInitializer�� ���� �ʱ�ȭ�ǰ�, GameManager�� ���� ����˴ϴ�.
/// </summary>
public class BoardManager : MonoBehaviour
{
    // --- ������ ---
    private DiceData[,] gridData = new DiceData[6, 6]; // 6x6 �׸����� ���� ������
    private GameObject[,] gridTiles; // 6x6 �׸����� ���� Ÿ�� ������Ʈ ���� (FieldInitializer�� ����)

    // --- ���� Ŭ���� ---
    private BaseTree baseTreeEvaluator = new BaseTree();
    private AddTree addTreeEvaluator = new AddTree();
    private BaseTreeScore baseTreeScores = new BaseTreeScore();
    private AddTreeScore addTreeScores = new AddTreeScore();

    // --- �ܺ� ���� ---
    [HideInInspector] public PlayerHand currentTurnPlayerHand;
    [SerializeField] private Sprite[] diceSprites; // �ֻ��� �̹����� ǥ���ϱ� ���� ��������Ʈ �迭 (�ν����Ϳ��� �Ҵ�)
                                                   //[SerializeField] private Sprite emptyTileSprite; // �� Ÿ�� �̹��� (�ν����Ϳ��� �Ҵ�)


    // BoardManager.cs �� Initialize �Լ�

    public void Initialize(GameObject[,] tiles)
    {
        this.gridTiles = tiles;

        // Ÿ�� Ŭ�� �̺�Ʈ ���� ����
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                // --- �Ʒ� if�� �߰� ---
                // �� ĭ�� null�� �ƴ�, ���� Ÿ�� ������Ʈ�� ��쿡�� ������ �����մϴ�.
                if (gridTiles[x, y] != null)
                {
                    Tile tile = gridTiles[x, y].GetComponent<Tile>();
                    int coordX = x;
                    int coordY = y;
                    tile.button.onClick.AddListener(() => GameManager.Instance.OnGridTileClicked(coordX, coordY));
                }
                else
                {
                    // �� �αװ� �ֿܼ� �����ٸ�, �� ������ ������ �ִٴ� �ǹ��Դϴ�.
                    Debug.LogError($"BoardManager Error: gridTiles[{x},{y}]�� null�Դϴ�. �� ������ Ȯ���ϼ���.");
                }
            }
        }
    }

    /// <summary>
    /// �÷��̾ �׸����� Ÿ���� Ŭ������ �� ȣ��� �Լ�
    /// </summary>
    private void OnTileClicked(int x, int y)
    {
        // GameManager���� ��ǥ�� �����Ͽ�, �ֻ��� ��ġ �Ǵ� ȸ���� ��û
        GameManager.Instance.OnGridTileClicked(x, y);
    }


    /// <summary>
    /// �÷��̾��� ���п� �ִ� �ֻ����� �׸����� Ư�� ��ġ�� ��ġ�մϴ�.
    /// </summary>
    public void PlaceDiceOnGrid(int diceNumber, int x, int y)
    {
        if (gridData[x, y] != null)
        {
            Debug.Log("�̹� �ֻ����� ���� �ڸ��Դϴ�.");
            return;
        }

        if (currentTurnPlayerHand != null && currentTurnPlayerHand.HasDice(diceNumber))
        {
            currentTurnPlayerHand.UseDice(diceNumber);
            gridData[x, y] = new DiceData { DiceNumber = diceNumber };
            UpdateTileUI(x, y); // UI ������Ʈ
        }
        else
        {
            Debug.LogError("���� ���� PlayerHand�� ���ų�, �ش� �ֻ����� ������ ���� �ʽ��ϴ�.");
        }
    }

    /// <summary>
    /// �׸��忡 ��ġ�� �ֻ����� �ٽ� �÷��̾��� ���з� �ǵ����ϴ�.
    /// </summary>
    public void PickUpDiceFromGrid(int x, int y)
    {
        if (gridData[x, y] == null)
        {
            Debug.Log("�ֻ����� ���� �� �ڸ��Դϴ�.");
            return;
        }

        DiceData diceToReclaim = gridData[x, y];
        currentTurnPlayerHand.ReclaimDice(diceToReclaim.DiceNumber);
        gridData[x, y] = null;
        UpdateTileUI(x, y); // UI ������Ʈ
    }

    /// <summary>
    /// '��ȯ' �� GameManager�� ȣ��. �׸����� ��� ���� ��ĵ�Ͽ� �ϼ��� �� ����� ��ȯ�մϴ�.
    /// </summary>
    public List<CompletedYeokInfo> ScanAllLines()
    {
        List<CompletedYeokInfo> foundYeoks = new List<CompletedYeokInfo>();
        var yeokOrder = System.Enum.GetValues(typeof(BaseTreeEnum)).Cast<BaseTreeEnum>().Reverse().ToArray();

        // ������(Row) ��ĵ
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

        // ������(Column) ��ĵ
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
    /// ���� ��ȯ�� ���� �ֻ������� �׸��忡�� �����մϴ�.
    /// </summary>
    public void ClearUsedDice(List<CompletedYeokInfo> yeoksToClear)
    {
        foreach (var yeokInfo in yeoksToClear)
        {
            foreach (var pos in yeokInfo.DicePositions)
            {
                gridData[pos.x, pos.y] = null;
                UpdateTileUI(pos.x, pos.y); // UI ������Ʈ
            }
        }
    }

    /// <summary>
    /// Ư�� Ÿ���� UI�� ���� gridData ���¿� �°� ������Ʈ�մϴ�.
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

    #region ���� �Լ� (���� ����)
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