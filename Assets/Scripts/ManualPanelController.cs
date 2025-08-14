// ManualPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualPanelController : MonoBehaviour
{
    [Header("UI ��� ����")]
    [SerializeField] private Image manualImage;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI pageNumberText;

    [Header("�Ŵ��� ������ ����")]
    [SerializeField] private Sprite[] manualPages; // �ν����Ϳ��� �Ŵ��� �̹������� ����

    private int currentPageIndex = 0;

    void Start()
    {
        // ��ư Ŭ�� �� ����� �Լ� ����
        previousButton.onClick.AddListener(GoToPreviousPage);
        nextButton.onClick.AddListener(GoToNextPage);
        closeButton.onClick.AddListener(ClosePanel);

        // ���� �� ù �������� ������
        UpdatePanel();
    }

    void Update()
    {
        // Ű���� ����Ű �Է� ó��
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            GoToPreviousPage();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            GoToNextPage();
        }
        // ESC Ű�ε� ���� �� �ֵ��� ��
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    private void GoToPreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePanel();
        }
    }

    private void GoToNextPage()
    {
        if (currentPageIndex < manualPages.Length - 1)
        {
            currentPageIndex++;
            UpdatePanel();
        }
    }

    private void ClosePanel()
    {
        // �г� ������Ʈ�� ��Ȱ��ȭ�Ͽ� ����
        gameObject.SetActive(false);
    }

    // ���� �������� �°� UI�� �����ϴ� �Լ�
    private void UpdatePanel()
    {
        // ���� ������ �ε����� �´� ��������Ʈ�� �̹����� ǥ��
        manualImage.sprite = manualPages[currentPageIndex];

        // ������ ��ȣ �ؽ�Ʈ ���� (��: "1 / 4")
        pageNumberText.text = $"{currentPageIndex + 1} / {manualPages.Length}";

        // ù ������������ '����' ��ư��, ������ ������������ '����' ��ư�� ����
        previousButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < manualPages.Length - 1);
    }
}