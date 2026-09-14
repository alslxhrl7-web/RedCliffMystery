using System.Collections.Generic;
using UnityEngine;

namespace RedCliffMystery.Ending
{
    public enum EndingType
    {
        Bad,     // 실패/오답 엔딩
        Normal,  // 정답이지만 단서를 충분히 못 모은 엔딩
        Good,    // 정답 + 단서 대부분 확보
        True,    // 모든 조건을 만족한 완전한 엔딩
    }

    /// <summary>
    /// 엔딩 하나를 표현하는 데이터 에셋.
    /// 코드를 건드리지 않고 Project 창에서 "Create > 적벽추리 > Ending Definition"으로
    /// 새 엔딩을 계속 추가할 수 있도록 ScriptableObject로 만들었습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnding", menuName = "적벽추리/Ending Definition", order = 0)]
    public class EndingDefinition : ScriptableObject
    {
        [Header("식별 정보")]
        [Tooltip("이 엔딩을 구분하는 고유 ID. 저장 파일(endings_unlocked.json)에도 이 값으로 기록됩니다.")]
        public string endingId;

        public EndingType endingType = EndingType.Normal;

        [Header("화면에 표시할 내용")]
        public string title;

        [TextArea(4, 12)]
        public string bodyText;

        [Tooltip("엔딩 화면 배경 이미지 (선택 사항, 비워도 됨)")]
        public Sprite backgroundImage;

        [Header("성립 조건 (모두 만족해야 이 엔딩이 선택됨)")]
        [Tooltip("조건을 하나도 넣지 않으면 '무조건 성립'으로 취급됩니다. 그래서 다른 조건에 하나도 안 맞을 때를 대비한 기본(배드) 엔딩은 조건을 비워두고 우선순위 목록의 맨 마지막에 둡니다.")]
        public List<EndingCondition> conditions = new List<EndingCondition>();

        public bool AreConditionsMet(RedCliffMystery.Save.SaveManager saveManager)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true; // 조건 없음 = 예비(fallback) 엔딩
            }

            foreach (var condition in conditions)
            {
                if (!condition.IsMet(saveManager))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
