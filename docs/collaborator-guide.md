# 협업자 가이드 — 바이브코딩 명령 (Codex + VS Code + Unity CLI)

- 대상: 디자이너 — UI/UX·고양이 에셋·이펙트를 Unity에 직접 넣고 바꿔 봄
- 게임 골격(데이터·시스템·전투·화면 전환)은 프로그래머가 먼저 세움 → 그 위에서 작업
- 골격 UI는 회색 박스 → 레이아웃·흐름·연출은 직접 해보며 변경·재구성
- 도구: VS Code + Codex 확장(또는 codex CLI), Unity 6 LTS, Unity CLI(batchmode)
- 규칙 정본: 저장소 루트 AGENTS.md (Codex가 자동으로 읽음)

## 준비
- Unity Hub에서 Unity 6000.4.7f1 설치, 모듈: Windows Build Support·WebGL Build Support
- VS Code 확장: C# Dev Kit, Unity, Codex
- Unity → Preferences → External Tools → Editor = VS Code
- 환경변수 UNITY_EXE = Unity.exe 전체 경로
- 예: C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe
- VS Code에서 CatVillage 폴더 열기 → Codex 패널 열기

## Unity CLI 기본 명령
- 에디터 작업은 Codex가 스크립트로 만들고 CLI로 실행 → 손 클릭 반복 금지
- 실행 전 Unity 에디터 창 닫기 (같은 프로젝트 동시 실행 불가)
- 종료 코드 0 아니면 실패 → 로그를 Codex에 붙여 "이 로그 고쳐줘"

```bash
"$UNITY_EXE" -batchmode -quit -projectPath . -executeMethod 클래스.메서드 -logFile -
```

| 용도 | -executeMethod 값 |
|---|---|
| 스프라이트 일괄 임포트 | `CatVillage.EditorTools.ArtImporter.ImportAll` |
| 이펙트 프리팹 검사 | `CatVillage.EditorTools.FxValidator.ValidateAll` |
| 프로젝트 초기 설정 | `CatVillage.EditorTools.ProjectSetup.Run` |
| PC 빌드 | `CatVillage.EditorTools.BuildTool.BuildWindows` |
| 웹앱 빌드 | `CatVillage.EditorTools.BuildTool.BuildWebGL` |
| PC + 웹 둘 다 | `CatVillage.EditorTools.BuildTool.BuildAll` |
- 웹앱 로컬 확인: Builds/WebGL에서 `npx serve` → 브라우저로 열기 (파일 더블클릭은 안 됨)

- 로그를 파일로: `-logFile -` 대신 `-logFile Logs/cli.log`

## Codex 명령 — 에셋 넣기
- "Assets/Art/Cats/에 PNG 넣었어. 납품 규격대로 임포트 설정하는 ArtImporter 만들고 CLI로 실행해줘"
- "cat_fire_stage1_idle_01~04.png로 idle 클립과 Animator 만들어줘, CLI로 생성"
- "플레이스홀더 cat_water_stage2를 내가 넣은 실제 스프라이트로 교체해줘"
- "스프라이트 피벗 발밑 중앙·PPU 100인지 검사 스크립트 만들어 CLI로 돌려줘"

## Codex 명령 — UI
- "결과 화면 프리팹 만들어줘: 별 3개, 보상 3칸, 확인 버튼, 패널은 Assets/Art/UI/panel.png 9-slice"
- "버튼 누르면 살짝 작아졌다 커지는 DOTween 효과를 공용 컴포넌트로 만들어줘"
- "전투 화면 하단에 아군 3마리 HP바·스킬 게이지 배치해줘, 1080×1920 기준"
- "Assets/Art/UI/Fonts 폰트로 TextMeshPro 한글 폰트 에셋 만들어줘"

## Codex 명령 — UI/UX 바꿔 보기
- "마을 화면 버튼을 하단 탭바 5개로 바꿔줘, 기존 버튼 이벤트는 그대로 연결"
- "필살기 게이지 찬 고양이 초상화가 반짝이며 흔들리게 해줘, 탭 이벤트 유지"
- "적 들쥐 베이스로 물방울 배리언트 만들어줘: 파란 틴트, 크기 0.9, EnemyVariant SO로"
- "쥐 대왕 소환 연출: 땅 흔들림 → 들쥐 2마리 튀어나옴"
- "화면 전환을 페이드 대신 좌우 슬라이드로 바꿔줘"
- "편성 화면을 드래그 앤 드롭으로 바꿔줘, 저장되는 파티 데이터 형식은 유지"
- "이 화면 레이아웃 시안 세 개를 각각 프리팹으로 만들어줘, 비교해 볼게"
- 화면 추가·삭제·흐름 변경은 프로그래머와 합의 후

## 4주 작업 순서
- 1주: 환경 세팅·아트 콘셉트·고양이 1종 3단계·UI 톤 시안
- 2주: 고양이 6종 1단계·전투 화면 UI 교체·타격 이펙트
- 3주: 나머지 성장 단계·전 화면 UI/UX 재구성·부화·성장 연출
- 4주: 에셋 교체 마무리·사운드·폴리싱

## Codex 명령 — 이펙트
- "불꽃 타격 이펙트: Particle System, 0.5초, 주황 불티, 2초 내 자동 소멸 프리팹"
- "fx_hit_fire를 복사해 물방울·풀잎·바람·햇살·달빛 색만 바꾼 5개 만들어줘"
- "데미지 숫자 팝업: 위로 떠오르며 사라짐, 상성 유리면 노랑 크게"
- "부화 연출: 바구니 흔들림 3번 → 빛 터짐 → 아기 고양이 등장"

## Codex 명령 — 확인·수정
- "씬의 Missing 스크립트·깨진 참조를 찾는 검사 스크립트 만들어 CLI로 돌려줘"
- "Unity 에러 이거야: (붙여넣기) 원인 찾아서 고쳐줘"
- "내가 바꾼 파일만 git에 커밋해줘, 메시지는 한국어로"

## 하면 안 되는 것
- Scripts/Battle, Data/ 수치 수정 → 프로그래머 담당
- 파일명 규격 무시, Assets/Art 밖에 에셋 저장
- 출처 불명 이미지 사용 → Credits.md 기록 필수
- .meta 파일 삭제
