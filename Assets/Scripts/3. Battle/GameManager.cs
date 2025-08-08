using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// BattleScene�� ��ü ���� ���¿� �� �帧, �÷��̾� �Է��� �����ϴ� �Ѱ��� ��ũ��Ʈ�Դϴ�.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    // --- ���� ���� ---
    public enum GameState { WaitingForPlayers, Draft, Player1_Turn, Player2_Turn, ResolvingCombat, GameOver }
    [SerializeField] private GameState currentState;

    [Header("�ٽ� �Ŵ��� �� UI ����")]
    [SerializeField] private BoardManager boardManager;
    //[SerializeField] private UnitManager unitManager;
    private UnitManager unitManager;
    [SerializeField] private GameObject unitManagerPrefab;
    [SerializeField] private DraftManager draftManager;
    [SerializeField] private PlayerHand myHand;
    [SerializeField] private Transform tileContainer; // GridLayoutGroup�� �ִ� �θ� ������Ʈ

    [Header("UI ����")]
    [SerializeField] private Button summonButton;
    [SerializeField] private TextMeshProUGUI turnIndicatorText;

    // --- �÷��̾� ���� ---
    private Player player1; // MasterClient
    private Player player2; // Other
    private Player currentPlayer;
    private int currentRound = 1;

    // --- �÷��̾� �Է� ���� ---
    private int? selectedDiceNumber = null; // ���� ���п��� ������ �ֻ��� ��

    // �̱���
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- �Ʒ� ������ġ �߰� ---
        if (tileContainer == null)
        {
            Debug.LogError("ġ���� ����: GameManager�� �ν����Ϳ� 'Tile Container'�� ������� �ʾҽ��ϴ�!", this.gameObject);
            return; // �ʱ�ȭ�� �ߴ��Ͽ� �߰� ���� ����
        }

        InitializeField(); // ���� tileContainer�� null�� �ƴ��� �����
    }

    void Start()
    {
        summonButton.gameObject.SetActive(false);
        turnIndicatorText.text = "������ ��ٸ��� ��...";
        summonButton.onClick.AddListener(OnSummonButtonClicked);
        TrySetupPlayers();
    }

    #region �ʱ�ȭ �� ���� ����

    /// <summary>
    /// ���� ���� �� Ÿ�ϵ��� �ڵ����� ã�� �� �Ŵ������� ������ �ο��մϴ�.
    /// </summary>
    private void InitializeField()
    {
        Tile[] allTiles = tileContainer.GetComponentsInChildren<Tile>();
        List<Tile> sortedTiles = allTiles
            .OrderByDescending(t => t.transform.position.y)
            .ThenBy(t => t.transform.position.x)
            .ToList();

        GameObject[,] gridTiles = new GameObject[6, 6];
        GameObject[] perimeterSlots = new GameObject[24];

        for (int i = 0; i < sortedTiles.Count; i++)
        {
            Tile tile = sortedTiles[i];
            int x = i % 8;
            int y = 7 - (i / 8);
            tile.coordinates = new Vector2Int(x, y);

            if (x >= 1 && x <= 6 && y >= 1 && y <= 6)
            {
                gridTiles[x - 1, y - 1] = tile.gameObject;
            }
            else if (x == 0 || x == 7 || y == 0 || y == 7)
            {
                if ((x == 0 || x == 7) && (y == 0 || y == 7)) continue;
                int index = GetPerimeterIndexFromCoords(x, y);
                if (index != -1) perimeterSlots[index] = tile.gameObject;
            }
        }

        // --- �Ʒ� ���� ���� �߰� ---
        // �� �Ŵ������� �迭�� �����ϱ� ����, �迭�� �Ϻ��ϰ� ä�������� �˻��մϴ�.
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                if (gridTiles[x, y] == null)
                {
                    // � ��ǥ�� Ÿ���� ����ִ��� ��Ȯ�ϰ� �˷��ݴϴ�.
                    Debug.LogError($"[FieldInitializer Error] ���� �׸��� Ÿ�� (x:{x}, y:{y})�� �������� �ʾҽ��ϴ�! ���� Ÿ�� ������ ��ġ�� Ȯ���ϼ���.");
                    return; // ������ ������ �ʱ�ȭ�� �ߴ�
                }
            }
        }

        boardManager.Initialize(gridTiles);
        unitManager.Initialize(perimeterSlots);
        Debug.Log("�ʵ� �ڵ�ȭ ���� �Ϸ�!");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySetupPlayers();
    }

    private void TrySetupPlayers()
    {
        if (PhotonNetwork.PlayerList.Length < 2) return;
        player1 = PhotonNetwork.MasterClient;
        player2 = PhotonNetwork.PlayerListOthers[0];

        // --- ������ �κ� 2: ������ Ŭ���̾�Ʈ�� UnitManager�� ��Ʈ��ũ�� ���� ---
        if (PhotonNetwork.IsMasterClient)
        {
            // "UnitManager"�� Resources ������ �ִ� �������� �̸��� ��Ȯ�� ��ġ�ؾ� �մϴ�.
            PhotonNetwork.Instantiate("UnitManager", Vector3.zero, Quaternion.identity);
        }

        ChangeState(GameState.Draft);
    }

    // ���� UnitManager�� �����Ǹ� �� �Լ��� ȣ���Ͽ� ������ ����
    public void SetUnitManager(UnitManager manager)
    {
        this.unitManager = manager;
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case GameState.Draft:
                turnIndicatorText.text = "�巡��Ʈ ���� ��...";
                summonButton.gameObject.SetActive(false);
                draftManager.StartDraft(currentRound);
                break;
            case GameState.Player1_Turn:
                currentPlayer = player1;
                SetupTurn();
                break;
            case GameState.Player2_Turn:
                currentPlayer = player2;
                SetupTurn();
                break;
            case GameState.ResolvingCombat:
                summonButton.gameObject.SetActive(false);
                turnIndicatorText.text = "���� ���� ��...";

                // ������ Ŭ���̾�Ʈ�� ������ ������ �����Ͽ� �ߺ� ��� ����
                if (PhotonNetwork.IsMasterClient)
                {
                    unitManager.ApplyContinuousDamage();
                }

                // 1�� �� ���� �� ���� (���� ����� �� �ð�)
                Invoke("GoToNextTurn", 1.0f);
                break;
            case GameState.GameOver:
                summonButton.gameObject.SetActive(false);
                turnIndicatorText.text = "���� ����!";
                break;
        }
    }

    private void SetupTurn()
    {
        turnIndicatorText.text = $"{currentPlayer.NickName}�� ��";
        boardManager.currentTurnPlayerHand = currentPlayer.IsLocal ? myHand : null;
        summonButton.gameObject.SetActive(true);
        summonButton.interactable = currentPlayer.IsLocal;
    }

    private void GoToNextTurn()
    {
        if (currentPlayer == player1) ChangeState(GameState.Player2_Turn);
        else ChangeState(GameState.Player1_Turn);
    }

    #endregion

    #region �÷��̾� �Է� ó�� (UI ȣ��)

    /// <summary>
    /// PlayerHand�� �ֻ��� ��ư�� Ŭ������ �� ȣ��� �Լ�
    /// </summary>
    public void SelectDiceFromHand(int diceNumber)
    {
        if (currentState != GameState.Player1_Turn && currentState != GameState.Player2_Turn) return;
        if (!currentPlayer.IsLocal) return; // �� ���� �ƴϸ� ���� �Ұ�
        if (!myHand.HasDice(diceNumber)) return;

        selectedDiceNumber = diceNumber;
        Debug.Log($"{diceNumber + 1}�� �� �ֻ��� ���õ�.");
        // TODO: ���õ� �ֻ��� ��ư�� �׵θ� �� �ð��� ȿ�� ǥ��
    }

    /// <summary>
    /// 6x6 �׸����� Ÿ���� Ŭ������ �� ȣ��� �Լ�
    /// </summary>
    public void OnGridTileClicked(int x, int y)
    {
        if (currentState != GameState.Player1_Turn && currentState != GameState.Player2_Turn) return;
        if (!currentPlayer.IsLocal) return; // �� ���� �ƴϸ� �ൿ �Ұ�

        if (selectedDiceNumber != null)
        {
            // ���п��� ������ �ֻ����� ������ -> ��ġ �õ�
            boardManager.PlaceDiceOnGrid(selectedDiceNumber.Value, x, y);
            selectedDiceNumber = null; // ��ġ �� ���� ���� ����
            // TODO: �ֻ��� ��ư�� ���� ȿ�� ����
        }
        else
        {
            // ������ �ֻ����� ������ -> Ÿ�Ͽ� ���� �ֻ��� ȸ�� �õ�
            boardManager.PickUpDiceFromGrid(x, y);
        }
    }

    /// <summary>
    /// '��ȯ' ��ư�� ������ �� ȣ��� �Լ�
    /// </summary>
    public void OnSummonButtonClicked()
    {
        List<CompletedYeokInfo> completedYeoks = boardManager.ScanAllLines();

        if (completedYeoks.Count > 0)
        {
            unitManager.SummonUnitsViaRPC(completedYeoks, PhotonNetwork.LocalPlayer);
            boardManager.ClearUsedDice(completedYeoks);
        }
        else
        {
            Debug.Log("�ϼ��� ���� ���� ��ȯ�� ������ �����ϴ�. ���� �ѱ�ϴ�.");
        }

        ChangeState(GameState.ResolvingCombat);
    }

    #endregion

    /// <summary>
    /// DraftManager�� �巡��Ʈ�� ��� ��ģ �� ȣ���� �Լ�
    /// </summary>
    public void EndDraftPhase(int[] p1Dice, int[] p2Dice)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < p1Dice.Length; i++) myHand.AddDice(i, p1Dice[i]);
        }
        else
        {
            for (int i = 0; i < p2Dice.Length; i++) myHand.AddDice(i, p2Dice[i]);
        }

        Debug.Log("�巡��Ʈ�� ����Ǿ����ϴ�. P1�� �Ϻ��� �����մϴ�.");
        ChangeState(GameState.Player1_Turn);
    }

    private int GetPerimeterIndexFromCoords(int x, int y)
    {
        if (y == 7) return x - 1;
        if (y == 0) return 6 + (x - 1);
        if (x == 0) return 12 + (y - 1);
        if (x == 7) return 18 + (y - 1);
        return -1;
    }
}