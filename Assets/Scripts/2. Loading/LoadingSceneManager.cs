using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// ���� ���� �ε��ϴ� ���� �ð����� ������ �����ְ�,
/// ������ Ŭ���̾�Ʈ�� ���� �� �ε带 �����ϵ��� Ʈ�����ϴ� ������ �մϴ�.
/// </summary>
public class LoadingSceneManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;

    void Start()
    {
        // 1. �ð����� �ε� �� �ִϸ��̼��� �����մϴ�.
        StartCoroutine(LoadSceneAnimation());

        // 2. [�ٽ�] ���� ������ Ŭ���̾�Ʈ(����)���� ���� ���� �ε��� ������ �����ϴ�.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("������ Ŭ���̾�Ʈ�� BattleScene �ε带 �����մϴ�.");

            // ��� �÷��̾ �� ��(LoadingScene)�� ���� ���� Ȯ���ϱ� ���� ª�� �����̸� �ݴϴ�.
            // 3�ʴ� �ε� �� �ִϸ��̼� �ð��� ����ϰ� ���� ���Դϴ�.
            StartCoroutine(DelayedSceneLoad());
        }
    }

    private IEnumerator DelayedSceneLoad()
    {
        // �ٸ� �÷��̾���� ���� ���� �ð��� �����ݴϴ�.
        yield return new WaitForSeconds(3f);

        // Photon���� "BattleScene"�� �ε��϶�� ����մϴ�.
        // NetworkManager�� AutomaticallySyncScene ���� ���п�, �濡 �ִ� ��� Ŭ���̾�Ʈ�� �Բ� �̵��մϴ�.
        PhotonNetwork.LoadLevel("BattleScene");
    }

    /// <summary>
    /// �ε� �ٸ� ä��� �ð��� ������ ���� �ڷ�ƾ�Դϴ�.
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