# YangVil 에이전트 규칙 (Codex)

- 기획 정본: docs/game-design.md, 협업자 가이드: docs/collaborator-guide.md
- Unity 6 LTS, 2D URP, uGUI + TextMeshPro, DOTween
- 에디터 작업은 Assets/Editor/ 아래 static 메서드로 작성 → Unity CLI로 실행
- CLI: `"$UNITY_EXE" -batchmode -quit -projectPath . -executeMethod <클래스.메서드> -logFile -`
- CLI 실행 전 열린 Unity 에디터 없는지 확인, 종료 코드 0 아니면 로그 읽고 수정
- 네임스페이스: YangVil.*, 에디터 도구는 YangVil.EditorTools
- 에셋 위치: Assets/Art/{Cats,UI,FX}/, 파일명 소문자·언더바
- 스프라이트: 원본 16·32·64·128px 자유, 표시 크기는 프리팹이 결정, Filter Point
- UI: 화면별 프리팹(Assets/Prefabs/UI), 9-slice 스프라이트, TextMeshPro, public 필드 연결 유지
- BuildContent·UiPrefabBuilder는 프리팹을 덮어씀 → 디자이너 작업 후 실행 금지
- 빌드 확인은 WebGL, PC 빌드는 배포 때만
- 화면 프리팹·캐릭터 그림은 Addressables로만 로드 (Resources·직접 참조 금지)
- 캐릭터 그림 연결은 SpriteSet의 AssetReferenceTexture2D, 로드는 화면의 AssetScope
- 새 화면 프리팹 → UiPrefabBuilder.RegisterScreens, 새 캐릭터 폴더 → AddressablesSetup.Run
- 로드한 에셋은 AssetScope가 화면 종료 시 해제 → 직접 Addressables.Load 호출 시 해제 책임 명시
- 이펙트 프리팹: 2초 내 자동 소멸, 루프 금지(오라 제외)
- 디자이너 작업 범위: Assets/Art, UI 프리팹·레이아웃, FX 프리팹, 연출 컴포넌트
- UI 변경 시 데이터 바인딩 이름·버튼 이벤트 연결 유지
- Scripts/Battle, Data/ 수치는 디자이너 요청으로 수정하지 않음
- .meta 파일 삭제·이동 금지 (Unity 에디터나 CLI로만 이동)

## 동시 작업
- 같은 프로젝트 폴더는 Unity 1개만 열림 (Temp/UnityLockfile)
- CLI 점검·빌드는 점검 폴더 `../YangVil-check` 하나에서만 (git 저장소 아님, 트리를 풀어 씀)
- 점검 폴더 Library 캐시(약 2.4GB)는 최초 1회 생성 후 재사용 → 지우지 않음
- 빌더 콘텐츠 재생성: `bash scripts/tools/regen-content.sh <ExecuteMethod> <가져올 경로>…` (스테이징 트리 기준, 점검 폴더에서 생성 후 원본으로 복사)

## 빌드 규칙
- 🚫 컴파일 확인 없이 빌드 금지: 먼저 `-batchmode -quit`(executeMethod 없이) → `error CS` 0건 확인 후 빌드
- BuildTool은 컴파일 실패 상태면 빌드를 거부함 (scriptCompilationFailed)
- 컴파일 확인 로그에서 `error CS`와 함께 `invalid parameters`(MenuItem 오류)도 0건 확인
- `[MenuItem]` 메서드는 인자 없음 → 인자 필요하면 메뉴용 래퍼 메서드를 따로 둠
- 🚫 Unity 오브젝트에 `??`·`?.` 금지 (에디터의 가짜 null 통과) → `TryGetComponent`·`== null` 사용
- 화면(GameScreen)은 Addressables 비동기 생성 → `Update`가 `Init`보다 먼저 돌 수 있음 → `if (Screens == null) return;`
- 웹 빌드는 증분: Library·Addressables 캐시 재사용, 전체 재빌드는 `-cleanBuild` 인자 줄 때만
- 빌드보다 플레이 확인이 먼저: 헤드리스 Play 점검 통과 → 그다음 웹 빌드
- Play 점검: `"$UNITY_EXE" -batchmode -projectPath <클론> -executeMethod YangVil.EditorTools.PlaySmokeCheck.Run -logFile -`
- Play 점검 = 창 없음, `-quit` 붙이지 않음, 마을→전투→결과→마을 자동 진행, 오류 1건이라도 있으면 종료 코드 1
- 🚫 컴파일 0건 + Play 점검 `결과 오류 0건` 둘 다 확인 전 커밋·푸시 금지
- 새 화면·기능 추가 시 PlaySmokeCheck에 단계 추가

## 자동 검증·배포
- 최초 1회: `git config core.hooksPath .githooks` → 커밋마다 pre-commit 훅 동작
- 훅: 스테이징 내용을 `../YangVil-check`에 풀어 컴파일 + 헤드리스 Play 점검 → 실패 시 커밋 거부
- Assets·Packages·ProjectSettings 변경 없는 커밋(문서 등)은 점검 생략
- 🚫 `--no-verify`·`SKIP_UNITY_CHECK=1` 사용 금지 (사용자가 지시한 경우만)
- 웹 배포: `bash scripts/deploy-web.sh` → HEAD 검증 통과 시에만 WebGL 빌드 → Cloudflare 배포
- 웹 빌드는 Unity 설치 PC에서만 가능 (CI는 Unity 라이선스 등록 후 수동 실행만)
- 자동 배포: 셀프 호스티드 러너(라벨 unity) → main 푸시 시 `deploy-self-hosted.yml`이 deploy-web.sh 실행
- 게임 버전: `VERSION`(메이저.마이너) + 패치 = 빌드 번호(CI는 GITHUB_RUN_NUMBER, 로컬은 Library 카운터) → 빌드마다 자동 증가, 커밋 불필요
- 버전 표시: 인트로 하단 `v메이저.마이너.패치 · 빌드일시`, 웹 version.json(페이지에 박힌 버전과 다르면 새 버전 배너), 배포 성공 시 릴리즈 `v버전`(채팅 앱 배포 알림)
- 메이저·마이너 올리기: `VERSION` 파일만 수정 후 커밋
- Unity 버전 정본: `ProjectSettings/ProjectVersion.txt` (현재 6000.6.8f1) → 스크립트·러너·CI 전부 여기서 읽음, 하드코딩 금지
- 버전 올리기: 새 에디터로 점검 폴더에서 프로젝트 열기 → ProjectSettings·Packages 반영 → 커밋 (러너는 다음 실행에서 자동 설치 대상 인식)
- 러너 조건: ProjectVersion 버전 + WebGL 모듈 활성화, bash, Node, 시크릿 CLOUDFLARE_API_TOKEN·CLOUDFLARE_ACCOUNT_ID
- 러너 장비의 저장소 폴더는 스크립트 실행용 → Unity로 열지 않음, 갱신은 `git pull` 대신 `git sync`
- 리눅스 러너: `setup-linux-runner.sh install` → 원격 데스크톱에서 unityhub 로그인·Personal 추가, `npx wrangler login` → `setup-linux-runner.sh ci`
- 러너 .env: UNITY_EXE·UNITY_EXTRA_ARGS=-nographics·HOME·YANGVIL_CHECK_DIR(~/YangVil-check, Library 캐시 유지)
- 🚫 공개 저장소 → 셀프 호스티드 워크플로에 `pull_request`·`pull_request_target` 트리거 금지 (외부 코드가 장비에서 실행됨)
- 🚫 장비 이름·IP·계정·포트·접속 경로를 저장소에 기록 금지 → 장비 설정 스크립트는 저장소 밖에서 관리
- 어느 씬에서 Play 해도 Intro(빌드 0번)부터 시작됨

## 커밋 전 체크 (하나라도 실패하면 커밋 금지)
- 컴파일 0에러: 클론에서 `-batchmode -quit` 실행 → `error CS` 0건, 종료 코드 0
- 코드와 생성 콘텐츠 짝 맞춤: 데이터·프리팹·씬을 읽는 코드를 바꿨으면 빌더 실행 결과(SO·프리팹·씬·GameConfig)까지 같은 커밋
- 빌더 코드 수정 시 해당 빌더 실행 → 로그에 `Builder:` 완료 줄 확인
- GameConfig 필수 참조 비어 있지 않음: starterCat·starterBasket·starterHouse·villageScene·battleScene·screens
- 새 화면 프리팹 → screens 등록, 새 씬 → Addressables Scenes 그룹 등록
- 삭제한 클래스 이름이 코드·프리팹에 남지 않음: `rg <클래스명> Assets` 0건
- 저장 구조(SaveData) 바뀌면 `SaveData.CurrentVersion` 올리고 `Game.Upgrade`에 이행 추가 → 🚫 옛 저장 버리기 금지
- 생성물·테스트 산출물 제외: Builds/ · Library/ · Assets/Resources/PerformanceTestRun*.json
- 사용자가 에디터로 확인하는 변경이면 원본 폴더에 콘텐츠까지 반영된 뒤 알림

## 커밋·푸시
- 커밋 전 `git status --short`로 바뀐 파일·새 파일(??)·삭제(D) 전부 확인
- 새 에셋·스크립트는 짝 `.meta`와 함께 add (meta 빠지면 다른 PC에서 참조 깨짐)
- 폴더째 추가한 경우 하위 파일과 폴더 `.meta`까지 포함됐는지 확인
- 삭제한 파일은 `.meta`도 같이 삭제 상태인지 확인
- 커밋 금지: Library/ · Temp/ · Logs/ · UserSettings/ · Builds/ · *.csproj · *.sln
- 바꾼 파일만 add, 남의 작업·무관한 변경 섞지 않기
- 에디터 자동 변경(ProjectSettings 정리·폰트 동적 아틀라스 등)도 반영 → 기능 커밋과 분리해 `chore: 에디터 자동 변경 반영`으로 커밋
- 메시지는 한국어, `종류: 무엇을` (feat·fix·docs·chore)
- 커밋 후 `git status --short`가 비었거나 의도한 미커밋만 남았는지 재확인
- 푸시: `git push` → main 푸시 시 GitHub Actions가 WebGL 빌드·Cloudflare 배포
- 푸시 후 `gh run list --limit 1`로 배포 실행 상태 확인
