using System.Collections.Generic;
using UnityEngine;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 최종 지목(조조의 막사) 장면에서 재생되는 대사 세트.
    ///
    /// 채모·장윤을 지목했을 때는 플레이어가 "증거로 제시"한 단서의 개수에 따라
    /// 세 가지 배드 엔딩 컷씬 중 하나가 재생됩니다 (실제로는 무엇을 제시하든 결국 오답입니다 —
    /// 진범은 설평이기 때문입니다). 어느 경우에도 설평이 진범이라는 사실은 밝히지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "FinalAccusationDialogueSet", menuName = "적벽추리/Final Accusation Dialogue Set", order = 3)]
    public class FinalAccusationDialogueSet : ScriptableObject
    {
        [Header("공통 오프닝 (지목 대상을 고르기 전, 조조가 먼저 묻는 부분)")]
        public List<DialogueLine> openingLines = new List<DialogueLine>();

        [Header("채모·장윤 지목 — CASE 1: 증거 없이 지목 (성급한 판단)")]
        public List<DialogueLine> caimaoNoEvidenceLines = new List<DialogueLine>();

        [Header("채모·장윤 지목 — CASE 2: 불충분한/잘못된 증거 (거짓된 확신)")]
        public List<DialogueLine> caimaoWrongEvidenceLines = new List<DialogueLine>();

        [Header("채모·장윤 지목 — CASE 3: 조조가 납득할 만큼 증거를 모음 → 처형 후에도 문서가 발견되지 않음 (잘못된 지목)")]
        public List<DialogueLine> caimaoWrongfulExecutionLines = new List<DialogueLine>();
    }
}
