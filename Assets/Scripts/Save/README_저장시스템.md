# 저장/로드 시스템 사용법

이 폴더의 스크립트는 외부 패키지 없이 Unity 기본 `JsonUtility`만으로 동작하는
저장/로드 시스템입니다. 파일은 `Application.persistentDataPath/Saves/` 아래
`save_slot_{번호}.json` 형태로 저장됩니다.

## 구성 파일
- `SaveData.cs` : 저장되는 데이터 구조 (씬 이름, 플레이어 위치/회전, 수집한 단서, 진행 플래그)
- `ISaveable.cs` : 저장/복원 대상 오브젝트가 구현하는 인터페이스
- `SaveManager.cs` : 저장/로드를 총괄하는 싱글턴 매니저
- `PlayerSaveData.cs` : 플레이어 위치/회전을 저장하는 컴포넌트
- `SaveableClue.cs` : 단서(증거) 오브젝트 예시 (테스트 겸 향후 단서 수집 시스템의 기반)
- `SaveLoadHotkeys.cs` : F5 저장 / F9 로드 / F6 삭제+초기화 테스트용 단축키

## 에디터에서 설정하는 순서

1. **SaveManager 배치**
   - 게임이 시작되는 첫 씬(타이틀 또는 메인 씬)에 빈 GameObject를 만들고 이름을 `SaveManager`로 지정
   - `SaveManager.cs` 컴포넌트를 붙임 (씬이 바뀌어도 파괴되지 않도록 자동으로 `DontDestroyOnLoad` 처리됨)
   - 같은 오브젝트에 테스트용으로 `SaveLoadHotkeys.cs`도 붙여두면 편함

2. **플레이어 설정**
   - 플레이어(1인칭 캐릭터) 루트 오브젝트에 `PlayerSaveData.cs`를 붙임
   - 인스펙터의 `Look Pitch Transform`에는 고개를 상하로 움직이는 카메라(또는 카메라 피벗)를 드래그해서 연결
     (없으면 비워둬도 되고, 그 경우 상하 시점 회전은 저장되지 않음)
   - 플레이어 오브젝트의 **Tag를 "Player"**로 지정 (단서 트리거 인식에 필요)

3. **단서(증거) 오브젝트 테스트 (선택)**
   - 아무 오브젝트(예: Cube)에 Collider를 추가하고 Is Trigger 체크
   - `SaveableClue.cs`를 붙이고 `Clue Id`에 겹치지 않는 고유 문자열 입력 (예: `clue_letter_01`)

4. **플레이 모드에서 테스트**
   - Play 버튼을 눌러 게임 실행
   - 플레이어를 이동시킨 뒤 **F5** → 저장
   - 계속 이동하다가 **F9** → 저장했던 위치/회전으로 복원되는지 확인
   - 단서 오브젝트에 닿아 사라지게 한 뒤 F5로 저장 → F6으로 초기화 → F9로 로드해보면
     단서가 다시 나타나지 않고(수집 상태 유지) 정상 동작하는지 확인 가능

## 다른 시스템에서 이 저장 시스템을 활용하는 방법

- 대화/추리 진행 상황 기록: `SaveManager.Instance.SetFlag("met_zhuge_liang", true);`
- 진행 상황 확인: `SaveManager.Instance.GetFlag("met_zhuge_liang");`
- 코드에서 저장/로드 직접 호출: `SaveManager.Instance.SaveGame(slot);` / `SaveManager.Instance.LoadGame(slot);`
- 저장 여부 확인(세이브 슬롯 UI 만들 때): `SaveManager.Instance.HasSave(slot)`,
  `SaveManager.Instance.PeekSavedAt(slot)`

## 참고
- 이 스크립트들을 프로젝트에 새로 추가한 뒤 Unity 에디터를 열면, 에디터가 자동으로
  `.meta` 파일을 생성하며 컴파일합니다. 별도 작업은 필요 없습니다.
- 나중에 실제 저장/불러오기 메뉴 UI(버튼 등)를 만들 때는 `SaveManager`의
  `OnSaveCompleted`, `OnLoadStarted`, `OnLoadCompleted` 이벤트를 구독해서
  "저장했습니다" 같은 안내 문구를 띄우면 됩니다.
