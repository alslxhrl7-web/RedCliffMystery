using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.Ending
{
    /// <summary>
    /// 엔딩 전용 씬(예: "Ending" 씬)에 배치해서 사용하는 UI 컨트롤러.
    /// EndingManager가 골라준 EndingDefinition의 내용을 화면에 표시하고,
    /// 다시 시작하기 / 타이틀로 나가기 버튼을 처리합니다.
    ///
    /// 특수 문자(TextMeshPro)를 쓰지 않고 UnityEngine.UI.Text만 사용해서,
    /// 별도의 TMP Essentials 임포트 없이 바로 동작하도록 만들었습니다.
    /// 나중에 TextMeshPro로 바꾸고 싶다면 Text 필드 타입만
    /// TMPro.TextMeshProUGUI로 교체하면 됩니다.
    /// </summary>
    public class EndingUIController : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitToTitleButton;

        [Header("타이핑 연출 (선택)")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float secondsPerCharacter = 0.03f;

        [Header("씬 이동 대상")]
        [Tooltip("'다시 시작하기' 버튼을 눌렀을 때 이동할 게임 플레이 씬 이름")]
        [SerializeField] private string gameplaySceneName = "SampleScene";
        [Tooltip("'타이틀로' 버튼을 눌렀을 때 이동할 타이틀 씬 이름")]
        [SerializeField] private string titleSceneName = "Title";

        [Header("이 씬을 단독으로 테스트할 때 사용할 엔딩 (선택)")]
        [Tooltip("EndingManager를 거치지 않고 이 씬만 단독 재생했을 때 보여줄 엔딩입니다. 실제 플레이 흐름에서는 무시됩니다.")]
        [SerializeField] private EndingDefinition previewEndingForStandaloneTesting;

        private void Start()
        {
            EndingDefinition ending = null;

            if (EndingManager.Instance != null && EndingManager.Instance.CurrentEnding != null)
            {
                ending = EndingManager.Instance.CurrentEnding;
            }
            else if (previewEndingForStandaloneTesting != null)
            {
                // EndingManager 없이 이 씬만 Play해서 UI를 확인할 때 사용
                ending = previewEndingForStandaloneTesting;
            }

            if (ending == null)
            {
                Debug.LogWarning("[EndingUIController] 표시할 엔딩 데이터가 없습니다.");
                if (titleText != null) titleText.text = "(엔딩 데이터 없음)";
                if (bodyText != null) bodyText.text = string.Empty;
                return;
            }

            Show(ending);

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnClickRestart);
            }
            if (quitToTitleButton != null)
            {
                quitToTitleButton.onClick.AddListener(OnClickQuitToTitle);
            }
        }

        private void Show(EndingDefinition ending)
        {
            if (titleText != null) titleText.text = ending.title;

            if (backgroundImage != null)
            {
                if (ending.backgroundImage != null)
                {
                    backgroundImage.sprite = ending.backgroundImage;
                    backgroundImage.enabled = true;
                }
                else
                {
                    backgroundImage.enabled = false;
                }
            }

            if (bodyText != null)
            {
                if (useTypewriterEffect)
                {
                    StopAllCoroutines();
                    StartCoroutine(TypewriterRoutine(ending.bodyText));
                }
                else
                {
                    bodyText.text = ending.bodyText;
                }
            }
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            bodyText.text = string.Empty;
            var wait = new WaitForSeconds(secondsPerCharacter);

            for (int i = 0; i < fullText.Length; i++)
            {
                bodyText.text += fullText[i];
                yield return wait;
            }
        }

        private void OnClickRestart()
        {
            if (Save.SaveManager.Instance != null)
            {
                Save.SaveManager.Instance.ResetProgress();
            }
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnClickQuitToTitle()
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }
}
