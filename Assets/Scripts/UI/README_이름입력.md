# 주인공 이름 입력 화면 사용법

## 구성 파일
- `PlayerNameInputUI.cs` : 새 게임 시작 시 이름을 입력받는 UI 컨트롤러

## 관련 변경 사항
- `Assets/Scripts/Save/SaveData.cs`에 `playerName` 필드 추가
- `Assets/Scripts/Save/SaveManager.cs`에 다음이 추가됨
  - `SaveManager.DefaultPlayerName` (`"이름 없는 참모"`) — 빈 값으로 확정했을 때 쓰이는 기본 이름
  - `SetPlayerName(string name)` — 이름을 설정 (빈 값/공백만 있으면 자동으로 기본 이름 사용)
  - `GetPlayerName()` — 현재 설정된 이름을 가져옴 (대화 시스템 등에서 "OO님, ..." 식으로 활용 가능)
  - `SaveGame(slot)` 저장 시 이름도 함께 저장되도록 반영됨

## 에디터에서 설정하는 순서

1. **SaveManager가 있는 씬(타이틀 씬)에 UI 배치**
   - Canvas를 만들고 그 아래에 InputField(이름 입력창), Button(확인/게임 시작), Text(안내 문구, 선택)를 배치
   - 같은 Canvas(또는 빈 GameObject)에 `PlayerNameInputUI.cs`를 붙이고 인스펙터에서 방금 만든
     InputField/Button/Text를 각각 연결
   - `Gameplay Scene Name`에는 실제 플레이 씬 이름을 입력 (Build Settings에 추가되어 있어야 함)

2. **테스트**
   - Play 모드에서 이름을 입력하고 확인 버튼(또는 Enter)을 누르면 게임 플레이 씬으로 이동
   - 빈 채로 확인을 누르면 `"이름 없는 참모"`로 자동 설정되는지 확인
   - 이후 F5로 저장하고 F9로 로드했을 때 이름이 유지되는지 확인 (필요하면 `SaveManager.Instance.GetPlayerName()`을
     아무 디버그 텍스트에 출력해서 확인해보세요)

## 참고: 이어서 정할 것

- 엔딩 화면(`EndingUIController`)의 "다시 시작하기" 버튼은 현재 `SaveManager.ResetProgress()`를 호출한 뒤
  바로 게임 플레이 씬으로 이동하는데, 이렇게 하면 저장된 이름도 함께 초기화됩니다. 재시작할 때 이름을 다시 입력받고
  싶다면 그 버튼이 게임 플레이 씬 대신 이 이름 입력 씬(또는 타이틀 씬)으로 이동하도록 씬 이름만 바꿔주면 됩니다.
- 대화 시스템을 만들 때 `SaveManager.Instance.GetPlayerName()`을 대사 텍스트에 삽입하면
  ("{이름} 참모, 이번 일을 맡아주게" 같은 식으로) 다른 인물이 주인공 이름을 불러주는 연출을 넣을 수 있습니다.
