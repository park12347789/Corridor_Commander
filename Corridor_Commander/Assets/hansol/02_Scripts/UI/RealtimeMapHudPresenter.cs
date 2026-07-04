using System.Collections.Generic;
using CorridorCommander.PlayerControl;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class RealtimeMapHudPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform mapContentRoot;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform enemyMarkerTemplate;
        [SerializeField] private RectTransform doorMarkerTemplate;
        [SerializeField] private Text waveTimerText;
        [SerializeField] private TMP_Text waveTimerTmpText;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private Vector2 worldMin = new Vector2(-12f, -34f);
        [SerializeField] private Vector2 worldMax = new Vector2(112f, 34f);
        [SerializeField] private bool centerOnPlayer = true;
        [SerializeField] private Vector2 playerCenteredHalfExtent = new Vector2(28f, 18f);
        [SerializeField, Min(0.05f)] private float refreshInterval = 0.2f;
        [SerializeField, Min(1)] private int maxEnemyMarkers = 96;
        [SerializeField] private bool showWorldGeometry = true;
        [SerializeField, Min(1)] private int maxWorldGeometryShapes = 384;
        [SerializeField] private Color floorGeometryColor = new Color(0.2f, 0.7f, 0.9f, 0.24f);
        [SerializeField] private Color wallGeometryColor = new Color(0.78f, 0.95f, 1f, 0.56f);
        [SerializeField] private Color rampGeometryColor = new Color(0.35f, 1f, 0.72f, 0.32f);

        private readonly List<RectTransform> enemyMarkers = new List<RectTransform>();
        private readonly List<RectTransform> doorMarkers = new List<RectTransform>();
        private readonly List<Image> worldGeometryImages = new List<Image>();
        private readonly List<GeometrySample> worldGeometrySamples = new List<GeometrySample>();
        private RectTransform worldGeometryLayer;
        private Transform player;
        private float nextRefreshTime;
        private float nextMissingPlayerLogTime;
        private bool isConfigured;

        private void Awake()
        {
            ResolveWaveDirector();
            isConfigured = ValidateConfiguration();
            if (!isConfigured)
            {
                enabled = false;
                return;
            }

            enemyMarkerTemplate.gameObject.SetActive(false);
            doorMarkerTemplate.gameObject.SetActive(false);
            EnsureWorldGeometryLayer();
            ResolvePlayer();
            RefreshWorldGeometrySamples();
            RefreshWorldGeometryImages();
            RefreshDynamicMarkers();
            RefreshWaveText();
        }

        private void Update()
        {
            if (!isConfigured)
            {
                return;
            }

            if (waveDirector == null)
            {
                ResolveWaveDirector();
            }

            if (player == null)
            {
                ResolvePlayer();
            }

            RefreshPlayerMarker();
            RefreshWorldGeometryImages();
            RefreshWaveText();

            if (Time.unscaledTime >= nextRefreshTime)
            {
                RefreshWorldGeometrySamples();
                RefreshDynamicMarkers();
                nextRefreshTime = Time.unscaledTime + refreshInterval;
            }
        }

        private bool ValidateConfiguration()
        {
            bool valid = true;
            if (mapContentRoot == null)
            {
                Debug.LogError("[RealtimeMapHudPresenter] mapContentRoot is missing.", this);
                valid = false;
            }

            if (playerMarker == null)
            {
                Debug.LogError("[RealtimeMapHudPresenter] playerMarker is missing.", this);
                valid = false;
            }

            if (enemyMarkerTemplate == null)
            {
                Debug.LogError("[RealtimeMapHudPresenter] enemyMarkerTemplate is missing.", this);
                valid = false;
            }

            if (doorMarkerTemplate == null)
            {
                Debug.LogError("[RealtimeMapHudPresenter] doorMarkerTemplate is missing.", this);
                valid = false;
            }

            if (waveTimerTmpText == null && waveTimerText == null)
            {
                Debug.LogError("[RealtimeMapHudPresenter] waveTimerText is missing.", this);
                valid = false;
            }

            if (waveDirector == null)
            {
                Debug.LogError("[RealtimeMapHudPresenter] waveDirector is missing.", this);
                valid = false;
            }

            if (Mathf.Approximately(worldMin.x, worldMax.x) || Mathf.Approximately(worldMin.y, worldMax.y))
            {
                Debug.LogError("[RealtimeMapHudPresenter] world bounds are invalid.", this);
                valid = false;
            }

            if (playerCenteredHalfExtent.x <= 0f || playerCenteredHalfExtent.y <= 0f)
            {
                Debug.LogError("[RealtimeMapHudPresenter] player centered bounds are invalid.", this);
                valid = false;
            }

            return valid;
        }

        private void ResolveWaveDirector()
        {
            if (waveDirector == null)
            {
                waveDirector = FindFirstObjectByType<WaveDirector>(FindObjectsInactive.Include);
            }
        }

        private void ResolvePlayer()
        {
            PlayerCentralInputController inputController = FindFirstObjectByType<PlayerCentralInputController>(FindObjectsInactive.Exclude);
            if (inputController != null)
            {
                player = inputController.transform;
                return;
            }

            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                player = taggedPlayer.transform;
                return;
            }

            GameObject namedPlayer = GameObject.Find("Player");
            if (namedPlayer != null)
            {
                player = namedPlayer.transform;
                return;
            }

            if (player == null && Time.unscaledTime >= nextMissingPlayerLogTime)
            {
                Debug.LogWarning("[RealtimeMapHudPresenter] Player transform not found for map tracking.", this);
                nextMissingPlayerLogTime = Time.unscaledTime + 2f;
            }
        }

        private void RefreshPlayerMarker()
        {
            if (player == null)
            {
                playerMarker.gameObject.SetActive(false);
                return;
            }

            playerMarker.gameObject.SetActive(true);
            PlaceMarker(playerMarker, player.transform.position);
        }

        private void RefreshDynamicMarkers()
        {
            RefreshEnemyMarkers();
            RefreshDoorMarkers();
        }

        private void RefreshEnemyMarkers()
        {
            EnemyMeleeAttackController[] enemies = FindObjectsByType<EnemyMeleeAttackController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            int visibleCount = 0;
            for (int i = 0; i < enemies.Length && visibleCount < maxEnemyMarkers; i++)
            {
                EnemyMeleeAttackController enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || IsDead(enemy))
                {
                    continue;
                }

                RectTransform marker = GetMarker(enemyMarkers, enemyMarkerTemplate, visibleCount);
                marker.gameObject.SetActive(true);
                PlaceMarker(marker, enemy.transform.position);
                visibleCount++;
            }

            HideUnusedMarkers(enemyMarkers, visibleCount);
        }

        private void RefreshDoorMarkers()
        {
            MapExpansionDoorOpener[] doors = FindObjectsByType<MapExpansionDoorOpener>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            int visibleCount = 0;
            for (int i = 0; i < doors.Length; i++)
            {
                MapExpansionDoorOpener door = doors[i];
                if (door == null || !door.gameObject.activeInHierarchy)
                {
                    continue;
                }

                RectTransform marker = GetMarker(doorMarkers, doorMarkerTemplate, visibleCount);
                marker.gameObject.SetActive(true);
                PlaceMarker(marker, door.transform.position);
                Image image = marker.GetComponent<Image>();
                if (image != null)
                {
                    image.color = door.IsOpen ? new Color(0.25f, 1f, 0.55f, 0.95f) : new Color(1f, 0.82f, 0.25f, 0.95f);
                }

                visibleCount++;
            }

            HideUnusedMarkers(doorMarkers, visibleCount);
        }

        private void RefreshWorldGeometrySamples()
        {
            worldGeometrySamples.Clear();
            if (!showWorldGeometry)
            {
                HideUnusedGeometryImages(0);
                return;
            }

            Collider[] colliders = FindObjectsByType<Collider>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            List<GeometrySample> floors = new List<GeometrySample>();
            List<GeometrySample> ramps = new List<GeometrySample>();
            List<GeometrySample> walls = new List<GeometrySample>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (!TryCreateGeometrySample(candidate, out GeometrySample sample))
                {
                    continue;
                }

                switch (sample.Kind)
                {
                    case GeometryKind.Wall:
                        walls.Add(sample);
                        break;
                    case GeometryKind.Ramp:
                        ramps.Add(sample);
                        break;
                    default:
                        floors.Add(sample);
                        break;
                }
            }

            AddGeometrySamples(floors);
            AddGeometrySamples(ramps);
            AddGeometrySamples(walls);
        }

        private bool TryCreateGeometrySample(Collider candidate, out GeometrySample sample)
        {
            sample = default;
            if (candidate == null || !candidate.enabled || candidate.isTrigger || !candidate.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (candidate.GetComponentInParent<PlayerCentralInputController>() != null
                || candidate.GetComponentInParent<EnemyMeleeAttackController>() != null
                || candidate.GetComponentInParent<PlacementPoint>() != null
                || candidate.GetComponentInParent<TreasureChest>() != null
                || candidate.GetComponentInParent<SupportTruckShopInteraction>() != null)
            {
                return false;
            }

            GeometryKind kind = ResolveGeometryKind(candidate);
            if (kind == GeometryKind.None)
            {
                return false;
            }

            Bounds bounds = candidate.bounds;
            float width = Mathf.Max(bounds.size.x, 0.35f);
            float height = Mathf.Max(bounds.size.z, 0.35f);
            if (width > playerCenteredHalfExtent.x * 4f || height > playerCenteredHalfExtent.y * 4f)
            {
                return false;
            }

            sample = new GeometrySample(new Vector3(bounds.center.x, 0f, bounds.center.z), new Vector2(width, height), kind);
            return true;
        }

        private GeometryKind ResolveGeometryKind(Collider candidate)
        {
            string objectName = candidate.name;
            string parentName = candidate.transform.parent != null ? candidate.transform.parent.name : string.Empty;
            string combinedName = (objectName + " " + parentName).ToLowerInvariant();

            if (combinedName.Contains("floor") || combinedName.Contains("ground") || combinedName.Contains("platform") || combinedName.Contains("pad"))
            {
                return combinedName.Contains("ramp") ? GeometryKind.Ramp : GeometryKind.Floor;
            }

            if (combinedName.Contains("ramp"))
            {
                return GeometryKind.Ramp;
            }

            if (combinedName.Contains("wall")
                || combinedName.Contains("doorheader")
                || combinedName.Contains("blocker")
                || candidate.GetComponent<MapObstacle>() != null)
            {
                return GeometryKind.Wall;
            }

            return GeometryKind.None;
        }

        private void RefreshWorldGeometryImages()
        {
            if (!showWorldGeometry || mapContentRoot == null)
            {
                HideUnusedGeometryImages(0);
                return;
            }

            Rect rect = mapContentRoot.rect;
            for (int i = 0; i < worldGeometrySamples.Count; i++)
            {
                Image image = GetGeometryImage(i);
                GeometrySample sample = worldGeometrySamples[i];
                image.gameObject.SetActive(true);
                image.color = GetGeometryColor(sample.Kind);
                PlaceGeometryImage(image.rectTransform, sample, rect);
            }

            HideUnusedGeometryImages(worldGeometrySamples.Count);
        }

        private Image GetGeometryImage(int index)
        {
            EnsureWorldGeometryLayer();
            while (worldGeometryImages.Count <= index)
            {
                GameObject geometryObject = new GameObject("WorldGeometry_" + worldGeometryImages.Count.ToString("000"));
                geometryObject.transform.SetParent(worldGeometryLayer, false);
                RectTransform rect = geometryObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                Image image = geometryObject.AddComponent<Image>();
                image.raycastTarget = false;
                worldGeometryImages.Add(image);
            }

            return worldGeometryImages[index];
        }

        private void EnsureWorldGeometryLayer()
        {
            if (worldGeometryLayer != null || mapContentRoot == null)
            {
                return;
            }

            GameObject layerObject = new GameObject("WorldGeometryLayer");
            layerObject.transform.SetParent(mapContentRoot, false);
            layerObject.transform.SetAsFirstSibling();
            worldGeometryLayer = layerObject.AddComponent<RectTransform>();
            worldGeometryLayer.anchorMin = Vector2.zero;
            worldGeometryLayer.anchorMax = Vector2.one;
            worldGeometryLayer.pivot = new Vector2(0.5f, 0.5f);
            worldGeometryLayer.anchoredPosition = Vector2.zero;
            worldGeometryLayer.sizeDelta = Vector2.zero;
        }

        private void AddGeometrySamples(List<GeometrySample> samples)
        {
            for (int i = 0; i < samples.Count && worldGeometrySamples.Count < maxWorldGeometryShapes; i++)
            {
                worldGeometrySamples.Add(samples[i]);
            }
        }

        private void HideUnusedGeometryImages(int visibleCount)
        {
            for (int i = visibleCount; i < worldGeometryImages.Count; i++)
            {
                worldGeometryImages[i].gameObject.SetActive(false);
            }
        }

        private void PlaceGeometryImage(RectTransform target, GeometrySample sample, Rect mapRect)
        {
            Vector2 normalized = WorldToMapNormalized(sample.Center);
            Vector2 mapSize = ResolveMapWorldSize();
            target.anchoredPosition = new Vector2(
                (normalized.x - 0.5f) * mapRect.width,
                (normalized.y - 0.5f) * mapRect.height);
            target.sizeDelta = new Vector2(
                Mathf.Max(2f, mapRect.width * sample.Size.x / mapSize.x),
                Mathf.Max(2f, mapRect.height * sample.Size.y / mapSize.y));
        }

        private Color GetGeometryColor(GeometryKind kind)
        {
            switch (kind)
            {
                case GeometryKind.Wall:
                    return wallGeometryColor;
                case GeometryKind.Ramp:
                    return rampGeometryColor;
                default:
                    return floorGeometryColor;
            }
        }

        private void RefreshWaveText()
        {
            if (waveDirector.IsHoldingWaveReward)
            {
                SetWaveTimerText("REWARD SELECT");
                return;
            }

            if (waveDirector.IsRunningWave)
            {
                SetWaveTimerText($"WAVE {waveDirector.CurrentWaveNumber:00} | RUN");
                return;
            }

            if (waveDirector.IsWaitingForWave)
            {
                SetWaveTimerText($"WAVE {waveDirector.CurrentWaveNumber:00} | {waveDirector.CurrentWaveRemainingSeconds:0}s");
                return;
            }

            SetWaveTimerText("WAVE READY");
        }

        private void SetWaveTimerText(string value)
        {
            if (waveTimerTmpText != null)
            {
                waveTimerTmpText.text = value;
                return;
            }

            if (waveTimerText != null)
            {
                waveTimerText.text = value;
            }
        }

        private bool IsDead(EnemyMeleeAttackController enemy)
        {
            Health health = enemy.GetComponent<Health>();
            return health != null && !health.IsAlive;
        }

        private RectTransform GetMarker(List<RectTransform> markers, RectTransform template, int index)
        {
            while (markers.Count <= index)
            {
                RectTransform marker = Instantiate(template, mapContentRoot);
                marker.name = template.name + "_" + markers.Count.ToString("00");
                markers.Add(marker);
            }

            return markers[index];
        }

        private void HideUnusedMarkers(List<RectTransform> markers, int visibleCount)
        {
            for (int i = visibleCount; i < markers.Count; i++)
            {
                markers[i].gameObject.SetActive(false);
            }
        }

        private void PlaceMarker(RectTransform marker, Vector3 worldPosition)
        {
            Rect rect = mapContentRoot.rect;
            Vector2 normalized = WorldToMapNormalized(worldPosition);

            marker.anchoredPosition = new Vector2(
                (Mathf.Clamp01(normalized.x) - 0.5f) * rect.width,
                (Mathf.Clamp01(normalized.y) - 0.5f) * rect.height);
        }

        private Vector2 WorldToMapNormalized(Vector3 worldPosition)
        {
            if (centerOnPlayer && player != null)
            {
                Vector3 playerPosition = player.position;
                return new Vector2(
                    0.5f + ((worldPosition.x - playerPosition.x) / (playerCenteredHalfExtent.x * 2f)),
                    0.5f + ((worldPosition.z - playerPosition.z) / (playerCenteredHalfExtent.y * 2f)));
            }

            return new Vector2(
                Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x),
                Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z));
        }

        private Vector2 ResolveMapWorldSize()
        {
            if (centerOnPlayer && player != null)
            {
                return playerCenteredHalfExtent * 2f;
            }

            return new Vector2(
                Mathf.Max(1f, Mathf.Abs(worldMax.x - worldMin.x)),
                Mathf.Max(1f, Mathf.Abs(worldMax.y - worldMin.y)));
        }

        private readonly struct GeometrySample
        {
            public GeometrySample(Vector3 center, Vector2 size, GeometryKind kind)
            {
                Center = center;
                Size = size;
                Kind = kind;
            }

            public readonly Vector3 Center;
            public readonly Vector2 Size;
            public readonly GeometryKind Kind;
        }

        private enum GeometryKind
        {
            None,
            Floor,
            Wall,
            Ramp
        }
    }
}
