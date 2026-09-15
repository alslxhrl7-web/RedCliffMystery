using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 반복 열람 가능한 대화 화제 하나 (예: "그날 밤 행적").
    /// </summary>
    [Serializable]
    public class DialogueTopic
    {
        [Tooltip("코드/저장용 내부 ID. 예: pt_night")]
        public string topicId;

        [Tooltip("화제 목록 버튼에 표시할 문구. 예: \"그날 밤 행적\"")]
        public string buttonLabel;

        [Tooltip("이 조건들을 모두(AND) 만족해야 화제 버튼이 목록에 보입니다. 비워두면 항상 보입니다.")]
        public List<DialogueCondition> visibilityConditions = new List<DialogueCondition>();

        [Tooltip("순서대로 출력되는 대사 목록")]
        public List<DialogueLine> lines = new List<DialogueLine>();

        [Tooltip("이 화제를 처음 볼 때만 적용되는 효과 (플래그 설정, 단서 획득 등)")]
        public List<DialogueEffect> effectsOnFirstView = new List<DialogueEffect>();

        [Tooltip("체크하면, 첫 열람 후에는 화제 목록에서 사라집니다. 실토/최종 반전 같은 1회성 화제에 사용하세요.")]
        public bool hideAfterFirstView;

        public bool IsVisible()
        {
            foreach (var condition in visibilityConditions)
            {
                if (condition != null && !condition.Evaluate())
                    return false;
            }
            return true;
        }
    }
}
