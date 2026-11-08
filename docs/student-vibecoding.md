# 학생용 바이브코딩 명령 모음 (Codex + VS Code + Unity CLI)

- 대상: 아트 담당 학생 — UI·고양이 에셋·이펙트를 Unity에 직접 넣고 연결
- 도구: VS Code + Codex 확장(또는 codex CLI), Unity 6 LTS, Unity CLI(batchmode)
- 규칙 정본: 저장소 루트 AGENTS.md (Codex가 자동으로 읽음)

## 준비
- Unity Hub에서 Unity 6 LTS 설치, 모듈: Android Build Support
- VS Code 확장: C# Dev Kit, Unity, Codex
- Unity → Preferences → External Tools → Editor = VS Code
- 환경변수 UNITY_EXE = Unity.exe 전체 경로
- 예: C:\Program Files\Unity\Hub\Editor\6000.0.x\Editor\Unity.exe
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
| 안드로이드 빌드 | `CatVillage.EditorTools.BuildTool.BuildAndroid` |

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
