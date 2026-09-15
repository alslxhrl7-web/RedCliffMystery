using System.Collections.Generic;
using UnityEngine;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 인물 한 명의 대화 데이터 전체(첫 만남 대사 + 화제 목록 + 증거 반응).
    /// Project 창에서 "Create > 적벽추리 > Character Dialogue Data"로 직접 만들 수도 있고,
    /// 메뉴 "적벽추리 > 대사 데이터 자동 생성"으로 기획서 10절 내용을 한 번에 채울 수도 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterDialogue", menuName = "적벽추리/Character Dialogue Data", order = 1)]
    public class CharacterDialogueData : ScriptableObject
    {
        [Header("식별 정보")]
        [Tooltip("met_<characterId> 플래그 등에 쓰이는 내부 ID. 예: pangtong")]
        public string characterId;

        [Tooltip("대화창에 표시될 이름. 예: 방통")]
        public string displayName;

        [Header("첫 만남 전용 대사")]
        public List<DialogueLine> introLines = new List<DialogueLine>();

        [Header("화제 목록 (반복 열람 가능)")]
        public List<DialogueTopic> topics = new List<DialogueTopic>();

        [Header("증거 제시 반응")]
        public List<EvidencePresentation> evidencePresentations = new List<EvidencePresentation>();

        public string MetFlagKey => $"met_{characterId}";
    }
}
