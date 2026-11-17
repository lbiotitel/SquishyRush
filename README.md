# 말랑 돌격대! (Squishy Rush)

유닛을 모아 배치하고 6대6 자동 전투로 35라운드를 진행하는 모바일 오토배틀러입니다.

| 항목 | 내용 |
|---|---|
| 인원 | 2인 개발 (팀장) |
| 개발 기간 | 2026.07 ~ 2026.10 |
| 플랫폼 | Android |
| 사용 기술 | Unity 2022.3, C#, 뒤끝(BACKND), UniTask, Addressable |
| 구글 플레이 | https://play.google.com/store/apps/details?id=com.TinyTeams.SquishyRush |
| 플레이 영상 | https://www.youtube.com/shorts/y16HKZqZzsU |

## 담당 파트

전투 로직을 제외한 로비, 메타 시스템, 서버 연동, 데이터 저장 구조를 맡았고 구글 플레이 출시까지 진행했습니다.
제가 작성한 코드는 [`Assets/02 Scripts/Biotite`](Assets/02%20Scripts/Biotite)에 있습니다.

| 파트 | 주요 파일 |
|---|---|
| 전역 매니저, 재시작 | `Managers/Global/GlobalManagers.cs`, `SceneController.cs` |
| UI 프레임워크 | `UI/UI Define/UIManager.cs`, `PopUpContainer.cs`, `SheetContainer.cs`, `UI/BaseUI.cs` |
| 뒤끝 서버 연동 | `BackEnd/*` |
| 유저 데이터 저장 | `Managers/Global/UserServerDataManager.cs`, `Data/UserServer*.cs` |
| 게임 로직 서비스 | `Service/*` (재화, 유닛, 유물, 가챠, 출석, 일일 퀘스트, 우편, 랭킹 등) |
| 유물 시스템 | `Artifact/*`, `System/ArtifactSystem.cs` |
| 기타 | 에너지, 광고, 퍽 상점, 현지화, 디버그 콘솔 |

## 주요 구현

### 전역 매니저 구조와 재시작

- `GlobalManagers`가 모든 매니저를 먼저 생성한 뒤, 정해진 순서대로 `Initialize()`와 `Release()`를 호출합니다. 매니저끼리 서로 참조해도 초기화 전에 접근하는 일이 없습니다.
- 재시작은 빈 씬(`ForInit`)을 거쳐서 씬과 리소스 해제 → 매니저 재초기화 → 타이틀 순서로 진행합니다.
- 씬 전환은 빌더 패턴으로 만들어서, 호출하는 쪽에서 필요한 단계만 이어 붙입니다.

```csharp
await Scene.NewTransition()
    .Unload(nameof(EScene.Lobby))
    .UnLoadResources(nameof(EResourceLabel.Lobby))
    .Load(nameof(EScene.ForInit))
    .Perform();
```

### 시트, 팝업 UI 프레임워크

- 팝업은 위로 쌓이고 시트는 서로 교체돼야 해서, 화면마다 이 규칙을 따로 처리하지 않도록 `PopUpContainer`와 `SheetContainer`로 나누었습니다.
- 모든 UI는 `BaseUI`를 상속하고, 타입으로 열고 닫습니다.
- 닫은 UI는 파괴하지 않고 닫힌 UI 풀에 보관했다가 다시 열 때 재사용합니다. 처음 열 때만 리소스에서 프리팹을 만듭니다.

### 뒤끝 서버 연동

- `BackEndManager` 아래에 로그인, 유저 데이터, 차트, 가챠, 우편, 랭킹 담당 클래스를 나눴습니다.
- 콜백 기반 SDK를 UniTask로 감싸서 `async/await`로 순서대로 읽히게 했습니다.
- 점검, 요청 과다, 기기 차단 같은 서버 에러는 `BackEndErrorHandlerService`에서 이벤트 하나로 모으고, UI 쪽에서 한 곳에서 에러 팝업을 띄웁니다.
- 구글 로그인은 콜백이 메인 스레드가 아닌 곳에서 와서 인증이 실패하던 문제가 있었습니다. 인증 전에 `UniTask.SwitchToMainThread()`를 넣어 해결했습니다.
- 실시간 알림 서버에 연결해서 새 우편이 오면 이벤트로 알립니다.

### 변경된 데이터만 저장하는 구조

- 유저 데이터는 프로필, 재화, 에너지, 출석, 유닛, 유물, 일일 퀘스트, 진행도 테이블로 나눴습니다. 각 테이블은 `UserServerDataBase`를 상속한 모듈이고, 타입을 키로 하는 매니저에서 관리합니다.
- 값이 바뀐 모듈만 `IsDirty`로 표시하고, 저장할 때 바뀐 테이블만 서버로 보냅니다. 저장에 성공했을 때만 표시를 지워서 실패하면 다음 저장 때 다시 보냅니다.
- 일정 주기마다 비동기로 저장하고, 앱이 백그라운드로 가거나 종료될 때는 동기로 저장합니다.

### 가챠 롤백

- 가챠는 재화 차감, 결과 지급, 서버 저장이 한 번에 일어나서 중간에 실패하면 재화만 사라질 수 있었습니다.
- 가챠 전에 재화와 대상(유닛 또는 유물) 데이터의 스냅샷을 만들고, 저장에 실패하거나 예외가 나면 스냅샷으로 되돌립니다.
- 확률은 뒤끝 서버의 확률표에서 받아와서 클라이언트에서 조작할 수 없고, 앱 업데이트 없이 확률을 바꿀 수 있습니다.
- 서버 호출(`GachaBackEnd`)과 게임 규칙(`GachaService`)을 분리했습니다.

### 서비스 계층

- 데이터 모듈은 저장과 불러오기만 맡고, 재화 사용이나 유닛 강화 같은 게임 규칙은 `Service` 클래스가 맡습니다.
- 서비스는 `ServiceManager`에 타입으로 등록하고 꺼내 써서, UI는 서비스만 호출하면 됩니다.

### 유물 시스템

- 유물 데이터에 효과 ID 목록을 넣고, `ArtifactEffectFactory`가 레벨에 맞는 수치로 효과 객체를 만듭니다.
- 효과는 스탯 수정 효과와 조건 발동 효과로 나뉘고, `IArtifactEffect`를 구현해서 같은 방식으로 적용합니다.

## 저장소 안내

- 2인 프로젝트라 팀원의 코드(`JSH` 폴더, 전투와 스킬)도 함께 있습니다. 제 코드는 위의 `Biotite` 폴더입니다.
- 에셋 스토어 리소스가 포함되어 있습니다.
