# Corridor Commander MVP1 단순 OOP/Behavior 뼈대 설계

## 목적

MVP1에서는 기능을 많이 쌓기보다, 적/포탑/장애물/건설의 책임을 작게 나누고 한 씬에서 검증 가능한 뼈대를 먼저 만든다.

## 기본 원칙

- 각 컴포넌트는 한 가지 역할만 가진다.
- 적과 포탑의 판단 흐름은 Unity Behavior Graph에서 실행한다.
- 이동 시스템은 이동만 담당한다.
- 전투 시스템은 `Health`를 찾아 피해만 준다.
- 장애물은 자기 타입에 따라 길을 막거나 파괴 대상이 된다.
- 별도 커스텀 우회 AI는 MVP1 기본 설계에서 제외한다.
- 작업 보조용 MCP/AI 연결 패키지는 공유 프로젝트에 포함하지 않는다.

## 책임 분리

### Player

- `CharacterController`로 이동한다.
- `isTrigger = false`인 `Collider`는 통과하지 못한다.
- 설치는 `PlacementPoint`를 통해서만 수행한다.

### Enemy

- `NavMeshAgent`와 `NavMeshMovementMotor`로 목표까지 이동한다.
- `BehaviorGraphAgent`를 가진다.
- Behavior Graph는 `CCRunEnemyMovementAction`과 `CCRunEnemyMeleeAttackAction`을 실행한다.
- 근처에 비적 `Health`가 있으면 이동을 멈추고 공격한다.
- 직접 좌/우 우회점을 계산하지 않는다.

### Turret

- `BehaviorGraphAgent`를 가진다.
- Behavior Graph는 `CCRunTurretTargetingAction`을 실행한다.
- 적의 `Health`만 타겟으로 잡는다.
- 투사체 생성과 발사만 담당한다.

### Solid Obstacle

- 통과 불가능한 일반 장애물이다.
- `Collider`는 non-trigger다.
- `MapObstacle` 값은 `Solid`다.
- `NavMeshObstacle.carving = true`로 적 경로를 잘라낸다.
- `Health`는 없다.

### Breakable Obstacle

- 적이 부술 수 있는 장애물이다.
- `Collider`는 non-trigger다.
- `MapObstacle` 값은 `Breakable`이다.
- `Health`를 가진다.
- 활성 `NavMeshObstacle`은 두지 않는다.

### PlacementPoint

- 설치 가능한 위치만 표시한다.
- `Collider`는 trigger다.
- 빌드 앵커에 터렛 또는 바리케이드를 1개 생성한다.

### Goal

- `Health`를 가진다.
- `GameOverOnDeath`로 패배 조건을 연결한다.

## 현재 코드 뼈대

- `Assets/hansol/02_Scripts/World/MapObstacle.cs`
- `Assets/hansol/02_Scripts/World/MapObstacleKind.cs`
- `Assets/hansol/02_Scripts/Movement/NavMeshMovementMotor.cs`
- `Assets/hansol/02_Scripts/Editor/SlopedTurretEnemyTestMapBuilder.cs`

## 검증 기준

- 플레이어가 Solid/Breakable 장애물을 통과하지 못한다.
- 적이 Solid 장애물은 NavMesh로 우회한다.
- 적이 Breakable 장애물은 가까이 가서 공격한다.
- Breakable 장애물이 파괴되면 적이 다시 목표로 이동한다.
- Enemy prefab에는 `BehaviorGraphAgent`가 있다.
- Turret prefab에는 `BehaviorGraphAgent`가 있다.
- `Packages/manifest.json`과 `Packages/packages-lock.json`에 MCP/AI 작업 보조 패키지가 남지 않는다.

## 다음 작업

- Unity Editor에서 Behavior Graph와 테스트맵을 재생성한다.
- `SlopedTurretEnemyTest` 씬에서 충돌/우회/파괴 흐름을 플레이 모드로 검증한다.
- 검증 후 필요한 파일만 커밋/푸시한다.
