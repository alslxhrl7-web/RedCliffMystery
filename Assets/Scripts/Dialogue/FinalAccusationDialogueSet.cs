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
    ///
    /// 두석을 지목했을 때는 "추리 자체(거짓 알리바이 적발)는 옳았지만, 정작 사라진 문서를
    /// 회수하지 못해 배드 엔딩으로 이어지는" 전용 컷씬이 재생됩니다. 두석은 처벌되지만 진범이
    /// 아니므로 문서는 끝내 발견되지 않고, 라이벌 유성이 그 실패를 근거로 플레이어를 조사에서
    /// 배제시킵니다. 이 경우에도 설평이 진범이라는 사실은 밝히지 않습니다.
    ///
    /// 설평(진범)을 정확히 지목했지만 핵심 단서를 전부 모으지는 못했을 때는 노멀 엔딩 컷씬이
    /// 재생됩니다 — 설평은 체포되지만 화공에 대한 결정적인 대비책까지는 마련하지 못해, 며칠 뒤
    /// 적벽에서 역사대로 조조군이 화공으로 패배합니다. 핵심 단서를 전부 모았다면(트루 엔딩) 이
    /// 컷씬은 재생되지 않고 곧바로 엔딩으로 넘어갑니다.
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

        [Header("두석 지목 — 오인 지목 배드 엔딩 (알리바이 거짓말은 맞게 짚었으나 문서 회수 실패)")]
        public List<DialogueLine> dusukDocumentLostLines = new List<DialogueLine>();

        [Header("설평 정답 지목 — 노멀 엔딩 (범인은 잡았으나 화공 대비 실패, 트루 엔딩 미달 시 재생)")]
        public List<DialogueLine> normalEndingLines = new List<DialogueLine>();
    }
}
