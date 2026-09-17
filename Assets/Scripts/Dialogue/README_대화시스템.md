# 심문/대화 시스템 사용법

사건기획서_v1.md 10절에서 설계한 대사 트리를 실제로 동작하게 만든 시스템입니다.
저장 시스템(`SaveManager`)과 엔딩 시스템(`EndingManager`)에 바로 연결되도록 만들었습니다.

## 구성 파일

| 파일 | 역할 |
|---|---|
| `DialogueLine.cs` | 대사 한 줄 (화자 + 텍스트) |
| `DialogueCondition.cs` | 화제/증거가 보이는 조건 (플래그/단서 기반) |
| `DialogueEffect.cs` | 화제/증거를 본 뒤 적용되는 효과 (플래그 설정, 단서 획득) |
| `DialogueTopic.cs` | 반복 열람 가능한 화제 하나 |
| `EvidencePresentation.cs` | 특정 단서를 제시했을 때의 반응 |
| `CharacterDialogueData.cs` | 인물 한 명의 대화 데이터 전체 (ScriptableObject) |
| `ClueDatabase.cs` | 단서 ID → 표시 이름 매핑 (증거 제시 버튼 표시용) |
| `DialogueManager.cs` | 대화 진행을 담당하는 싱글턴 |
| `DialogueUIController.cs` | 대화 UI를 그리는 컨트롤러 |
| `NPCInteractable.cs` | NPC에 붙여서 상호작용(E키)으로 대화를 여는 컴포넌트 |
| `FinalAccusationController.cs` | 최종 지목 화면. 확정하면 `EndingManager.TriggerEnding()` 호출 |
| `FinalAccusationDialogueSet.cs` | 최종 지목 씬의 오프닝 대사 + 채모·장윤 배드 엔딩 3종 컷씬 대사 (ScriptableObject) |
| `../Editor/DialogueDataBootstrapper.cs` | 기획서 10절 대사와 최종 지목 컷씬·배드 엔딩 3종을 애셋으로 자동 생성해주는 편집기 도구 |

## 설계 요약

- 대화는 **화제 선택형**(반복 열람 가능) + **증거 제시형**(보유한 단서를 눌러서 반응을 이끌어내는 방식, Ace Attorney류) 두 가지를 섞었습니다.
- 조건/효과는 전부 `SaveManager`의 플래그(`SetFlag`/`GetFlag`)와 단서 수집(`MarkClueCollected`/`IsClueCollected`)만 사용합니다. 그래서 저장/로드해도 대화 진행 상황이 그대로 유지되고, `EndingCondition`에서도 같은 플래그를 바로 참조할 수 있습니다.
- 각 화제/증거를 "처음 봤는지"는 `topic_seen_<characterId>_<topicId>`, `evidence_presented_<characterId>_<clueId>` 라는 이름의 플래그로 자동 기록됩니다. 직접 신경 쓸 필요 없습니다.

## 에디터 설정 순서

1. **대사 데이터 자동 생성**
   - 상단 메뉴에서 **적벽추리 > 대사 데이터 자동 생성**을 실행합니다.
   - `Assets/Data/Dialogue/` 폴더에 `ClueDatabase.asset`, `Dialogue_방통/장간/두석/설평/유성.asset`, `FinalAccusationDialogueSet.asset`이 생성됩니다.
   - `Assets/Data/Endings/` 폴더에 채모·장윤 배드 엔딩 3종(`bad_caimao_no_evidence.asset`, `bad_caimao_wrong_evidence.asset`, `bad_caimao_wrongful_execution.asset`)이 생성됩니다.
   - 이 애셋들은 기획서 10절의 대사와, 최종 지목에서 채모·장윤을 지목했을 때의 컷씬 대사를 그대로 담고 있습니다. 내용을 수정하고 싶으면 인스펙터에서 직접 고치거나, `DialogueDataBootstrapper.cs`를 고쳐서 다시 실행하면 됩니다(기존 애셋을 덮어씁니다).
   - **수동 연결 필요**: 새로 생긴 배드 엔딩 3종은 자동으로 `EndingManager`에 등록되지 않습니다. `EndingManager`가 붙은 오브젝트를 선택하고, 인스펙터의 "엔딩 목록" 리스트에 이 3개 애셋을 드래그해 추가해주세요. 이 셋은 서로 조건이 겹치지 않으므로 순서는 상관없지만, 조건이 없는 기본 배드 엔딩(목록 맨 아래)보다는 위에 있어야 합니다.

2. **DialogueManager 배치**
   - `SaveManager`가 있는 씬(보통 타이틀/메인 씬)에 빈 GameObject `DialogueManager`를 만들고 `DialogueManager.cs`를 붙입니다.
   - 인스펙터의 `Clue Database` 필드에 방금 만든 `ClueDatabase.asset`을 연결합니다.
   - `Ui` 필드는 3번에서 만들 `DialogueUIController`를 연결합니다.

3. **대화 UI 구성 (Canvas)**
   - Canvas 안에 대략 이런 계층 구조를 만듭니다.
     ```
     Canvas
       DialoguePanelRoot (기본 비활성)
         LinePanel
           SpeakerNameText (Text)
           LineText (Text)
           AdvanceButton (Button, 화면 클릭 시 다음 대사로)
         TopicMenuPanel
           TopicListContent (빈 오브젝트 + Vertical Layout Group)
           EvidenceListContent (선택 사항, 비워두면 TopicListContent에 같이 나열됨)
           EndDialogueButton (Button, "대화 종료")
     ```
   - 버튼 하나짜리 프리팹(자식에 Text 포함)을 만들어 `TopicButtonPrefab`으로 저장해둡니다.
   - 같은 Canvas(또는 빈 GameObject)에 `DialogueUIController.cs`를 붙이고 위 오브젝트들을 인스펙터에 전부 연결합니다.

4. **NPC 배치**
   - 각 NPC 오브젝트에 `NPCInteractable.cs`를 붙이고 `Dialogue Data`에 해당 인물의 애셋(`Dialogue_방통.asset` 등)을 연결합니다.
   - Collider가 자동으로 붙고 `Is Trigger`가 켜집니다 (`Reset()`에서 처리). 크기를 상호작용 반경에 맞게 조절하세요.
   - 플레이어 오브젝트에 태그 **Player**가 지정되어 있어야 합니다.

5. **최종 지목 화면**
   - 이 씬(또는 UI)이 열리면 먼저 `FinalAccusationDialogueSet.openingLines`(조조: "조사를 끝냈다고 들었다...")가 재생된 뒤 지목 대상 버튼들이 나타나는 구조입니다. 그래서 이 UI는 `DialogueManager`/`DialogueUIController`와 **같은 씬**에 있어야 합니다 — 최종 지목을 별도 씬으로 분리하고 싶다면 `DialogueManager`가 붙은 오브젝트(와 그 UI 캔버스)를 `DontDestroyOnLoad`로 만들어 씬이 바뀌어도 유지되게 하세요.
   - UI 구성:
     - `TargetSelectionPanel` (기본 비활성): 방통/장간/두석/설평/**채모·장윤** 버튼 5개 + 확인 버튼(`ConfirmButton`, 방통/장간/두석/설평 전용) + 선택 표시 텍스트
     - `EvidenceSelectionPanel` (기본 비활성): 체크박스(Toggle) 목록이 생성될 `EvidenceToggleListContent` + 체크박스 프리팹(`EvidenceTogglePrefab`, 자식에 Text 라벨 필요) + `SubmitEvidenceButton`("증거 제시 확정" 등)
   - `FinalAccusationController.cs`를 붙이고 `Dialogue Set`(`FinalAccusationDialogueSet.asset`), `Clue Database`(`ClueDatabase.asset`), 위 UI 오브젝트들을 전부 인스펙터에 연결합니다. `True Culprit Id`는 기본값 `seolpyeong` 그대로 두면 됩니다.
   - **방통/장간/두석/설평**: 버튼 클릭 → `ConfirmButton`으로 확정 → `final_accusation_correct` 플래그 설정 → 곧바로 엔딩으로 이어집니다 (기존과 동일).
   - **채모·장윤**: 버튼 클릭 → 지금까지 수집한 단서 중 보유한 것만 체크박스로 나타남 → 플레이어가 "증거로 제시할" 단서를 몇 개 고르고 `SubmitEvidenceButton` 클릭 → 체크한 개수에 따라 세 가지 컷씬 중 하나가 재생된 뒤 해당 배드 엔딩으로 이어집니다.
       - 0개 선택 → `bad_caimao_no_evidence` ("성급한 판단")
       - 1개 ~ (`Minimum Evidence For Conviction` - 1)개 → `bad_caimao_wrong_evidence` ("거짓된 확신"), 기본 임계값은 3
       - `Minimum Evidence For Conviction`개 이상 → `bad_caimao_wrongful_execution` ("잘못된 지목" — 처형 후에도 문서가 발견되지 않고, 며칠 뒤 화공으로 조조군이 대패하는 결말. 어떤 경우에도 설평이 진범이라는 사실은 밝히지 않습니다)
   - 채모·장윤 쪽은 실제로 어떤 단서를 제시하든 관계없이 결과가 정해집니다(진범은 설평이므로 애초에 결정적 증거가 존재하지 않음) — 그래서 "제시한 개수"만으로 판정하도록 단순화했습니다. 특정 단서 조합에 따라 결과를 더 세분화하고 싶다면 `SubmitCaimaoEvidence()`에서 개수 대신 어떤 clueId가 체크됐는지를 검사하도록 확장하면 됩니다.

## 테스트 방법

1. Play 모드에서 NPC에게 다가가 E를 눌러 대화가 열리는지 확인합니다.
2. 처음 만났을 때는 인트로 대사가 먼저 나오고, 그 다음부터는 화제 목록만 나오는지 확인합니다.
3. 단서를 아직 못 모은 상태에서는 안 보이던 화제/증거 버튼이, `SaveManager.Instance.MarkClueCollected("clue_...")`를 디버그로 호출한 뒤에는 나타나는지 확인합니다.
4. 설평의 "[결정적 심문]" 화제는 `clue_household_registry_no_record`와 `clue_wu_secret_letter_true_name`을 **둘 다** 모아야만 나타납니다.
5. F5/F9로 저장·로드한 뒤에도 화제 열람 여부, 증거 제시 여부, 인물별 clear 상태가 그대로 유지되는지 확인합니다 (전부 SaveManager 플래그 기반이라 자동으로 유지됩니다).
6. 최종 지목 화면에서 채모·장윤을 골라 체크박스를 0개/1~2개/3개 이상 선택해가며 세 번 확정해보고, 각각 다른 컷씬과 다른 배드 엔딩(`bad_caimao_no_evidence`/`bad_caimao_wrong_evidence`/`bad_caimao_wrongful_execution`)으로 이어지는지 확인합니다. 세 경우 모두 설평이라는 이름이 대사에 등장하지 않아야 정상입니다.

## 알려진 제약 / 다음에 고려할 점

- 증거 제시 목록은 "이 인물과 관련된 단서 중 보유한 것"만 보여줍니다. 아무 단서나 골라서 제시했다가 "그건 상관없는 것 같다" 같은 반응을 받는 완전한 자유 제시 방식은 아직 없습니다. 필요하면 `DialogueManager.ShowTopicMenu()`에 전체 단서 인벤토리를 넘기고, `EvidencePresentation`에 없는 clueId를 제시했을 때의 공용 대사를 추가하는 식으로 확장할 수 있습니다.
- 설평이 정체를 들킨 뒤(`seolpyeong_exposed = true`)에도 "문서고 관리"/"고향/이름" 화제의 대사는 바뀌지 않습니다. 노출 후 전용 대사를 넣고 싶다면 해당 `DialogueTopic`에 `visibilityConditions`로 `seolpyeong_exposed = false`인 버전과, `true`인 버전 두 개를 만들어 교체하면 됩니다.
- 곽참모(방통의 알리바이 증인) NPC는 아직 별도로 만들지 않았습니다. `pangtong_alibi_lead` 플래그가 켜진 뒤에만 등장/대화 가능하게 만들고, 대화 끝에 `clue_pangtong_alibi_witness` 단서 획득 + `pangtong_cleared` 플래그를 설정하도록 새 `CharacterDialogueData`를 하나 더 만들어주면 됩니다 (기존 패턴 그대로 재사용 가능).
