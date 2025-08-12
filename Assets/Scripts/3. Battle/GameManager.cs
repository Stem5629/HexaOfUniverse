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

    [SerializeField] private GameObject infoPanel; // ����â �г� ������Ʈ
    [SerializeField] private TextMeshProUGUI unitTreeText;
    [SerializeField] private TextMeshProUGUI treeCombinationText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI beginDamageText;
    [SerializeField] private TextMeshProUGUI keepDamageText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("���� ��Ģ ����")]
    [SerializeField] private float turnTimeLimit = 60f;

    // --- ���� ���� ---
    private UnitManager unitManager;
    private Player player1; // MasterClient
    private Player player2; // Other
    private Player currentPlayer;
    private Player winner;
    private int currentRound = 1;
    private int? selectedDiceNumber = null; // ���� ���п��� ������ �ֻ��� ��
    private Coroutine turnTimerCoroutine;

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

        player1 = PhotonNetwork.MasterClient;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.IsMasterClient)
            {
                player2 = p;
                break;
            }
        }

        if (unitManager == null && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("������ Ŭ���̾�Ʈ�� UnitManager�� �����մϴ�.");
            GameObject umObject = PhotonNetwork.Instantiate(unitManagerPrefab.name, Vector3.zero, Quaternion.identity);

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
            SetUnitManager(pv.GetComponent<UnitManager>());
        }
    }

    public void SetUnitManager(UnitManager manager)
    {
        this.unitManager = manager;

        manager.infoPanel = this.infoPanel;
        manager.unitTreeText = this.unitTreeText;
        manager.treeCombinationText = this.treeCombinationText;
        manager.hpText = this.hpText;
        manager.beginDamageText = this.beginDamageText;
        manager.keepDamageText = this.keepDamageText;

        InitializeField();

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.Draft, currentRound);
        }
    }

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
    private void ChangeStateRPC(int newStateIndex, int newRound = -1)
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        if (newRound != -1)
        {
            this.currentRound = newRound;
        }

        currentState = (GameState)newStateIndex;
        Debug.Log($"���� ���� -> {currentState}");

        switch (currentState)
        {
            case GameState.Draft:
                roundIndicatorText.text = $"Round {currentRound}";
                turnIndicatorText.text = "�巡��Ʈ ���� ��...";
                timerText.text = "";
                summonButton.gameObject.SetActive(false);
                draftManager.gameObject.SetActive(true);
                bool isP1Starting = (currentRound % 2 == 1);
                draftManager.InitializeForRound(currentRound, isP1Starting);

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
                if (PhotonNetwork.IsMasterClient)
                {
                    StartCoroutine(GoToNextTurnCoroutine());
                }
                break;
            case GameState.GameOver:
                summonButton.gameObject.SetActive(false);
                draftManager.gameObject.SetActive(false);
                if (winner == null)
                {
                    turnIndicatorText.text = "���º�!";
                }
                else
                {
                    turnIndicatorText.text = $"{winner.NickName}���� �¸�!";
                }
                break;
        }
    }

    private void SetupTurn()
    {
        turnIndicatorText.text = $"{currentPlayer.NickName}�� ��";
        boardManager.currentTurnPlayerHand = currentPlayer.IsLocal ? myHand : null;
        summonButton.gameObject.SetActive(true);
        summonButton.interactable = currentPlayer.IsLocal;

        // --- �Ʒ� Ÿ�̸� ���� �ڵ带 �߰��մϴ� ---
        // ������ Ŭ���̾�Ʈ�� Ÿ�̸Ӹ� �����ϰ�, RPC�� ��� Ŭ���̾�Ʈ���� �ð��� ����ȭ�մϴ�.
        if (PhotonNetwork.IsMasterClient)
        {
            StartTurnTimer();
        }
    }

    public Player GetCurrentPlayer()
    {
        return currentPlayer;
    }

    private IEnumerator GoToNextTurnCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        player1.CustomProperties.TryGetValue("HP", out object p1HpObj);
        player2.CustomProperties.TryGetValue("HP", out object p2HpObj);
        int p1Hp = (p1HpObj != null) ? (int)p1HpObj : 100;
        int p2Hp = (p2HpObj != null) ? (int)p2HpObj : 100;

        bool p1Lost = p1Hp <= 0;
        bool p2Lost = p2Hp <= 0;

        if (p1Lost || p2Lost)
        {
            if (p1Lost && p2Lost) // 1. �� �� �й��� ���
            {
                // HP�� �� ���� ���� �й��մϴ�.
                if (p1Hp < p2Hp)
                {
                    winner = player2; // P1�� HP�� �� �����Ƿ� P2�� �¸�
                }
                else if (p2Hp < p1Hp)
                {
                    winner = player1; // P2�� HP�� �� �����Ƿ� P1�� �¸�
                }
                else // p1Hp == p2Hp
                {
                    winner = null; // HP�� ��Ȯ�� ���ٸ� ���º�
                }
            }
            else if (p1Lost) // 2. P1�� �й��� ���
            {
                winner = player2;
            }
            else // 3. P2�� �й��� ���
            {
                winner = player1;
            }

            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.GameOver, -1);
            yield break;
        }

        GameState nextState;
        int nextRound = -1;

        bool isP1Starter = (currentRound % 2 == 1);
        Player roundEndPlayer = isP1Starter ? player2 : player1;

        if (currentPlayer == roundEndPlayer)
        {
            // ���尡 ������ �������� ���� �������� �����մϴ�.
            unitManager.ApplyContinuousDamage();

            currentRound++;
            nextRound = currentRound;
            nextState = GameState.Draft;
        }
        else
        {
            nextState = (roundEndPlayer == player1) ? GameState.Player1_Turn : GameState.Player2_Turn;
        }

        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)nextState, nextRound);
    }
    #endregion

    #region �÷��̾� �Է� �� RPC
    public void SelectDiceFromHand(int diceNumber)
    {
        if (!IsMyTurn()) return;
        if (!myHand.HasDice(diceNumber)) return;
        selectedDiceNumber = diceNumber;
    }

    public void OnGridTileClicked(int x, int y)
    {
        if (!IsMyTurn()) return;

        if (selectedDiceNumber != null)
        {
            photonView.RPC("RequestPlaceDice", RpcTarget.MasterClient, selectedDiceNumber.Value, x, y);
            selectedDiceNumber = null;
        }
        else
        {
            photonView.RPC("RequestPickUpDice", RpcTarget.MasterClient, x, y);
        }
    }

    [PunRPC]
    private void RequestPlaceDice(int diceNumber, int x, int y, PhotonMessageInfo info)
    {
        if (info.Sender != currentPlayer) return;
        photonView.RPC("SyncPlaceDice", RpcTarget.All, diceNumber, x, y, info.Sender);
    }

    [PunRPC]
    private void RequestPickUpDice(int x, int y, PhotonMessageInfo info)
    {
        if (info.Sender != currentPlayer) return;
        photonView.RPC("SyncPickUpDice", RpcTarget.All, x, y);
    }

    [PunRPC]
    private void SyncPlaceDice(int diceNumber, int x, int y, Player placingPlayer)
    {
        boardManager.PlaceDiceOnGrid(diceNumber, x, y, placingPlayer);
    }

    [PunRPC]
    private void SyncPickUpDice(int x, int y)
    {
        boardManager.PickUpDiceFromGrid(x, y);
    }

    public void OnSummonButtonClicked()
    {
        photonView.RPC("FinishTurnRPC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void FinishTurnRPC(Player playerWhoFinished)
    {
        if (!PhotonNetwork.IsMasterClient || playerWhoFinished != currentPlayer) return;

        List<CompletedYeokInfo> completedYeoks = boardManager.ScanAllLines();

        if (completedYeoks.Count > 0)
        {
            unitManager.SummonUnitsViaRPC(completedYeoks, playerWhoFinished);
        }

        boardManager.ClearAllDiceViaRPC();

        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.ResolvingCombat, -1);
    }

    public void EndDraftPhase(int[] p1Dice, int[] p2Dice)
    {
        int[] myDiceResult = p1Dice;
        for (int i = 0; i < myDiceResult.Length; i++)
        {
            if (myDiceResult[i] > 0) myHand.AddDice(i, myDiceResult[i]);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            bool isP1Starting = (currentRound % 2 == 1);
            GameState firstTurnState = isP1Starting ? GameState.Player1_Turn : GameState.Player2_Turn;
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)firstTurnState, -1);
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

    private void StartTurnTimer()
    {
        // ������ ����Ǵ� Ÿ�̸Ӱ� �ִٸ� ������ŵ�ϴ�.
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }
        // ���ο� Ÿ�̸� �ڷ�ƾ�� �����մϴ�.
        turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }

    private IEnumerator TurnTimerCoroutine()
    {
        float remainingTime = turnTimeLimit;
        while (remainingTime > 0)
        {
            // ��� Ŭ���̾�Ʈ���� ���� �ð��� ����ȭ�Ͽ� UI�� ������Ʈ�ϵ��� ����մϴ�.
            photonView.RPC("UpdateTimerRPC", RpcTarget.All, remainingTime);

            yield return new WaitForSeconds(1.0f);
            remainingTime--;
        }

        // �ð��� 0�� �Ǹ� ���� ������ �����մϴ�.
        Debug.Log($"{currentPlayer.NickName}���� �� �ð��� �ʰ��Ǿ����ϴ�.");
        photonView.RPC("UpdateTimerRPC", RpcTarget.All, 0f); // UI�� 0���� ����

        // ���� ���� �÷��̾ ������ '��ȯ' ��ư�� ���� ��ó�� ó���մϴ�.
        photonView.RPC("FinishTurnRPC", RpcTarget.MasterClient, currentPlayer);
    }

    [PunRPC]
    private void UpdateTimerRPC(float time)
    {
        // �ʸ� ��:�� ������ ���ڿ��� ��ȯ�մϴ�. (��: 60 -> 01:00)
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    #endregion
}