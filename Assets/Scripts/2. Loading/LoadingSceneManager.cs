using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// 다음 씬을 로드하는 동안 시각적인 연출을 보여주고,
/// 마스터 클라이언트가 실제 씬 로드를 시작하도록 트리거하는 역할을 합니다.
/// </summary>
public class LoadingSceneManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;

    void Start()
    {
        // 1. 시각적인 로딩 바 애니메이션을 시작합니다.
        StartCoroutine(LoadSceneAnimation());

        // 2. [핵심] 오직 마스터 클라이언트(방장)만이 다음 씬을 로드할 권한을 가집니다.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("마스터 클라이언트가 BattleScene 로드를 시작합니다.");

            // 모든 플레이어가 이 씬(LoadingScene)에 들어온 것을 확인하기 위해 짧은 딜레이를 줍니다.
            // 3초는 로딩 바 애니메이션 시간과 비슷하게 맞춘 것입니다.
            StartCoroutine(DelayedSceneLoad());
        }
    }

    private IEnumerator DelayedSceneLoad()
    {
        // 다른 플레이어들이 씬에 들어올 시간을 벌어줍니다.
        yield return new WaitForSeconds(3f);

        // Photon에게 "BattleScene"을 로드하라고 명령합니다.
        // NetworkManager의 AutomaticallySyncScene 설정 덕분에, 방에 있는 모든 클라이언트가 함께 이동합니다.
        PhotonNetwork.LoadLevel("BattleScene");
    }

    /// <summary>
    /// 로딩 바를 채우는 시각적 연출을 위한 코루틴입니다.
    /// </summary>
    private IEnumerator LoadSceneAnimation()
    {
        float timer = 0f;
        float loadTime = 3.0f;

        loadingText.text = "Loading...";

        while (timer < loadTime)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / loadTime);
            loadingBar.value = progress;
            yield return null;
        }

        loadingText.text = "Loading Complete!";
    }
}