using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManualManager : MonoBehaviour
{
    [SerializeField] private GameObject manual;
    [SerializeField] private Image manualImage;
    [SerializeField] private Sprite[] manualSprites;
    [SerializeField] private Button escButton;
    [SerializeField] private Button progressButton;
    [SerializeField] private Button regressButton;

    private Button manualButton;
    private int currentManualPageNumber = 0;

    private void Start()
    {
        manualButton = GetComponent<Button>();

        manualButton.onClick.AddListener(OpenManual);
        manualButton.onClick.AddListener(SetTransitionButton);

        escButton.onClick.AddListener(CloseManual);

        progressButton.onClick.AddListener(ProgressManual);
        progressButton.onClick.AddListener(SetTransitionButton);

        regressButton.onClick.AddListener(RegressManual);
        regressButton.onClick.AddListener(SetTransitionButton);
    }

    private void Update()
    {
        if (manual.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseManual();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Return))
            {
                ProgressManual();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                RegressManual();
            }
        }
    }

    private void OpenManual()
    {
        currentManualPageNumber = 0;
        manual.SetActive(true);
        manualImage.sprite = manualSprites[currentManualPageNumber];
    }

    private void CloseManual()
    {
        manual.SetActive(false);
    }

    private void ProgressManual()
    {
        currentManualPageNumber++;
        manualImage.sprite = manualSprites[currentManualPageNumber];
    }

    private void RegressManual()
    {
        currentManualPageNumber--;
        manualImage.sprite = manualSprites[currentManualPageNumber];
    }

    private void SetTransitionButton()
    {
        if (currentManualPageNumber == 0)
        {
            regressButton.gameObject.SetActive(false);
        }
        else if (currentManualPageNumber == manualSprites.Length - 1)
        {
            progressButton.gameObject.SetActive(false);
        }
        else
        {
            regressButton.gameObject.SetActive(true);
            progressButton.gameObject.SetActive(true);
        }
    }
}
