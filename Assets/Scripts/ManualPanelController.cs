// ManualPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualPanelController : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Image manualImage;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI pageNumberText;

    [Header("매뉴얼 페이지 설정")]
    [SerializeField] private Sprite[] manualPages; // 인스펙터에서 매뉴얼 이미지들을 연결

    private int currentPageIndex = 0;

    void Start()
    {
        // 버튼 클릭 시 실행될 함수 연결
        previousButton.onClick.AddListener(GoToPreviousPage);
        nextButton.onClick.AddListener(GoToNextPage);
        closeButton.onClick.AddListener(ClosePanel);

        // 시작 시 첫 페이지를 보여줌
        UpdatePanel();
    }

    void Update()
    {
        // 키보드 방향키 입력 처리
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            GoToPreviousPage();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            GoToNextPage();
        }
        // ESC 키로도 닫을 수 있도록 함
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
        // 패널 오브젝트를 비활성화하여 닫음
        gameObject.SetActive(false);
    }

    // 현재 페이지에 맞게 UI를 갱신하는 함수
    private void UpdatePanel()
    {
        // 현재 페이지 인덱스에 맞는 스프라이트를 이미지에 표시
        manualImage.sprite = manualPages[currentPageIndex];

        // 페이지 번호 텍스트 갱신 (예: "1 / 4")
        pageNumberText.text = $"{currentPageIndex + 1} / {manualPages.Length}";

        // 첫 페이지에서는 '이전' 버튼을, 마지막 페이지에서는 '다음' 버튼을 숨김
        previousButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < manualPages.Length - 1);
    }
}