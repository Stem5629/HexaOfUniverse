using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button buttonStart;
    [SerializeField] private Button buttonExit;

    [SerializeField] private Button buttonYes;
    [SerializeField] private Button buttonNo;

    [SerializeField] private GameObject alertExit;

    private bool isTryingExit = false;

    private void Start()
    {
        buttonStart.onClick.AddListener(StartGame);
        buttonExit.onClick.AddListener(TryExitGame);

        buttonYes.onClick.AddListener(ExitGame);
        buttonNo.onClick.AddListener(ReturnGame);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.KeypadEnter) && isTryingExit)
        {
            ExitGame();
        }

        if(Input.GetKeyDown(KeyCode.Escape) && isTryingExit)
        {
            ReturnGame();
        }
    }

    private void StartGame()
    {
        SceneManager.LoadScene((int)SceneEnum.Home);
    }

    private void TryExitGame()
    {
        alertExit.SetActive(true);
        isTryingExit = true;
    }
    private void ReturnGame()
    {
        alertExit.SetActive(false);
        isTryingExit = false;
    }

    private void ExitGame()
    {
        Application.Quit();
    }
}
