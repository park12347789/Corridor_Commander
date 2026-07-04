# Map Expansion Route Design

## 현재맵 분석

- `MainScene`에는 적 루트 표시 오브젝트가 3개 있다.
  - `Enemy_RouteLine_Flow_01`
  - `Enemy_RouteLine_Flow_02`
  - `Enemy_RouteLine_Flow_03`
- 세 라인은 `EnemyRouteLineVisualizer`를 갖지만 `route` 참조가 모두 비어 있다.
- 세 라인은 씬 안의 스폰 프리팹 인스턴스 아래에 수동으로 추가된 상태다. 프리팹 원본 자체에 포함된 구조는 아니다.
- 현재 링크는 기존 `OffMeshLink` 기반이다.
  - `OffMeshLink_sector02_to_sector01`: 활성, 짧은 연결.
  - `OffMeshLink_sector03_to_sector02`: 비활성, 긴 대각 연결. 섹터3 관통 원인.
- 현재 문제는 라인이 실제 적 이동 데이터의 소유자가 아니라는 점이다. 스포너는 `spawnPoint`, `route`, `goal`을 알고 있지만 라인은 별도 오브젝트라 참조가 쉽게 깨진다.

## 설계 목표

- 입구가 여러 개 동시에 있어도 각 입구가 자기 루트를 독립적으로 표시한다.
- 스폰포인트를 배치하면 루트 표시가 같이 따라온다.
- 라인은 실제 적 이동과 같은 기준으로 계산한다.
- 벽을 가로지르는 임시 직선은 표시하지 않는다.
- 맵 확장 때 수동 연결 지점을 줄인다.

## 기본 단위

입구 하나는 아래 단위로 본다.

```text
EnemyEntrance
  EnemySpawner
  EnemyRoute
  EnemySpawnAnchor
  EnemyRouteLine
```

- `EnemySpawner`가 소유자다.
- `EnemyRoute`는 해당 입구의 웨이포인트 목록이다.
- `EnemyRouteLine`은 부모 `EnemySpawner`에서 `spawnPoint`, `route`, `goal`을 자동으로 읽는다.
- 같은 섹터에 입구가 3개면 `EnemyEntrance`도 3개 둔다.

## 여러 입구 공존

여러 입구는 하나의 스포너가 배열로 들고 있는 구조로 만들지 않는다.

```text
Sector_03
  Entrances
    Sector03_Entrance_A
      EnemySpawner
      EnemyRoute
      EnemyRouteLine
    Sector03_Entrance_B
      EnemySpawner
      EnemyRoute
      EnemyRouteLine
    Sector03_Entrance_C
      EnemySpawner
      EnemyRoute
      EnemyRouteLine
```

- 동시에 켜질 수 있다.
- 같은 `goal`을 봐도 된다.
- 각자 다른 `route`를 가진다.
- 웨이브는 `EnemySpawnGroupSO` 또는 `EnemySpawnManager` 바인딩으로 여러 스포너를 동시에 선택한다.

## 섹터 연결 규칙

- 섹터 간 이동 연결은 긴 대각 링크로 만들지 않는다.
- 문, 계단, 램프, 통로 단위의 짧은 연결만 허용한다.
- 새 연결은 가능하면 `NavMeshLink`를 쓴다.
- 기존 `OffMeshLink`는 유지 가능하지만, 새 맵 확장에서는 점진적으로 `NavMeshLink`로 옮긴다.
- 링크 기준:
  - 수평 거리 4m 이하.
  - 높이 차 1.5m 이하.
  - 링크 경로에 벽/장애물 콜라이더가 있으면 실패.

## 루트 표시 규칙

- 라인은 `spawnPoint -> route waypoints -> goal` 순서로 제어점을 만든다.
- 각 구간은 `NavMesh.CalculatePath`로 실제 경로를 계산한다.
- `PathComplete`가 아니면 그 구간은 표시하지 않는다.
- 경로가 없을 때 직선을 그려서 속이지 않는다.
- 섹터가 비활성인 동안에는 해당 스포너 오브젝트가 꺼져 있으므로 라인도 같이 꺼진다.

## 프리팹 설계

`Enemy_SpawnPoint_RED.prefab`에는 다음 자식을 추가하는 방향이 맞다.

```text
Enemy_SpawnPoint_RED
  EnemySpawnAnchor
  EnemyRouteLine
    LineRenderer
    EnemyRouteLineVisualizer
```

`EnemyRouteLineVisualizer` 기본값:

- `autoResolveSpawner = true`
- `sourceSpawner = null`
- `startPoint = null`
- `goalPoint = null`
- `route = null`
- `flowMaterial = EnemyRouteFlow_Arrow_Cyan`
- `autoRefresh = true`

씬 인스턴스에서 `EnemySpawner.goal`과 `EnemySpawner.route`만 맞으면 라인이 자동으로 잡힌다.

## 섹터3 처리 방향

현재 `sector03_to_sector02` 긴 링크를 다시 켜면 관통 문제가 재발한다.

섹터3 라인이 제대로 보이려면 다음 중 하나가 필요하다.

- 실제 문/통로 위치에 짧은 `NavMeshLink`를 새로 만든다.
- 장애물이 막고 있는 구간에 실제 이동 가능한 문 열림 상태를 만든다.
- 섹터3 `EnemyRoute` waypoint를 실제 통로를 지나가게 다시 배치한다.

임시로 라인만 보이게 직선을 되살리는 것은 금지한다. 그 선은 실제 적 이동과 다르다.

## 검증 설계

검증은 메뉴 하나로 묶는다.

- `Corridor Commander/Navigation/Validate Map Links`
  - 활성 `NavMeshLink` 검사.
  - 활성 legacy `OffMeshLink` 검사.
  - 활성 스포너의 `spawnPoint -> route -> goal` 전체 경로 검사.
- `Corridor Commander/Stage/Validate Room Corridor Samples`
  - 샘플 스테이지 전체 검사.
  - 비활성 미래 스포너까지 검사.

검증 실패 메시지는 어느 스포너/링크가 실패했는지 이름으로 보여야 한다.

## 구현 순서

1. `EnemyRouteLineVisualizer`가 부모 `EnemySpawner`를 자동 바인딩하게 한다.
2. `Enemy_SpawnPoint_RED.prefab`에 `EnemyRouteLine` 자식을 넣는다.
3. `MainScene`의 수동 라인 3개를 프리팹 기반 라인으로 대체한다.
4. 섹터3 실제 연결 위치를 정하고 짧은 `NavMeshLink`를 배치한다.
5. 섹터3 `EnemyRoute` waypoint를 실제 통로 기준으로 재배치한다.
6. `Validate Map Links`로 링크/루트 경로를 확인한다.
7. 플레이에서 섹터3 스폰 적이 라인과 같은 경로로 이동하는지 확인한다.

## 완료 기준

- 스폰포인트 프리팹 인스턴스를 추가하면 라인이 자동 생성된다.
- 같은 섹터에 여러 입구가 있어도 각 라인이 독립 표시된다.
- 섹터3 라인이 보이고, 실제 적 이동과 같은 경로다.
- 벽을 관통하는 긴 링크가 활성 상태로 남아 있지 않다.
- 검증 메뉴가 unsafe link 또는 incomplete route를 잡는다.
