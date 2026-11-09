# CatVillage 에이전트 규칙 (Codex)

- 기획 정본: docs/game-design.md, 협업자 가이드: docs/collaborator-guide.md
- Unity 6 LTS, 2D URP, uGUI + TextMeshPro, DOTween
- 에디터 작업은 Assets/Editor/ 아래 static 메서드로 작성 → Unity CLI로 실행
- CLI: `"$UNITY_EXE" -batchmode -quit -projectPath . -executeMethod <클래스.메서드> -logFile -`
- CLI 실행 전 열린 Unity 에디터 없는지 확인, 종료 코드 0 아니면 로그 읽고 수정
- 네임스페이스: CatVillage.*, 에디터 도구는 CatVillage.EditorTools
- 에셋 위치: Assets/Art/{Cats,UI,FX}/, 파일명 소문자·언더바
- 스프라이트: 원본 16·32·64·128px 자유, 표시 크기는 프리팹이 결정, Filter Point
- UI: 화면별 프리팹(Assets/Prefabs/UI), 9-slice 스프라이트, TextMeshPro, public 필드 연결 유지
- BuildContent·UiPrefabBuilder는 프리팹을 덮어씀 → 디자이너 작업 후 실행 금지
- 빌드 확인은 WebGL, PC 빌드는 배포 때만
- 이펙트 프리팹: 2초 내 자동 소멸, 루프 금지(오라 제외)
- 디자이너 작업 범위: Assets/Art, UI 프리팹·레이아웃, FX 프리팹, 연출 컴포넌트
- UI 변경 시 데이터 바인딩 이름·버튼 이벤트 연결 유지
- Scripts/Battle, Data/ 수치는 디자이너 요청으로 수정하지 않음
- .meta 파일 삭제·이동 금지 (Unity 에디터나 CLI로만 이동)
- 커밋: 바꾼 파일만 add, 한국어 메시지
