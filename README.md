# Corridor Commander

3D 3인칭 복도 디펜스 MVP를 위한 Unity 프로토타입입니다.

## 현재 작업 슬라이스

- Unity 프로젝트: `Corridor_Commander`
- 메인 테스트 씬: `Assets/01_Scenes/EnemyBackgroundTest.unity`
- 현재 범위: 적 스폰/이동/근접공격, 포탑 사격, 바리케이드/목표 체력, 임시 건설 상호작용, 1차 Unity Behavior 그래프 전환
- 팀원이 추가로 설치해야 하는 로컬 에디터 자동화 도구는 없습니다.
- Unity Behavior 패키지는 프로젝트 의존성으로 포함됩니다.

## 팀 작업 기준

- 인터페이스 스크립트는 항상 `I`로 시작합니다.
- 이동, 데미지, 체력처럼 공유되는 기능은 OOP 경계를 유지합니다.
- 프로토타입 콘텐츠는 씬/하이어라키/프리팹 배치를 우선합니다.
- 임시 플레이어 스크립트는 일부러 `TEMP_...` 이름을 사용합니다. 캐릭터 담당자가 나중에 교체할 대상입니다.

현재 인수인계는 [MVP1 Behavior Node Checkpoint](docs/01-mvp-behavior-node-checkpoint.md)를 보면 됩니다.
