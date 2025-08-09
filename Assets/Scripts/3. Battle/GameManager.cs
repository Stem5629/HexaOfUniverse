using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// BattleScene�� ��ü ���� ���¿� �� �帧, �÷��̾� �Է��� �����ϴ� �Ѱ��� ��ũ��Ʈ�Դϴ�.
/// ��� �� ��ȯ�� ���� ������ ������ Ŭ���̾�Ʈ�� �ֵ��ϰ�, RPC�� ���� ��� Ŭ���̾�Ʈ���� �����մϴ�.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    // --- ���� ���� ---
    public enum GameState { WaitingForPlayers, Draft, Player1_Turn, Player2_Turn, ResolvingCombat, GameOver }
    [SerializeField] private GameState currentState;

    [Header("�ٽ� �Ŵ��� �� ������")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private GameObject unitManagerPrefab; // Resources ������ �־�� �մϴ�.
    [SerializeField] private DraftManager draftManager;
    [SerializeField] private PlayerHand myHand;
    [SerializeField] private Transform tileContainer; // GridLayoutGroup�� �ִ� �θ� ������Ʈ

    [Header("UI ����")]
    [SerializeField] private Button summonButton;
    [SerializeField] private TextMeshProUGUI turnIndicatorText;
    [SerializeField] private TextMeshProUGUI roundIndicatorText;

    // --- ���� ���� ---
    private UnitManager unitManager;
    private Player player1; // MasterClient
    private Player player2; // Other
    private Player currentPlayer;
    private int currentRound = 1;
    private int? selectedDiceNumber = null; // ���� ���п��� ������ �ֻ��� ��

    // --- �̱��� ---
    public static GameManager Instance;

    #region �ʱ�ȭ �� ����
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        summonButton.gameObject.SetActive(false);
        turnIndicatorText.text = "������ ��ٸ��� ��...";
        roundIndicatorText.text = $"Round {currentRound}";
        summonButton.onClick.AddListener(OnSummonButtonClicked);
        TrySetupPlayers();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySetupPlayers();
    }

    private void TrySetupPlayers()
    {
        if (PhotonNetwork.PlayerList.Length < 2) return;

        // Player1�� �׻� ������ Ŭ���̾�Ʈ�Դϴ�. �� ������ ��� ��ǻ�Ϳ��� �����մϴ�.
        player1 = PhotonNetwork.MasterClient;

        // ��ü �÷��̾� ����� Ȯ���Ͽ�, ������ Ŭ���̾�Ʈ�� �ƴ� ����� Player2�� �����մϴ�.
        // �� ���� ���� ��� ��ǻ�Ϳ��� ������ ����� �����մϴ�.
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.IsMasterClient)
            {
                player2 = p;
                break; // Player2�� ã������ �ݺ��� �����մϴ�.
            }
        }

        // UnitManager�� ���� �������� �ʾҰ�, ���� ������ Ŭ���̾�Ʈ��� �����մϴ�.
        // UnitManager�� ���� �� �ϳ��� �����ؾ� �մϴ�.
        if (unitManager == null && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("������ Ŭ���̾�Ʈ�� UnitManager�� �����մϴ�.");
            // 1. Resources ������ �ִ� UnitManager �������� ��Ʈ��ũ �� �����մϴ�.
            GameObject umObject = PhotonNetwork.Instantiate(unitManagerPrefab.name, Vector3.zero, Quaternion.identity);

            // 2. ��� Ŭ���̾�Ʈ���� ��� ������ UnitManager�� ������ �˷��ݴϴ�.
            if (umObject != null)
            {
                photonView.RPC("SetUnitManagerRPC", RpcTarget.All, umObject.GetComponent<PhotonView>().ViewID);
            }
        }
    }

    [PunRPC]
    private void SetUnitManagerRPC(int viewID)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv != null)
        {
            // --- �ٷ� �� �Լ� ȣ���� ���� �Ʒ��� �Լ��� �߰��ؾ� �մϴ� ---
            SetUnitManager(pv.GetComponent<UnitManager>());
        }
    }

    /// <summary>
    /// ������ UnitManager�� ������ GameManager�� ����ϰ� �ʱ�ȭ�� �����մϴ�.
    /// (UnitManager.cs���� �� �Լ��� ȣ���մϴ�)
    /// </summary>
    public void SetUnitManager(UnitManager manager)
    {
        this.unitManager = manager;
        InitializeField(); // �ʵ� �ʱ�ȭ�� UnitManager�� ������ �Ŀ� ����

        // ��� Ŭ���̾�Ʈ�� ������ �������� Ȯ�� ��, ������ �巡��Ʈ ������ ���
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.Draft);
        }
    }

    /// <summary>
    /// ���� ���� �� Ÿ�ϵ��� �ڵ����� ã�� �� �Ŵ������� ������ �ο��մϴ�.
    /// </summary>
    private void InitializeField()
    {
        if (tileContainer == null)
        {
            Debug.LogError("ġ���� ����: GameManager�� �ν����Ϳ� 'Tile Container'�� ������� �ʾҽ��ϴ�!", this.gameObject);
            return;
        }

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

        boardManager.Initialize(gridTiles);
        unitManager.Initialize(perimeterSlots);
        Debug.Log("�ʵ� �ڵ�ȭ ���� �Ϸ�!");
    }
    #endregion

    #region ���� ����
    [PunRPC]
    private void ChangeStateRPC(int newStateIndex)
    {
        currentState = (GameState)newStateIndex;
        Debug.Log($"���� ���� -> {currentState}");

        switch (currentState)
        {
            case GameState.Draft:
                turnIndicatorText.text = "�巡��Ʈ ���� ��...";
                summonButton.gameObject.SetActive(false);
                draftManager.gameObject.SetActive(true);

                // --- ������ �κ� ---
                // 1. ��� Ŭ���̾�Ʈ�� ���� ��Ģ�� ���� �ʱ�ȭ�մϴ�.
                draftManager.InitializeForRound(currentRound);
                // 2. ������ Ŭ���̾�Ʈ�� ���� �巡��Ʈ�� �����մϴ�.
                if (PhotonNetwork.IsMasterClient) draftManager.StartDraft();
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
                // ���常 ���� ������ �Ѿ�� �ڷ�ƾ�� ����
                if (PhotonNetwork.IsMasterClient)
                {
                    StartCoroutine(GoToNextTurnCoroutine());
                }
                break;
            case GameState.GameOver:
                summonButton.gameObject.SetActive(false);
                turnIndicatorText.text = "���� ����!";
                // TODO: ��� �� �ε� �Ǵ� ����� ��ư Ȱ��ȭ
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
    
    /// <summary>
     /// �ٸ� ��ũ��Ʈ���� ���� ���� �÷��̾� ������ ������ �� �ֵ��� ���ִ� �Լ�
     /// </summary>
     /// <returns>���� ���� Player ��ü</returns>
    public Player GetCurrentPlayer()
    {
        return currentPlayer;
    }

    private IEnumerator GoToNextTurnCoroutine()
    {
        // ���� ������ ���� (������ Ŭ���̾�Ʈ������ ����)
        unitManager.ApplyContinuousDamage();

        // ���� ����� �� �ð�
        yield return new WaitForSeconds(1.5f);

        // --- ������ �κ� ---

        GameState nextState;

        // ���� ���� Player1�� ���̾��°�?
        if (currentPlayer == player1)
        {
            // �׷��ٸ� ���� ���� Player2�� ���Դϴ�.
            nextState = GameState.Player2_Turn;
        }
        else // ���� ���� Player2�� ���̾��ٸ�
        {
            // �� ���尡 ����� ���Դϴ�.
            currentRound++; // ���� ���ڸ� 1 ������ŵ�ϴ�.
            roundIndicatorText.text = $"Round {currentRound}";
            nextState = GameState.Draft;
        }

        // --- ������� ���� ---

        // ��� Ŭ���̾�Ʈ���� ���� ���� ������ �˸�
        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)nextState);
    }
    #endregion

    #region �÷��̾� �Է� �� RPC
    public void SelectDiceFromHand(int diceNumber)
    {
        if (!IsMyTurn()) return;
        if (!myHand.HasDice(diceNumber)) return;
        selectedDiceNumber = diceNumber;
        // TODO: ���õ� �ֻ��� ��ư�� �ð��� ȿ�� ǥ��
    }

    public void OnGridTileClicked(int x, int y)
    {
        if (!IsMyTurn()) return;

        // 1. �ൿ ��û (Client -> Master)
        if (selectedDiceNumber != null)
        {
            // ���� ������ �ֻ����� ���� ��ġ�� ������ Ŭ���̾�Ʈ���� "��û"�մϴ�.
            photonView.RPC("RequestPlaceDice", RpcTarget.MasterClient, selectedDiceNumber.Value, x, y);
            selectedDiceNumber = null;
        }
        else
        {
            // �ֻ��� ȸ���� ������ Ŭ���̾�Ʈ���� "��û"�մϴ�.
            photonView.RPC("RequestPickUpDice", RpcTarget.MasterClient, x, y);
        }
    }

    [PunRPC]
    private void RequestPlaceDice(int diceNumber, int x, int y, PhotonMessageInfo info)
    {
        // ��û�� �÷��̾ ���� ���� �´��� ������ Ŭ���̾�Ʈ�� �����մϴ�.
        if (info.Sender != currentPlayer) return;

        // ���⿡ �߰����� ��ȿ�� �˻�(��: ��� Ȯ�� ��)�� �� �� �ֽ��ϴ�.

        // 2. ���� ����ȭ (Master -> All)
        // ��� Ŭ���̾�Ʈ���� ���� �ֻ��� ��ġ�� "����ȭ"�϶�� ����մϴ�.
        photonView.RPC("SyncPlaceDice", RpcTarget.All, diceNumber, x, y, info.Sender);
    }

    [PunRPC]
    private void RequestPickUpDice(int x, int y, PhotonMessageInfo info)
    {
        // ��û�� �÷��̾ ���� ���� �´��� ������ Ŭ���̾�Ʈ�� �����մϴ�.
        if (info.Sender != currentPlayer) return;

        // ��� Ŭ���̾�Ʈ���� ���� �ֻ��� ȸ���� "����ȭ"�϶�� ����մϴ�.
        photonView.RPC("SyncPickUpDice", RpcTarget.All, x, y);
    }

    [PunRPC]
    private void SyncPlaceDice(int diceNumber, int x, int y, Player placingPlayer)
    {
        // BoardManager�� �ֻ��� ��ġ ������ ȣ���մϴ�.
        // �� �Լ��� ��� Ŭ���̾�Ʈ���� ����Ǿ�, ����� ���� ���¸� �����ϰ� ����ϴ�.
        boardManager.PlaceDiceOnGrid(diceNumber, x, y, placingPlayer);
    }

    [PunRPC]
    private void SyncPickUpDice(int x, int y)
    {
        // BoardManager�� �ֻ��� ȸ�� ������ ȣ���մϴ�.
        boardManager.PickUpDiceFromGrid(x, y);
    }

    public void OnSummonButtonClicked()
    {
        // ��ư�� ���� �÷��̾�� "�� ���� ��ġ�ڴ�"�� ���忡�� �˸��ϴ�.
        photonView.RPC("FinishTurnRPC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void FinishTurnRPC(Player playerWhoFinished)
    {
        if (!PhotonNetwork.IsMasterClient || playerWhoFinished != currentPlayer) return;

        List<CompletedYeokInfo> completedYeoks = boardManager.ScanAllLines();

        // ���� �ִٸ� ������ ��ȯ�ϴ� ������ �״�� �Ӵϴ�.
        if (completedYeoks.Count > 0)
        {
            unitManager.SummonUnitsViaRPC(completedYeoks, playerWhoFinished);
            // boardManager.ClearUsedDiceViaRPC(completedYeoks); <-- �� ���� �����մϴ�.
        }

        // --- ������ �κ� ---
        // ���� ���� ���ο� �������, �׻� ���� ��ü�� �ʱ�ȭ�ϴ� �� �Լ��� ȣ���մϴ�.
        boardManager.ClearAllDiceViaRPC();

        // ��� Ŭ���̾�Ʈ�� ���¸� '���� ����'���� �����϶�� ���
        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.ResolvingCombat);
    }

    public void EndDraftPhase(int[] p1Dice, int[] p2Dice)
    {
        // �巡��Ʈ ����� �� Ŭ���̾�Ʈ�� DraftManager�� ���÷� ����ϹǷ�,
        // GameManager�� �ڽ��� ���и� ä��� �˴ϴ�.

        // �� Ŭ���̾�Ʈ�� DraftManager�� �ڽ��� �ֻ��� ����� ù ��° ���ڷ� �Ѱ��ֹǷ�,
        // p1Dice�� �׻� "�� �ֻ���"�� �˴ϴ�.
        int[] myDiceResult = p1Dice; // <-- �̷��� �����ϼ���!

        for (int i = 0; i < myDiceResult.Length; i++)
        {
            if (myDiceResult[i] > 0) myHand.AddDice(i, myDiceResult[i]);
        }

        // ���常 ù �� ������ �˸��ϴ�.
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.Player1_Turn);
        }
    }
    #endregion

    #region ���� �Լ�
    private bool IsMyTurn()
    {
        if (currentState != GameState.Player1_Turn && currentState != GameState.Player2_Turn) return false;
        return currentPlayer.IsLocal;
    }

    private int GetPerimeterIndexFromCoords(int x, int y)
    {
        if (y == 7) return x - 1;
        if (y == 0) return 6 + (x - 1);
        if (x == 0) return 12 + (y - 1);
        if (x == 7) return 18 + (y - 1);
        return -1;
    }
    #endregion
}