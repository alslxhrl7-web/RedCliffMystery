using System;
using System.Collections.Generic;
using UnityEngine;
using RedCliffMystery.Save;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 대화(심문) 시스템 전체를 관리하는 싱글턴.
    ///
    /// 사용 방법
    /// 1) 씬(예: 타이틀/메인 씬)에 빈 GameObject를 만들어 "DialogueManager"라 이름 짓고
    ///    이 스크립트를 붙인 뒤, 인스펙터에 DialogueUIController와 ClueDatabase를 연결합니다.
    /// 2) NPC 오브젝트에는 NPCInteractable을 붙이고 CharacterDialogueData를 연결합니다.
    /// 3) 플레이어가 NPC와 상호작용하면 NPCInteractable이 StartDialogue(data)를 호출합니다.
    ///
    /// SaveManager/EndingManager와 마찬가지로 씬이 바뀌어도 살아남는 싱글턴입니다.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("연결")]
        [SerializeField] private DialogueUIController ui;
        [SerializeField] private ClueDatabase clueDatabase;

        public CharacterDialogueData CurrentCharacter { get; private set; }
        public bool IsDialogueActive => CurrentCharacter != null;

        private List<DialogueLine> _activeLines;
        private int _lineIndex;
        private Action _onLineSequenceFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ------------------------------------------------------------------
        // 대화 시작/종료
        // ------------------------------------------------------------------

        public void StartDialogue(CharacterDialogueData data)
        {
            if (data == null || ui == null)
            {
                Debug.LogError("[DialogueManager] CharacterDialogueData 또는 DialogueUIController가 연결되지 않았습니다.");
                return;
            }

            CurrentCharacter = data;
            ui.OpenDialoguePanel(data.displayName);

            bool alreadyMet = SaveManager.Instance != null && SaveManager.Instance.GetFlag(data.MetFlagKey, false);

            if (!alreadyMet && data.introLines.Count > 0)
            {
                PlayLines(data.introLines, () =>
                {
                    SaveManager.Instance?.SetFlag(data.MetFlagKey, true);
                    ShowTopicMenu();
                });
            }
            else
            {
                SaveManager.Instance?.SetFlag(data.MetFlagKey, true);
                ShowTopicMenu();
            }
        }

        public void EndDialogue()
        {
            CurrentCharacter = null;
            _activeLines = null;
            _onLineSequenceFinished = null;
            ui.CloseDialoguePanel();
        }

        // ------------------------------------------------------------------
        // 대사 재생
        // ------------------------------------------------------------------

        private void PlayLines(List<DialogueLine> lines, Action onFinished)
        {
            _activeLines = lines;
            _lineIndex = 0;
            _onLineSequenceFinished = onFinished;
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (_activeLines == null || _lineIndex >= _activeLines.Count)
            {
                var callback = _onLineSequenceFinished;
                _onLineSequenceFinished = null;
                _activeLines = null;
                callback?.Invoke();
                return;
            }

            var line = _activeLines[_lineIndex];

            // speakerName이 비어있으면 플레이어 본인의 대사로 취급하고, 저장된 주인공 이름을 화자로 씁니다.
            string speaker = string.IsNullOrEmpty(line.speakerName)
                ? (SaveManager.Instance != null ? SaveManager.Instance.GetPlayerName() : "나")
                : line.speakerName;

            ui.ShowLine(speaker, line.text);
        }

        /// <summary>대사창의 "다음" 버튼(또는 클릭)에서 호출합니다.</summary>
        public void AdvanceLine()
        {
            if (!IsShowingLines) return;
            _lineIndex++;
            ShowCurrentLine();
        }

        public bool IsShowingLines => _activeLines != null;

        // ------------------------------------------------------------------
        // 화제 목록 / 증거 제시
        // ------------------------------------------------------------------

        public void ShowTopicMenu()
        {
            _activeLines = null;

            var visibleTopics = new List<DialogueTopic>();
            foreach (var topic in CurrentCharacter.topics)
            {
                bool hiddenBySeen = topic.hideAfterFirstView && GetTopicSeen(topic.topicId);
                if (topic.IsVisible() && !hiddenBySeen)
                {
                    visibleTopics.Add(topic);
                }
            }

            var presentableEvidence = new List<EvidencePresentation>();
            foreach (var ep in CurrentCharacter.evidencePresentations)
            {
                if (SaveManager.Instance != null && SaveManager.Instance.IsClueCollected(ep.requiredClueId))
                {
                    presentableEvidence.Add(ep);
                }
            }

            ui.ShowTopicMenu(visibleTopics, presentableEvidence, clueDatabase);
        }

        public void SelectTopic(DialogueTopic topic)
        {
            bool firstView = !GetTopicSeen(topic.topicId);

            PlayLines(topic.lines, () =>
            {
                if (firstView)
                {
                    foreach (var effect in topic.effectsOnFirstView) effect.Apply();
                    SetTopicSeen(topic.topicId, true);
                }
                ShowTopicMenu();
            });
        }

        public void SelectEvidence(EvidencePresentation ep)
        {
            string characterId = CurrentCharacter.characterId;
            bool alreadyPresented = GetEvidencePresented(characterId, ep.requiredClueId);

            if (ep.oneTimeOnly && alreadyPresented)
            {
                var fallback = new List<DialogueLine>
                {
                    new DialogueLine(CurrentCharacter.displayName, "...그 얘기는 이미 다 하지 않았나.")
                };
                PlayLines(fallback, ShowTopicMenu);
                return;
            }

            PlayLines(ep.reactionLines, () =>
            {
                if (!alreadyPresented)
                {
                    foreach (var effect in ep.effects) effect.Apply();
                    SetEvidencePresented(characterId, ep.requiredClueId, true);
                }
                ShowTopicMenu();
            });
        }

        // ------------------------------------------------------------------
        // 화제/증거 열람 여부 (SaveManager의 범용 플래그를 그대로 활용 -> 자동으로 저장/로드됨)
        // ------------------------------------------------------------------

        private string TopicSeenKey(string topicId) => $"topic_seen_{CurrentCharacter.characterId}_{topicId}";
        private bool GetTopicSeen(string topicId) => SaveManager.Instance != null && SaveManager.Instance.GetFlag(TopicSeenKey(topicId), false);
        private void SetTopicSeen(string topicId, bool value) => SaveManager.Instance?.SetFlag(TopicSeenKey(topicId), value);

        private string EvidencePresentedKey(string characterId, string clueId) => $"evidence_presented_{characterId}_{clueId}";
        private bool GetEvidencePresented(string characterId, string clueId) => SaveManager.Instance != null && SaveManager.Instance.GetFlag(EvidencePresentedKey(characterId, clueId), false);
        private void SetEvidencePresented(string characterId, string clueId, bool value) => SaveManager.Instance?.SetFlag(EvidencePresentedKey(characterId, clueId), value);
    }
}
