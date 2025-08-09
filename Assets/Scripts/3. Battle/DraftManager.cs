using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; // Photon ����� ���� �߰�

public class DraftManager : MonoBehaviourPunCallbacks // MonoBehaviour ��� PhotonPunCallbacks�� ���
{
    // --- UI ���� ���� ---
    [SerializeField] private Sprite[] diceSprites;
    [SerializeField] private Button confirmButton;
    [SerializeField] private List<GameObject> dicePoolObjects; // 14���� �ֻ��� ������Ʈ Ǯ
    [SerializeField] private TextMeshProUGUI turnIndicatorText;

    // --- �巡��Ʈ ���� ������ ���� ������ ---
    private List<Button> availableDiceButtons = new List<Button>();
    private List<Button> selectedThisTurn = new List<Button>();

    private int[] myDiceCount = new int[6];
    private int[] versusDiceCount = new int[6];

    private int currentRound = 1;
    private int turnStep = 0; // ���� �巡��Ʈ ���� (0���� ����)
    private bool isMyTurn = false;
    private int picksRequired = 0;

    // ���庰 ��Ģ �̸� ����
    private readonly int[] pickOrder = { 1, 2, 2, 2, 2, 2, 2, 1 };

    private int[] currentPickOrder;

    void Start()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        //StartDraft(currentRound);
    }

    /// <summary>
    /// ������ ������ �巡��Ʈ�� �����մϴ�. (������ Ŭ���̾�Ʈ�� ȣ��)
    /// </summary>
    public void StartDraft() // ���� round �Ű������� �ʿ� �����ϴ�.
    {
        // �ֻ��� Ǯ �ʱ�ȭ �� ���� (������ Ŭ���̾�Ʈ������ ����)
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
                diceObject.GetComponent<Image>().color = Color.white; // ���� �ʱ�ȭ
                diceObject.transform.localScale = Vector3.one; // ũ�� �ʱ�ȭ

                Button diceButton = diceObject.GetComponent<Button>();
                int diceIndex = i; // Ŭ���� ���� ������ ���� �ε��� ����

                // �ֻ��� �� ����
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

        // Photon������ ������ Ŭ���̾�Ʈ�� ����(P1)�� �˴ϴ�.
        SetTurn();
    }

    /// <summary>
    /// ��� Ŭ���̾�Ʈ���� ���忡 �´� �巡��Ʈ ��Ģ�� �����ϱ� ���� �Լ�
    /// </summary>
    public void InitializeForRound(int round)
    {
        currentRound = round;
        turnStep = 0;

        // --- �Ʒ� �� ���� �߰��ϼ��� ---
        System.Array.Clear(myDiceCount, 0, myDiceCount.Length);
        System.Array.Clear(versusDiceCount, 0, versusDiceCount.Length);

        // --- ������ �κ� ---
        // ����� ������� �׻� ���ϵ� ��Ģ�� ����մϴ�.
        currentPickOrder = pickOrder;
    }

    /// <summary>
    /// �巡��Ʈ Ǯ�� �ֻ����� Ŭ������ �� ȣ��˴ϴ�.
    /// </summary>
    private void OnDiceSelected(Button selectedButton, int diceIndex)
    {
        if (!isMyTurn) return; // �� ���� �ƴϸ� ���� �Ұ�

        // RPC�� ���� ��� Ŭ���̾�Ʈ���� ���� ������ �˸�
        photonView.RPC("SyncDiceSelectionRPC", RpcTarget.All, diceIndex, PhotonNetwork.IsMasterClient);
    }

    [PunRPC]
    private void SyncDiceSelectionRPC(int diceIndex, bool isMasterClient)
    {
        Button selectedButton = dicePoolObjects[diceIndex].GetComponent<Button>();

        // ���� ������ �ֻ�������, ��밡 ������ �ֻ��������� ���� ���� ����
        Color selectionColor = Color.white;
        if (PhotonNetwork.IsMasterClient == isMasterClient) // ���� ����
        {
            selectionColor = Color.red;
        }
        else // ����� ����
        {
            selectionColor = Color.blue;
        }

        // ���� ����Ʈ���� ����/���� ���� ó�� (��� Ŭ���̾�Ʈ���� �����ϰ� ����)
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
            Debug.Log($"�ֻ����� {picksRequired}�� �����ؾ� �մϴ�.");
            return;
        }

        // ���� Ȯ�� ������ RPC�� ����
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
        // ���õ� �ֻ������� Ȯ���ϰ� ī��Ʈ
        foreach (int index in selectedIndices)
        {
            GameObject diceObject = dicePoolObjects[index];
            diceObject.GetComponent<Button>().interactable = false; // ��Ȱ��ȭ

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

        // ��� �巡��Ʈ�� �������� Ȯ��
        if (turnStep >= currentPickOrder.Length)
        {
            turnIndicatorText.text = "�巡��Ʈ ����!";

            // --- ������ �κ� ---
            // GameManager���� �巡��Ʈ ����� �����ϰ�, �ڽ��� ��Ȱ��ȭ
            GameManager.Instance.EndDraftPhase(myDiceCount, versusDiceCount);
            this.gameObject.SetActive(false); // �巡����Ʈ �Ŵ����� ������ �������Ƿ� ��Ȱ��ȭ
        }
        else
        {
            SetTurn();
        }
    }

    private void SetTurn()
    {
        // �� ����(¦/Ȧ��)�� ���� �� ������ ����
        // (P1->P2->P1->P2...)
        bool isP1Turn = (turnStep % 2 == 0);
        isMyTurn = (PhotonNetwork.IsMasterClient == isP1Turn);

        picksRequired = currentPickOrder[turnStep];
        if (isMyTurn)
        {
            turnIndicatorText.text = $"<color=red>My Turn</color> ({picksRequired}�� ����)";
        }
        else
        {
            turnIndicatorText.text = $"<color=blue>Versus Turn</color>";
        }
    }
}