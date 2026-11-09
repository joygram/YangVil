# 협업자 가이드 — Unity UI 정석 구성 + 바이브코딩 명령

- 대상: 디자이너 — UI/UX·고양이 에셋·이펙트를 Unity에서 직접 바꿔 봄
- 게임 골격(데이터·전투·저장·화면 전환)은 완성 상태 → 프리팹·스프라이트만 바꾸면 화면이 바뀜
- 도구: VS Code + Codex, Unity 6000.4.7f1, Unity CLI(batchmode)
- 규칙 정본: 저장소 루트 AGENTS.md (Codex가 자동으로 읽음)

## 준비
- Unity Hub에서 Unity 6000.4.7f1 설치, 모듈: Windows Build Support·WebGL Build Support
- VS Code 확장: C# Dev Kit, Unity, Codex
- Unity → Preferences → External Tools → Editor = VS Code
- 환경변수 UNITY_EXE = C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe
- 첫 실행: Unity Hub에서 CatVillage 폴더 열기 → Assets/Scenes/Intro.unity 열고 Play

## 프로젝트 구조
| 위치 | 내용 |
|---|---|
| Assets/Scenes/Intro.unity | 유일한 씬: Canvas·EventSystem·GameBootstrap |
| Assets/Prefabs/UI/Screens | 화면 프리팹 5개 (Intro·Village·StageSelect·Battle·Result) |
| Assets/Prefabs/UI/Components | 카드·버튼·유닛 슬롯·데미지 팝업 프리팹 |
| Assets/Art/ThirdParty | 무료 기본 에셋 (Kenney UI·고양이·개·Galmuri) |
| Assets/Art/UI | 스프라이트 아틀라스·TMP 폰트 에셋 |
| Assets/Data | 고양이·적·스테이지·아이템 ScriptableObject |
| Assets/Resources/GameConfig.asset | 전체 설정·화면 프리팹 목록 |

## UI 정석 구성 규칙
- 화면 1개 = 프리팹 1개, 루트에 화면 스크립트(예: VillageScreen)
- 스크립트 public 필드 = 코드와 연결된 슬롯 → 인스펙터에서 연결 유지
- 배치·크기·색·스프라이트·폰트 크기는 자유롭게 변경
- 반복 항목(카드·버튼)은 Components 프리팹 → 화면이 런타임에 복제
- 버튼은 LabeledButton 컴포넌트 (Button + TMP 라벨 + 강조 스프라이트)
- 기준 해상도 1080×1920 세로, Canvas Scaler = Scale With Screen Size, Match Height
- 앵커: 상단 바는 위쪽, 하단 버튼은 아래쪽, 목록은 Stretch → 창 크기 바뀌어도 유지
- 목록은 ScrollRect + VerticalLayoutGroup + ContentSizeFitter

## 스프라이트 9-slice
- UI 이미지: Texture Type = Sprite (2D and UI), Filter = Point(픽셀아트)
- Sprite Editor → Border 지정 (Kenney 기본 16px, 자동 적용됨)
- Image 컴포넌트 Image Type = Sliced
- Pixels Per Unit Multiplier로 테두리 두께 조절 (현재 1/3 → 3배 굵게)
- 새 UI 이미지는 Assets/Art/ThirdParty/KenneyPixelUI 대신 Assets/Art/UI/Sprites에 넣고 Border 직접 지정

## 스프라이트 아틀라스
- Assets/Art/UI/UI.spriteatlasv2 = UI 스프라이트 묶음 → 드로우콜 감소
- 새 UI 폴더 추가 시 아틀라스 Objects for Packing에 폴더 추가
- 픽셀아트: Filter Point, Padding 4, 회전·Tight Packing 끔

## 폰트 (TextMeshPro)
- 한글 폰트는 필수: 웹앱(WebGL)은 OS 폰트를 못 써서 한글이 안 보임
- 픽셀 폰트는 선택: 지금은 Galmuri11(픽셀, OFL) → 일반 폰트로 바꿔도 됨
- 현재 폰트 에셋: Assets/Art/UI/Fonts/Galmuri11 SDF.asset (Dynamic → 쓰는 글자 자동 추가)
- 교체: Window → TextMeshPro → Font Asset Creator 또는 TTF 우클릭 → Create → TextMeshPro → Font Asset
- 교체 후 Codex: "모든 UI 프리팹 TMP 폰트를 새 폰트 에셋으로 바꿔줘"

## 캐릭터 픽셀 크기
- 원본 16·32·64·128px 무엇이든 가능 → 화면 표시 크기는 프리팹 Unit 크기가 결정
- 개별 프레임 PNG: CatData/EnemyBase의 sprites.idle·attack에 순서대로 연결
- 시트 1장: sprites.sheet 연결 + cellSize(16·32·64·128) + 행 번호·프레임 수 지정
- 픽셀아트는 Filter Point 유지 (Assets/Art 아래는 자동)

## 에셋 분산 로딩 (Addressables)
- 시작 다운로드 = 엔진·Intro 씬만, 나머지는 필요할 때 번들로 받음
- UI 그룹: 화면 프리팹·컴포넌트·폰트·UI 스프라이트 → 번들 1개
- Characters 그룹: 폴더 단위 번들 (CuteKitty·Dog·Art/Cats·Art/Enemies 폴더마다 1개)
- 새 캐릭터 그림은 Assets/Art/Cats·Assets/Art/Enemies 아래 폴더로 → 폴더 = 번들
- 캐릭터 그림은 CatData·EnemyBase의 sprites(AssetReference)로만 연결
- 🚫 화면 프리팹 Image에 캐릭터 그림 직접 끌어다 넣기 금지 → UI 번들에 딸려 들어감
- 새 화면 프리팹 추가 후 `UiPrefabBuilder.RegisterScreens` 실행 → GameConfig 등록

## 메모리 정책
- 화면 전환 시 이전 화면 프리팹·그림 해제 → 사용 안 하는 에셋 언로드 → GC
- 전투는 참가 유닛 그림만 로드, 끝나면 해제
- 브라우저 메모리 부족 신호(lowMemory) 시 즉시 언로드
- WebGL 힙: 시작 64MB → 필요 시 증가(Geometric) → 최대 2GB
- 256px 이하 그림: 무압축·Point (도트 선명)
- 256px 초과 그림: Crunch 압축·Bilinear, 최대 2048px 자동 축소
- 설정 위치: GameConfig.memory (언로드 주기·GC·저메모리 대응)

## Unity CLI 명령
- 실행 전 Unity 에디터 창 닫기 (같은 프로젝트 동시 실행 불가)
- 종료 코드 0 아니면 실패 → 로그를 Codex에 붙여 "이 로그 고쳐줘"

```bash
"$UNITY_EXE" -batchmode -quit -projectPath . -executeMethod 클래스.메서드 -logFile -
```

| 용도 | -executeMethod 값 |
|---|---|
| URP·플레이어 설정 | `CatVillage.EditorTools.ProjectSetup.Run` |
| 데이터·프리팹·씬 재생성 | `CatVillage.EditorTools.ProjectSetup.BuildContent` |
| 데이터만 | `CatVillage.EditorTools.GameDataBuilder.Build` |
| 화면 프리팹 등록 | `CatVillage.EditorTools.UiPrefabBuilder.RegisterScreens` |
| Addressables 그룹 정리 | `CatVillage.EditorTools.AddressablesSetup.Run` |
| 웹앱 빌드 (평소 확인용) | `CatVillage.EditorTools.BuildTool.BuildWebGL` |
| PC 빌드 (배포 때만) | `CatVillage.EditorTools.BuildTool.BuildWindows` |
- 평소 확인은 에디터 Play 또는 WebGL 빌드 → PC 빌드는 배포 시점에만
- TMP 리소스 가져오기만 `-quit` 없이: `-executeMethod CatVillage.EditorTools.UiPrefabBuilder.ImportTmpEssentials`
- ⚠ BuildContent·UiPrefabBuilder는 프리팹을 새로 만듦 → 디자이너가 손본 프리팹을 덮어씀
- 프리팹 손보기 시작한 뒤에는 BuildContent 금지, 데이터만 GameDataBuilder.Build
- 웹앱 로컬 확인: Builds/WebGL에서 `npx serve` → 브라우저로 열기

## Codex 명령 — UI
- "VillageScreen 프리팹 상단 바를 하단 탭바로 옮겨줘, public 필드 연결 유지"
- "Assets/Art/UI/Sprites/panel_wood.png를 9-slice(Border 12)로 설정하고 RowCard 배경으로 바꿔줘"
- "LabeledButton에 눌림 시 살짝 작아지는 연출 추가해줘"
- "BattleScreen 아군 초상화 버튼을 원형 프레임으로 바꿔줘"
- "Galmuri 대신 Assets/Art/UI/Fonts/NotoSansKR.ttf로 TMP 폰트 에셋 만들고 전 프리팹에 적용해줘"
- "이 화면 레이아웃 시안 세 개를 프리팹 Variant로 만들어줘, 비교해 볼게"

## Codex 명령 — 캐릭터·적
- "Assets/Art/Cats/cheese_32.png 시트(32px, idle 1행 4프레임, 공격 2행 6프레임)로 cat_fire 스프라이트 교체해줘"
- "들쥐 베이스로 물방울 배리언트 만들어줘: EnemyVariant SO, 크기 0.9"
- "쥐 대왕 소환 연출: 땅 흔들림 → 들쥐 등장"

## Codex 명령 — 이펙트
- "불꽃 타격 이펙트: Particle System, 0.5초, 주황 불티, 2초 내 자동 소멸 프리팹"
- "fx_hit_fire를 복사해 다른 5속성 색만 바꾼 프리팹 만들어줘"
- "부화 연출: 바구니 흔들림 3번 → 빛 터짐 → 아기 고양이 등장"

## Codex 명령 — 확인·수정
- "프리팹들의 Missing 스크립트·빈 public 필드를 찾는 검사 스크립트 만들어 CLI로 돌려줘"
- "Unity 에러 이거야: (붙여넣기) 원인 찾아서 고쳐줘"
- "내가 바꾼 파일만 git에 커밋해줘, 메시지는 한국어로"

## 4주 작업 순서
- 1주: 환경 세팅·Intro로 한 바퀴 플레이·아트 콘셉트·UI 톤 시안
- 2주: 고양이 6종·전투 화면 UI 교체·타격 이펙트
- 3주: 성장 단계 외형·전 화면 UI/UX 재구성·부화·성장 연출
- 4주: 에셋 교체 마무리·사운드·폴리싱

## 하면 안 되는 것
- Scripts/Battle, Data/ 수치 코드 수정 → 프로그래머 담당
- 화면 스크립트 public 필드 연결 끊기
- 출처 불명 이미지 사용 → Credits.md 기록 필수
- .meta 파일 삭제
