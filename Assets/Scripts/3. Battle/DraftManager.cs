using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; // Photon 사용을 위해 추가

public class DraftManager : MonoBehaviourPunCallbacks // MonoBehaviour 대신 PhotonPunCallbacks를 상속
{
    // --- UI 참조 변수 ---
    [SerializeField] private Sprite[] diceSprites;
    [SerializeField] private Button confirmButton;
    [SerializeField] private List<GameObject> dicePoolObjects; // 14개의 주사위 오브젝트 풀
    [SerializeField] private TextMeshProUGUI turnIndicatorText;

    // --- 드래프트 상태 관리를 위한 변수들 ---
    private List<Button> availableDiceButtons = new List<Button>();
    private List<Button> selectedThisTurn = new List<Button>();

    private int[] myDiceCount = new int[6];
    private int[] versusDiceCount = new int[6];

    private int currentRound = 1;
    private int turnStep = 0; // 현재 드래프트 순서 (0부터 시작)
    private bool isMyTurn = false;
    private int picksRequired = 0;

    // 라운드별 규칙 미리 정의
    private readonly int[] pickOrder = { 1, 2, 2, 2, 2, 2, 2, 1 };

    private int[] currentPickOrder;

    void Start()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        //StartDraft(currentRound);
    }

    /// <summary>
    /// 지정된 라운드의 드래프트를 시작합니다. (마스터 클라이언트만 호출)
    /// </summary>
    public void StartDraft() // 이제 round 매개변수가 필요 없습니다.
    {
        // 주사위 풀 초기화 및 생성 (마스터 클라이언트에서만 실행)
        if (PhotonNetwork.IsMasterClient)
        {
            int diceToDraft = currentPickOrder.Sum();

            List<int> randomDiceNumbers = new List<int>();
            for (int i = 0; i < diceToDraft; i++)
            {
                randomDiceNumbers.Add(Random.Range(0, 6));
            }
            photonView.RPC("SetupDraftPoolRPC", RpcTarget.All, randomDiceNumbers.ToArray());
        }
    }

    [PunRPC]
    private void SetupDraftPoolRPC(int[] diceNumbers)
    {
        availableDiceButtons.Clear();
        selectedThisTurn.Clear();

        for (int i = 0; i < dicePoolObjects.Count; i++)
        {
            if (i < diceNumbers.Length)
            {
                var diceObject = dicePoolObjects[i];
                diceObject.SetActive(true);
                diceObject.GetComponent<Button>().interactable = true;
                diceObject.GetComponent<Image>().color = Color.white; // 색상 초기화
                diceObject.transform.localScale = Vector3.one; // 크기 초기화

                Button diceButton = diceObject.GetComponent<Button>();
                int diceIndex = i; // 클로저 문제 방지를 위해 인덱스 복사

                // 주사위 눈 설정
                Dice dice = diceObject.GetComponent<Dice>();
                dice.DiceNumber = diceNumbers[i];
                dice.DiceSprite = diceSprites[dice.DiceNumber];
                dice.DiceSpriteInstance();

                diceButton.onClick.RemoveAllListeners();
                diceButton.onClick.AddListener(() => OnDiceSelected(diceButton, diceIndex));
                availableDiceButtons.Add(diceButton);
            }
            else
            {
                dicePoolObjects[i].SetActive(false);
            }
        }

        // Photon에서는 마스터 클라이언트가 선공(P1)이 됩니다.
        SetTurn();
    }

    /// <summary>
    /// 모든 클라이언트에서 라운드에 맞는 드래프트 규칙을 설정하기 위한 함수
    /// </summary>
    public void InitializeForRound(int round)
    {
        currentRound = round;
        turnStep = 0;

        // --- 아래 두 줄을 추가하세요 ---
        System.Array.Clear(myDiceCount, 0, myDiceCount.Length);
        System.Array.Clear(versusDiceCount, 0, versusDiceCount.Length);

        // --- 수정된 부분 ---
        // 라운드와 상관없이 항상 통일된 규칙을 사용합니다.
        currentPickOrder = pickOrder;
    }

    /// <summary>
    /// 드래프트 풀의 주사위를 클릭했을 때 호출됩니다.
    /// </summary>
    private void OnDiceSelected(Button selectedButton, int diceIndex)
    {
        if (!isMyTurn) return; // 내 턴이 아니면 선택 불가

        // RPC를 통해 모든 클라이언트에게 나의 선택을 알림
        photonView.RPC("SyncDiceSelectionRPC", RpcTarget.All, diceIndex, PhotonNetwork.IsMasterClient);
    }

    [PunRPC]
    private void SyncDiceSelectionRPC(int diceIndex, bool isMasterClient)
    {
        Button selectedButton = dicePoolObjects[diceIndex].GetComponent<Button>();

        // 내가 선택한 주사위인지, 상대가 선택한 주사위인지에 따라 색을 결정
        Color selectionColor = Color.white;
        if (PhotonNetwork.IsMasterClient == isMasterClient) // 나의 선택
        {
            selectionColor = Color.red;
        }
        else // 상대의 선택
        {
            selectionColor = Color.blue;
        }

        // 로컬 리스트에서 선택/해제 로직 처리 (모든 클라이언트에서 동일하게 실행)
        if (selectedThisTurn.Contains(selectedButton))
        {
            selectedThisTurn.Remove(selectedButton);
            selectedButton.GetComponent<Image>().color = Color.white;
        }
        else
        {
            if (selectedThisTurn.Count < picksRequired)
            {
                selectedThisTurn.Add(selectedButton);
                selectedButton.GetComponent<Image>().color = selectionColor;
            }
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (!isMyTurn) return;
        if (selectedThisTurn.Count != picksRequired)
        {
            Debug.Log($"주사위를 {picksRequired}개 선택해야 합니다.");
            return;
        }

        // 선택 확정 정보를 RPC로 전송
        List<int> selectedIndices = new List<int>();
        foreach (var button in selectedThisTurn)
        {
            selectedIndices.Add(dicePoolObjects.IndexOf(button.gameObject));
        }
        photonView.RPC("ConfirmTurnRPC", RpcTarget.All, selectedIndices.ToArray(), PhotonNetwork.IsMasterClient);
    }

    [PunRPC]
    private void ConfirmTurnRPC(int[] selectedIndices, bool isMasterClient)
    {
        // 선택된 주사위들을 확정하고 카운트
        foreach (int index in selectedIndices)
        {
            GameObject diceObject = dicePoolObjects[index];
            diceObject.GetComponent<Button>().interactable = false; // 비활성화

            int diceNumber = diceObject.GetComponent<Dice>().DiceNumber;
            if (isMasterClient)
            {
                if (PhotonNetwork.IsMasterClient) myDiceCount[diceNumber]++;
                else versusDiceCount[diceNumber]++;
            }
            else
            {
                if (!PhotonNetwork.IsMasterClient) myDiceCount[diceNumber]++;
                else versusDiceCount[diceNumber]++;
            }
        }

        selectedThisTurn.Clear();
        turnStep++;

        // 모든 드래프트가 끝났는지 확인
        if (turnStep >= currentPickOrder.Length)
        {
            turnIndicatorText.text = "드래프트 종료!";

            // --- 수정된 부분 ---
            // GameManager에게 드래프트 결과를 전달하고, 자신은 비활성화
            GameManager.Instance.EndDraftPhase(myDiceCount, versusDiceCount);
            this.gameObject.SetActive(false); // 드래셔프트 매니저의 역할은 끝났으므로 비활성화
        }
        else
        {
            SetTurn();
        }
    }

    private void SetTurn()
    {
        // 턴 순서(짝/홀수)에 따라 턴 주인을 결정
        // (P1->P2->P1->P2...)
        bool isP1Turn = (turnStep % 2 == 0);
        isMyTurn = (PhotonNetwork.IsMasterClient == isP1Turn);

        picksRequired = currentPickOrder[turnStep];
        if (isMyTurn)
        {
            turnIndicatorText.text = $"<color=red>My Turn</color> ({picksRequired}개 선택)";
        }
        else
        {
            turnIndicatorText.text = $"<color=blue>Versus Turn</color>";
        }
    }
}