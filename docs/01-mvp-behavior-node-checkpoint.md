# MVP1 Behavior Node Checkpoint

작성일: 2026-05-26

## 목적

이 문서는 `Corridor Commander Unity 2`의 첫 번째 적/배경 플레이 가능 슬라이스와, 주요 존재들의 1차 Unity Behavior 노드 전환 상태를 기록합니다.

목표는 최종 밸런스나 최종 캐릭터 조작이 아닙니다. 한 씬에서 적, 포탑, 바리케이드, 목표, 건설 포인트가 안정적으로 이어지고, 임시 코드와 최종 시스템이 헷갈리지 않게 구분되는 것이 목적입니다.

## 열어야 할 씬

사용 씬:

`Assets/01_Scenes/EnemyBackgroundTest.unity`

씬 구성:

- 빨간색 적 스폰 포인트
- 노란색 목표 오브젝트, 체력, 게임오버 연결
- 스폰 지점에서 목표까지 이어지는 청록색 경로 라인
- 초록색 건설 포인트 4개
- `TEMP_...` 이름을 가진 임시 어깨뷰 플레이어
- 임시 적, 포탑, 바리케이드, 총알 프리팹

## 조작

- `WASD`: 임시 플레이어 이동
- 마우스: 임시 어깨뷰 시점 회전
- 초록색 건설 포인트 근처로 이동
- `E`: 건설 패널 열기/닫기
- `1`: 포탑 건설
- `2`: 바리케이드 건설

임시 플레이어 스크립트는 캐릭터 담당자가 최종 작업물로 오해하지 않도록 일부러 `TEMP_...` 이름을 사용합니다.

## 주요 프리팹

- `Assets/03_Prefabs/Enemy_Basic.prefab`
- `Assets/03_Prefabs/Turret_Basic.prefab`
- `Assets/03_Prefabs/Barricade_Basic.prefab`
- `Assets/03_Prefabs/Prototype_Bullet.prefab`

## 주요 스크립트

공통:

- `Assets/02_Scripts/Common/IDamageable.cs`
- `Assets/02_Scripts/Common/DamageInfo.cs`
- `Assets/02_Scripts/Common/Health.cs`
- `Assets/02_Scripts/Common/GameManager.cs`
- `Assets/02_Scripts/Common/GameOverOnDeath.cs`

이동:

- `Assets/02_Scripts/Movement/IMovementMotor.cs`
- `Assets/02_Scripts/Movement/MovementStats.cs`
- `Assets/02_Scripts/Movement/CharacterMovementMotor.cs`
- `Assets/02_Scripts/Movement/NavMeshMovementMotor.cs`

적/배경:

- `Assets/02_Scripts/Enemies/EnemySpawner.cs`
- `Assets/02_Scripts/Enemies/EnemyMovementController.cs`
- `Assets/02_Scripts/Enemies/EnemyMeleeAttackController.cs`
- `Assets/02_Scripts/Enemies/EnemyRouteLineVisualizer.cs`

전투/건설:

- `Assets/02_Scripts/Combat/TurretTargetingController.cs`
- `Assets/02_Scripts/Combat/Projectile.cs`
- `Assets/02_Scripts/Construction/PlacementPoint.cs`
- `Assets/02_Scripts/Construction/BuildableKind.cs`

## Behavior 그래프 전환

필요 패키지:

- `com.unity.behavior` version `1.0.15`

현재 1차 브리지 노드:

- `CCRunEnemyMovementAction`
- `CCRunEnemyMeleeAttackAction`
- `CCRunTurretTargetingAction`
- `CCRunEnemySpawnerAction`

그래프:

- `Assets/09_Settings/Behavior/Enemy_Basic_Unity_Behavior.asset`
  - `On Start -> Run In Parallel -> Enemy Movement / Enemy Melee Attack`
- `Assets/09_Settings/Behavior/Turret_Basic_Unity_Behavior.asset`
  - `On Start -> Turret Targeting`
- `Assets/09_Settings/Behavior/EnemySpawner_Unity_Behavior.asset`
  - `On Start -> Enemy Spawner`

현재 전환 방식은 보수적으로 유지합니다.

- 기존 컨트롤러는 삭제하지 않습니다.
- 각 컨트롤러는 공개 `Tick...()` 메서드를 제공합니다.
- Behavior 노드가 실행을 소유할 때 컨트롤러의 기본 `Update` 루프를 꺼 둡니다.
- 이렇게 해서 이동, 근접공격, 사격, 스폰이 두 번 실행되지 않게 합니다.

## 로컬 전용 도구 기준

로컬 에디터 자동화 도구를 팀 의존성으로 추가하지 않습니다.

공유 프로젝트에는 Unity Behavior, NavMesh, 씬, 프리팹, 스크립트, 머티리얼, 실제로 필요한 에셋만 남깁니다. 개인 자동화 패키지는 `Packages/manifest.json`과 `Packages/packages-lock.json`에 넣지 않습니다.

## 검증 완료

Unity Editor에서 확인:

- 컴파일 에러: 0
- 씬 누락 참조: 0
- Behavior 그래프 placeholder 노드: 0
- 이번 슬라이스에서 발생한 콘솔 에러/경고: 0
- 수동 스모크:
  - 스포너 Behavior 노드가 `Enemy_Basic`을 생성
  - 적 Behavior 그래프가 이동/근접공격 tick 메서드 실행
  - 포탑 Behavior 그래프가 적을 향해 임시 투사체 발사

## 다음 안전 작업

1. 타겟 탐색과 사거리 확인을 명시적인 Behavior 조건 노드로 분리합니다.
2. 적이 바리케이드/목표를 공격 중인지 보이는 간단한 디버그 표시를 추가합니다.
3. `SampleScene`과 `test_1`을 유지할지, 이름을 바꿀지, 제거할지 결정합니다.
4. 임시 플레이어는 캐릭터 담당자가 실제 컨트롤러 작업을 시작한 뒤 교체합니다.
