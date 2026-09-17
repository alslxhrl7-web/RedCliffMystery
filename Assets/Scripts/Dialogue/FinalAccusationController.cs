using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RedCliffMystery.Save;
using RedCliffMystery.Ending;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 최종 지목(조조 앞 심문 결말) 화면.
    ///
    /// 흐름
    /// 1) 씬 시작 시 조조의 공통 오프닝 대사가 먼저 재생됩니다 (dialogueSet.openingLines).
    /// 2) 지목 대상 선택 버튼(방통/장간/두석/설평/채모·장윤)이 나타납니다.
    /// 3) 방통/장간/설평을 고르면 기존처럼 confirmButton으로 바로 확정합니다.
    /// 4) 두석을 고르면 확정 즉시 전용 컷씬(dialogueSet.dusukDocumentLostLines)이 재생됩니다 —
    ///    거짓 알리바이를 밝혀내는 추리 자체는 맞았지만, 문서 회수에는 실패해 두석 처벌 후에도
    ///    문서가 돌아오지 않고, 라이벌 유성이 그 실패를 근거로 조조에게 책임을 추궁해 플레이어가
    ///    조사에서 배제되는 배드 엔딩(`bad_dusuk_document_lost`)으로 이어집니다.
    /// 5) 채모·장윤을 고르면 지금까지 수집한 단서 중 "증거로 제시할 것"을 체크박스로 고르게 하고,
    ///    제시한 개수에 따라 세 가지 배드 엔딩 컷씬(무죄 추정 없이 지목 / 정황을 증거로 착각 / 처형 후에도
    ///    문서 미발견) 중 하나가 재생된 뒤 엔딩으로 넘어갑니다. 채모·장윤은 어떤 경우에도 진범이 아니므로
    ///    이 갈래는 항상 배드 엔딩으로 귀결되고, 설평이 진범이라는 사실은 어느 경우에도(두석 갈래 포함) 공개하지 않습니다.
    /// </summary>
    public class FinalAccusationController : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private FinalAccusationDialogueSet dialogueSet;
        [SerializeField] private ClueDatabase clueDatabase;

        [Header("설정")]
        [Tooltip("진범으로 인정되는 characterId. CharacterDialogueData.characterId와 일치해야 합니다.")]
        [SerializeField] private string trueCulpritId = "seolpyeong";

        [Tooltip("채모·장윤 지목 시, 제시한 증거 개수가 이 값 이상이면 조조가 납득하고 처형까지 진행됩니다 " +
                 "(CASE 3). 그보다 적으면 CASE 2, 0개면 CASE 1로 처리됩니다.")]
        [SerializeField] private int minimumEvidenceForConviction = 3;

        [Header("UI - 지목 대상 선택")]
        [SerializeField] private GameObject targetSelectionPanel;
        [SerializeField] private Button pangtongButton;
        [SerializeField] private Button jianggangButton;
        [SerializeField] private Button dusukButton;
        [SerializeField] private Button seolpyeongButton;
        [SerializeField] private Button caimaoButton;
        [Tooltip("방통/장간/두석/설평 지목 확정용. 채모·장윤은 아래 증거 제시 패널의 submitEvidenceButton으로 확정합니다.")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text selectedTargetText;

        [Header("UI - 채모·장윤 증거 제시")]
        [SerializeField] private GameObject evidenceSelectionPanel;
        [Tooltip("체크박스(Toggle)가 생성될 부모. Vertical Layout Group을 붙인 빈 오브젝트 권장")]
        [SerializeField] private Transform evidenceToggleListContent;
        [Tooltip("체크박스 하나짜리 프리팹. 자식에 Text(라벨)가 있어야 합니다.")]
        [SerializeField] private Toggle evidenceTogglePrefab;
        [SerializeField] private Button submitEvidenceButton;

        private string _selectedId;
        private readonly List<Toggle> _spawnedEvidenceToggles = new List<Toggle>();

        private void Start()
        {
            if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);
            if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);

            HookButton(pangtongButton, "pangtong", "방통");
            HookButton(jianggangButton, "jianggan", "장간");
            HookButton(dusukButton, "dusuk", "두석");
            HookButton(seolpyeongButton, trueCulpritId, "설평");

            if (caimaoButton != null)
                caimaoButton.onClick.AddListener(SelectCaimaoZhangyunTarget);

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
                confirmButton.onClick.AddListener(ConfirmSimpleAccusation);
            }

            if (submitEvidenceButton != null)
                submitEvidenceButton.onClick.AddListener(SubmitCaimaoEvidence);

            if (DialogueManager.Instance != null && dialogueSet != null && dialogueSet.openingLines.Count > 0)
            {
                DialogueManager.Instance.PlayStandaloneSequence(dialogueSet.openingLines, ShowTargetSelection);
            }
            else
            {
                ShowTargetSelection();
            }
        }

        private void ShowTargetSelection()
        {
            if (targetSelectionPanel != null) targetSelectionPanel.SetActive(true);
        }

        // ------------------------------------------------------------------
        // 방통 / 장간 / 두석 / 설평 — 기존 단순 지목 플로우
        // ------------------------------------------------------------------

        private void HookButton(Button button, string id, string label)
        {
            if (button == null) return;
            button.onClick.AddListener(() => SelectSimpleTarget(id, label));
        }

        private void SelectSimpleTarget(string id, string label)
        {
            _selectedId = id;
            if (selectedTargetText != null) selectedTargetText.text = $"지목 대상: {label}";
            if (confirmButton != null) confirmButton.interactable = true;
            if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);
        }

        private void ConfirmSimpleAccusation()
        {
            if (string.IsNullOrEmpty(_selectedId) || SaveManager.Instance == null) return;

            bool correct = _selectedId == trueCulpritId;
            SaveManager.Instance.SetFlag("final_accusation_correct", correct);
            SaveManager.Instance.SetFlag($"final_accusation_target_{_selectedId}", true);

            // 두석 오인 지목: 거짓 알리바이를 밝혀낸 추리 자체는 맞았지만, 문서를 회수하지
            // 못했다는 실패 때문에 전용 컷씬을 거쳐 배드 엔딩으로 이어집니다.
            if (_selectedId == "dusuk" && !correct)
            {
                SaveManager.Instance.SetFlag("accused_dusuk_document_lost", true);

                if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);

                if (DialogueManager.Instance != null && dialogueSet != null &&
                    dialogueSet.dusukDocumentLostLines != null && dialogueSet.dusukDocumentLostLines.Count > 0)
                {
                    DialogueManager.Instance.PlayStandaloneSequence(dialogueSet.dusukDocumentLostLines, FinishAndTriggerEnding);
                    return;
                }
            }

            FinishAndTriggerEnding();
        }

        // ------------------------------------------------------------------
        // 채모 · 장윤 — 증거 제시 개수에 따른 3가지 배드 엔딩 분기
        // ------------------------------------------------------------------

        private void SelectCaimaoZhangyunTarget()
        {
            _selectedId = "caimaozhangyun";
            if (selectedTargetText != null) selectedTargetText.text = "지목 대상: 채모·장윤";
            if (confirmButton != null) confirmButton.interactable = false;

            BuildEvidenceChecklist();
            if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(true);
        }

        private void BuildEvidenceChecklist()
        {
            foreach (var toggle in _spawnedEvidenceToggles)
            {
                if (toggle != null) Destroy(toggle.gameObject);
            }
            _spawnedEvidenceToggles.Clear();

            if (evidenceTogglePrefab == null || evidenceToggleListContent == null || clueDatabase == null)
            {
                Debug.LogWarning("[FinalAccusationController] evidenceTogglePrefab/evidenceToggleListContent/clueDatabase가 연결되지 않았습니다.");
                return;
            }

            foreach (var info in clueDatabase.clues)
            {
                if (SaveManager.Instance == null || !SaveManager.Instance.IsClueCollected(info.clueId))
                    continue; // 아직 갖고 있지 않은 단서는 애초에 "제시"할 수 없으므로 목록에서 제외

                var toggle = Instantiate(evidenceTogglePrefab, evidenceToggleListContent);
                toggle.gameObject.SetActive(true);
                toggle.isOn = false;

                var label = toggle.GetComponentInChildren<Text>();
                if (label != null) label.text = info.displayName;

                _spawnedEvidenceToggles.Add(toggle);
            }
        }

        private void SubmitCaimaoEvidence()
        {
            int presentedCount = 0;
            foreach (var toggle in _spawnedEvidenceToggles)
            {
                if (toggle != null && toggle.isOn) presentedCount++;
            }

            SaveManager.Instance?.SetFlag("final_accusation_correct", false);
            SaveManager.Instance?.SetFlag("final_accusation_target_caimaozhangyun", true);

            if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);
            if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);

            List<DialogueLine> caseLines;
            string caseFlag;

            if (presentedCount <= 0)
            {
                // CASE 1 — 증거 없이 지목: "성급한 판단"
                caseLines = dialogueSet != null ? dialogueSet.caimaoNoEvidenceLines : null;
                caseFlag = "accused_caimao_no_evidence";
            }
            else if (presentedCount < minimumEvidenceForConviction)
            {
                // CASE 2 — 정황을 증거로 착각: "거짓된 확신"
                caseLines = dialogueSet != null ? dialogueSet.caimaoWrongEvidenceLines : null;
                caseFlag = "accused_caimao_wrong_evidence";
            }
            else
            {
                // CASE 3 — 조조가 납득할 만큼 증거를 모음 → 처형 후에도 문서 미발견: "잘못된 지목"
                caseLines = dialogueSet != null ? dialogueSet.caimaoWrongfulExecutionLines : null;
                caseFlag = "accused_caimao_wrongful_execution";
            }

            SaveManager.Instance?.SetFlag(caseFlag, true);

            if (DialogueManager.Instance != null && caseLines != null && caseLines.Count > 0)
            {
                DialogueManager.Instance.PlayStandaloneSequence(caseLines, FinishAndTriggerEnding);
            }
            else
            {
                FinishAndTriggerEnding();
            }
        }

        // ------------------------------------------------------------------
        // 공통 마무리
        // ------------------------------------------------------------------

        private void FinishAndTriggerEnding()
        {
            DialogueManager.Instance?.EndDialogue();

            if (EndingManager.Instance != null)
            {
                EndingManager.Instance.TriggerEnding();
            }
            else
            {
                Debug.LogError("[FinalAccusationController] EndingManager가 씬에 없습니다.");
            }
        }
    }
}
