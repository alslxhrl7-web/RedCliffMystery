using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 특정 단서를 보유하고 있을 때 이 인물에게 "증거로 제시"할 수 있는 반응 하나.
    /// </summary>
    [Serializable]
    public class EvidencePresentation
    {
        [Tooltip("이 반응을 일으키는 단서의 clueId (SaveManager.MarkClueCollected에 쓴 값과 동일해야 함)")]
        public string requiredClueId;

        [Tooltip("증거 제시 버튼에 표시할 문구. 비워두면 ClueDatabase에서 이름을 찾아 자동으로 채웁니다.")]
        public string buttonLabelOverride;

        public List<DialogueLine> reactionLines = new List<DialogueLine>();

        public List<DialogueEffect> effects = new List<DialogueEffect>();

        [Tooltip("체크하면 한 번 제시한 뒤에는 같은 반응이 반복되지 않고 짧은 대체 대사로 넘어갑니다. (기본 권장: 켜짐)")]
        public bool oneTimeOnly = true;
    }
}
