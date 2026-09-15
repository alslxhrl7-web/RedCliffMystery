using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 대화 UI를 화면에 그리는 역할만 담당합니다. 실제 진행 로직은 DialogueManager가 갖고 있고,
    /// 이 컨트롤러는 그 결과를 받아 텍스트/버튼을 갱신하고, 사용자의 클릭을 DialogueManager로 돌려줍니다.
    ///
    /// UnityEngine.UI(레거시 Text/Button)만 사용하므로 TextMeshPro 임포트 없이 바로 동작합니다.
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        [Header("공통")]
        [SerializeField] private GameObject dialoguePanelRoot;

        [Header("대사 표시")]
        [SerializeField] private GameObject linePanel;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text lineText;
        [Tooltip("클릭하면 다음 대사로 넘어가는 버튼 (대사 패널 전체를 덮는 투명 버튼으로 만들어도 됩니다)")]
        [SerializeField] private Button advanceButton;

        [Header("화제/증거 목록")]
        [SerializeField] private GameObject topicMenuPanel;
        [Tooltip("화제 버튼들이 생성될 부모 (VerticalLayoutGroup을 붙인 빈 오브젝트 권장)")]
        [SerializeField] private Transform topicListContent;
        [Tooltip("증거 제시 버튼들이 생성될 부모. 비워두면 topicListContent에 같이 나열됩니다.")]
        [SerializeField] private Transform evidenceListContent;
        [Tooltip("버튼 하나짜리 프리팹. 자식에 Text 컴포넌트가 있어야 합니다.")]
        [SerializeField] private Button topicButtonPrefab;
        [SerializeField] private Button endDialogueButton;

        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

        private void Awake()
        {
            if (advanceButton != null)
                advanceButton.onClick.AddListener(() => DialogueManager.Instance.AdvanceLine());

            if (endDialogueButton != null)
                endDialogueButton.onClick.AddListener(() => DialogueManager.Instance.EndDialogue());

            if (dialoguePanelRoot != null)
                dialoguePanelRoot.SetActive(false);
        }

        public void OpenDialoguePanel(string characterDisplayName)
        {
            if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(true);
        }

        public void CloseDialoguePanel()
        {
            ClearSpawnedButtons();
            if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(false);
        }

        public void ShowLine(string speaker, string text)
        {
            if (topicMenuPanel != null) topicMenuPanel.SetActive(false);
            if (linePanel != null) linePanel.SetActive(true);

            if (speakerNameText != null) speakerNameText.text = speaker;
            if (lineText != null) lineText.text = text;
        }

        public void ShowTopicMenu(List<DialogueTopic> topics, List<EvidencePresentation> evidences, ClueDatabase clueDatabase)
        {
            if (linePanel != null) linePanel.SetActive(false);
            if (topicMenuPanel != null) topicMenuPanel.SetActive(true);

            ClearSpawnedButtons();

            if (topicButtonPrefab == null || topicListContent == null) return;

            foreach (var topic in topics)
            {
                var capturedTopic = topic;
                var btn = Instantiate(topicButtonPrefab, topicListContent);
                btn.gameObject.SetActive(true);
                SetButtonLabel(btn, capturedTopic.buttonLabel);
                btn.onClick.AddListener(() => DialogueManager.Instance.SelectTopic(capturedTopic));
                _spawnedButtons.Add(btn.gameObject);
            }

            var evidenceParent = evidenceListContent != null ? evidenceListContent : topicListContent;
            foreach (var ep in evidences)
            {
                var capturedEp = ep;
                var btn = Instantiate(topicButtonPrefab, evidenceParent);
                btn.gameObject.SetActive(true);
                string label = string.IsNullOrEmpty(capturedEp.buttonLabelOverride)
                    ? $"[증거 제시] {(clueDatabase != null ? clueDatabase.GetDisplayName(capturedEp.requiredClueId) : capturedEp.requiredClueId)}"
                    : capturedEp.buttonLabelOverride;
                SetButtonLabel(btn, label);
                btn.onClick.AddListener(() => DialogueManager.Instance.SelectEvidence(capturedEp));
                _spawnedButtons.Add(btn.gameObject);
            }
        }

        private void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        private void ClearSpawnedButtons()
        {
            foreach (var go in _spawnedButtons)
            {
                if (go != null) Destroy(go);
            }
            _spawnedButtons.Clear();
        }
    }
}
