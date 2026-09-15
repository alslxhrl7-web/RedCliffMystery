using UnityEngine;
using UnityEngine.UI;
using RedCliffMystery.Save;
using RedCliffMystery.Ending;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 최종 지목(조조 앞 심문 결말) 화면. 용의자를 선택하고 확정하면
    /// SaveManager에 "final_accusation_correct" 플래그를 설정한 뒤 EndingManager로 엔딩을 판정합니다.
    ///
    /// 진범 판정: 오직 trueCulpritId(기본값 "seolpyeong")를 지목했을 때만 final_accusation_correct = true.
    /// 트루/굿/배드의 세부 구분은 그 뒤 EndingDefinition의 조건(사건기획서 5-2절, 6절 참고)에서 처리됩니다.
    /// </summary>
    public class FinalAccusationController : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("진범으로 인정되는 characterId. CharacterDialogueData.characterId와 일치해야 합니다.")]
        [SerializeField] private string trueCulpritId = "seolpyeong";

        [Header("UI")]
        [SerializeField] private Button pangtongButton;
        [SerializeField] private Button jianggangButton;
        [SerializeField] private Button dusukButton;
        [SerializeField] private Button seolpyeongButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text selectedTargetText;

        private string _selectedId;

        private void Start()
        {
            HookButton(pangtongButton, "pangtong", "방통");
            HookButton(jianggangButton, "jianggan", "장간");
            HookButton(dusukButton, "dusuk", "두석");
            HookButton(seolpyeongButton, trueCulpritId, "설평");

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
                confirmButton.onClick.AddListener(ConfirmAccusation);
            }
        }

        private void HookButton(Button button, string id, string label)
        {
            if (button == null) return;
            button.onClick.AddListener(() => SelectTarget(id, label));
        }

        private void SelectTarget(string id, string label)
        {
            _selectedId = id;
            if (selectedTargetText != null) selectedTargetText.text = $"지목 대상: {label}";
            if (confirmButton != null) confirmButton.interactable = true;
        }

        private void ConfirmAccusation()
        {
            if (string.IsNullOrEmpty(_selectedId) || SaveManager.Instance == null) return;

            bool correct = _selectedId == trueCulpritId;
            SaveManager.Instance.SetFlag("final_accusation_correct", correct);
            SaveManager.Instance.SetFlag($"final_accusation_target_{_selectedId}", true);

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
