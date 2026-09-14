using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RedCliffMystery.Save;

namespace RedCliffMystery.UI
{
    /// <summary>
    /// 새 게임을 시작할 때 플레이어가 주인공 이름을 직접 입력하는 화면.
    /// 타이틀 씬(또는 "새 게임" 전용 씬)에 배치해서 사용합니다.
    ///
    /// 전제 조건: 이 씬에 SaveManager가 이미 있어야 합니다 (SaveManager.Instance가 null이 아니어야 함).
    /// 보통 타이틀 씬에 SaveManager를 두고, 그 위에 이 스크립트가 붙은 이름 입력 UI를 함께 두면 됩니다.
    ///
    /// 특수 문자(TextMeshPro) 없이 UnityEngine.UI만 사용해서 별도 임포트 없이 바로 동작합니다.
    /// </summary>
    public class PlayerNameInputUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [Tooltip("이름을 입력받는 입력창")]
        [SerializeField] private InputField nameInputField;

        [Tooltip("입력 확정(게임 시작) 버튼")]
        [SerializeField] private Button confirmButton;

        [Tooltip("안내 문구를 보여줄 텍스트 (선택 사항)")]
        [SerializeField] private Text hintText;

        [Header("설정")]
        [Tooltip("이름 최대 길이")]
        [SerializeField] private int maxNameLength = 12;

        [Tooltip("이름을 확정하면 이동할 게임 플레이 씬 이름")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        private void Awake()
        {
            if (nameInputField != null)
            {
                nameInputField.characterLimit = maxNameLength;
            }

            if (hintText != null)
            {
                hintText.text = $"이름을 입력하세요 (비워두면 \"{SaveManager.DefaultPlayerName}\"으로 시작합니다)";
            }
        }

        private void Start()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirm);
            }

            // 엔터 키로도 확정할 수 있도록 연결 (선택 사항)
            if (nameInputField != null)
            {
                nameInputField.onEndEdit.AddListener(OnEndEdit);
            }
        }

        private void OnEndEdit(string value)
        {
            // 엔터 키로 종료됐을 때만 확정 처리 (탭 이동 등 다른 종료 사유는 제외하고 싶다면
            // Input System의 Keyboard.current.enterKey.wasPressedThisFrame 등을 추가로 확인하세요)
            OnConfirm();
        }

        private void OnConfirm()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[PlayerNameInputUI] 씬에 SaveManager가 없습니다. SaveManager를 먼저 배치해주세요.");
                return;
            }

            string typedName = nameInputField != null ? nameInputField.text : string.Empty;
            SaveManager.Instance.SetPlayerName(typedName);

            Debug.Log($"[PlayerNameInputUI] 주인공 이름 확정: {SaveManager.Instance.GetPlayerName()}");

            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
