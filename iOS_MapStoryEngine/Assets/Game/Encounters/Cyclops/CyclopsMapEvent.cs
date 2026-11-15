using Mapbox.Examples;
using Mapbox.Unity.MeshGeneration.Factories;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Odyssey.Cyclops
{
    public sealed class CyclopsMapEvent : MonoBehaviour
    {
        internal const float SpawnRadius = 1f;
        internal const float MinSpawnDistance = 0.95f;
        internal const float CyclopsEntryDistance = 0.85f;
        internal const float MedusaRevealDistance = 0.85f;
        internal const float IthacaRevealDistance = 0.60f;
        private const float IthacaSpawnRadius = 1.50f;
        private const float IthacaMinSpawnDistance = 1.35f;
        private const float MedusaRearmDistance = 1.00f;
        private const float CyclopsAudioRange = 1.20f;
        private const float CyclopsFullVolumeDistance = 0.38f;
        private const float MedusaAudioRange = 1.30f;
        private const float MaxApproachVolume = 0.45f;
        private const float PreviewCenterHeight = 0.30f;
        private const float FlagBaseBelowCamera = 1.15f;

        [SerializeField] private Texture medusaImage;

        private readonly VoyageSite cyclops = new VoyageSite();
        private readonly VoyageSite medusa = new VoyageSite();
        private readonly VoyageSite ithaca = new VoyageSite();
        private ARSessionOrigin arOrigin;
        private ARSession arSession;
        private ARAnchorManager anchorManager;
        private Camera rearCamera;
        private Camera mapCamera;
        private TouchCamera mapTouch;
        private VoyageInterface voyage;
        private Canvas storyCanvas;
        private GraphicRaycaster storyRaycaster;
        private StoryManager story;
        private AudioSource cyclopsProximityAudio;
        private AudioSource medusaProximityAudio;
        private GameObject ithacaPrefab;
        private Transform ithacaFlag;
        private Vector3 ithacaFlagScale;
        private float ithacaRevealAt;
        private float cyclopsPreviewHeight;
        private bool sitesPlaced;
        private bool practiceFight;

        private VoyageSite activeSite;
        private CyclopsEncounter cyclopsPrefab;
        private CyclopsEncounter cyclopsPreview;
        private CyclopsEncounter activeCyclops;
        private MedusaEncounter medusaEncounter;
        private IFaceSignals editorFace;
        private bool savedArSession;
        private bool savedArCamera;
        private bool savedMapCamera;
        private bool savedMapTouch;
        private bool savedCanvas;
        private bool savedRaycaster;
        private bool savedStory;

        private Canvas overlay;
        private RectTransform safePanel;
        private Button faceCyclops;
        private Text resultMessage;

        private bool CanExplore => voyage != null && voyage.CanExplore && activeSite == null
            && VoyageState.Health > 0 && Application.isFocused
            && (Application.isEditor || ARSession.state == ARSessionState.SessionTracking);

        private void Start()
        {
            GameObject mapTools = null;
            Title title = null;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (title == null) title = root.GetComponentInChildren<Title>(true);
                if (mapTouch == null) mapTouch = root.GetComponentInChildren<TouchCamera>(true);
                if (arOrigin == null) arOrigin = root.GetComponentInChildren<ARSessionOrigin>(true);
                if (arSession == null) arSession = root.GetComponentInChildren<ARSession>(true);
                if (mapTools != null) continue;
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect.name != "MapToolsPanel_5") continue;
                    mapTools = rect.gameObject;
                    storyCanvas = rect.GetComponentInParent<Canvas>();
                    break;
                }
            }

            rearCamera = arOrigin == null ? null : arOrigin.camera;
            mapCamera = mapTouch == null ? null : mapTouch.GetComponent<Camera>();
            story = GetComponent<StoryManager>();
            cyclopsPrefab = Resources.Load<CyclopsEncounter>("Odyssey/CyclopsEncounter");
            ithacaPrefab = Resources.Load<GameObject>("Odyssey/Ithaca/IthacaBanner");
            if (ithacaPrefab == null)
                Debug.LogError("The Ithaca flag model is missing.", this);
            if (title == null || mapCamera == null || rearCamera == null || arSession == null
                || medusaImage == null || cyclopsPrefab == null || storyCanvas == null || mapTools == null || story == null)
            {
                Debug.LogError("The voyage is missing a boss asset or scene reference.", this);
                enabled = false;
                return;
            }

            storyRaycaster = storyCanvas.GetComponent<GraphicRaycaster>();
            medusaEncounter = gameObject.AddComponent<MedusaEncounter>();
            medusaEncounter.medusaImage = medusaImage;
            cyclopsProximityAudio = OdysseyEncounterAudio.CreateLoop(gameObject, OdysseyEncounterAudio.CyclopsProximity, 0f);
            medusaProximityAudio = OdysseyEncounterAudio.CreateLoop(gameObject, OdysseyEncounterAudio.MedusaProximity, 0f);
            gameObject.AddComponent<FaceTrackingServices>();
#if UNITY_EDITOR
            if (Services.Face == null)
                Services.Face = editorFace = gameObject.AddComponent<KeyboardFaceSignals>();
#endif
            GameEvents.EncounterEnded += OnEncounterEnded;
            CreateEncounterHud();
            OdysseyTheme.StyleLegacyCanvas(storyCanvas.transform);
            VoyageRoute.Prepare(gameObject.scene, mapCamera, mapTouch, mapTools, title);
            voyage = VoyageInterface.Create(arSession, rearCamera,
                () => OpenEncounter(cyclops, true), () => OpenEncounter(medusa, true));
        }

        private void LateUpdate()
        {
            bool explore = CanExplore;
            overlay.gameObject.SetActive(explore || VoyageState.Won);
            if (!explore)
            {
                SetApproachVolume(cyclopsProximityAudio, 0f);
                SetApproachVolume(medusaProximityAudio, 0f);
                if (cyclopsPreview != null)
                    cyclopsPreview.gameObject.SetActive(false);
                if (ithacaFlag != null)
                    UpdateIthacaFlag();
                return;
            }

            Transform trackingSpace = arOrigin.transform;
            Vector3 player = trackingSpace.InverseTransformPoint(rearCamera.transform.position);
            if (!sitesPlaced && !TryPlaceSites(player))
                return;

            CyclopsView.FitSafeArea(safePanel);
            if (TryReachIthaca(player))
                return;

            UpdateCyclops(player);
            CheckMedusa(player);
        }

        private bool TryPlaceSites(Vector3 player)
        {
            float radius = Random.Range(MinSpawnDistance, SpawnRadius);
            if (!TryOpeningPosition(player, Random.Range(0.43f, 0.57f), radius, out Vector3 position)
                && !TryOpeningPosition(player, 0.5f, radius, out position))
                return false;

            cyclops.LocalPosition = position;
            Vector3 offset = position - player;
            float bearing = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

            // Separation keeps both proximity zones from triggering on the same step.
            float medusaOffset = Random.Range(135f, 225f);
            medusa.LocalPosition = SitePosition(player, bearing + medusaOffset,
                Random.Range(MinSpawnDistance, SpawnRadius));

            // Place Ithaca in the middle of the wider arc between the bosses so their reveal areas stay separate.
            float ithacaBearing = bearing + (medusaOffset < 180f ? medusaOffset + 360f : medusaOffset) * 0.5f;
            ithaca.LocalPosition = SitePosition(player, ithacaBearing, Random.Range(IthacaMinSpawnDistance, IthacaSpawnRadius));
            sitesPlaced = true;

            if (!Application.isEditor)
            {
                anchorManager = arOrigin.GetComponent<ARAnchorManager>();
                if (anchorManager == null)
                    anchorManager = arOrigin.gameObject.AddComponent<ARAnchorManager>();
                Anchor(cyclops);
                Anchor(medusa);
                Anchor(ithaca);
            }
            return true;
        }

        private bool TryOpeningPosition(Vector3 player, float viewportX, float radius, out Vector3 position)
        {
            Transform trackingSpace = arOrigin.transform;
            Ray ray = rearCamera.ViewportPointToRay(new Vector3(viewportX, 0.48f, 0f));
            Vector3 direction = trackingSpace.InverseTransformDirection(ray.direction);
            position = OpeningPosition(player, direction, radius, out float centerHeight);
            if (WalkingDistance(player, position) < MinSpawnDistance - 0.01f)
                return false;

            Vector3 worldCenter = trackingSpace.TransformPoint(position + Vector3.up * centerHeight);
            Vector3 viewport = rearCamera.WorldToViewportPoint(worldCenter);
            if (viewport.z <= 0f || viewport.x < 0.28f || viewport.x > 0.72f || viewport.y < 0.22f || viewport.y > 0.78f)
                return false;

            cyclopsPreviewHeight = centerHeight - PreviewCenterHeight;
            return true;
        }

        internal static Vector3 OpeningPosition(Vector3 player, Vector3 direction, float radius, out float centerHeight)
        {
            Vector2 horizontal = new Vector2(direction.x, direction.z);
            float magnitude = horizontal.magnitude;
            if (magnitude < 0.2f)
            {
                centerHeight = 0f;
                return player;
            }

            float scale = radius / magnitude;
            centerHeight = Mathf.Clamp(direction.y * scale, -0.45f, 0.23f);
            return player + new Vector3(direction.x * scale, 0f, direction.z * scale);
        }

        private void Anchor(VoyageSite site)
        {
            if (anchorManager.isActiveAndEnabled && anchorManager.subsystem != null)
                site.Anchor = anchorManager.AddAnchor(new Pose(arOrigin.transform.TransformPoint(site.LocalPosition),
                    arOrigin.transform.rotation));
        }

        internal static Vector3 SitePosition(Vector3 start, float bearing, float radius)
        {
            float radians = bearing * Mathf.Deg2Rad;
            return start + new Vector3(Mathf.Sin(radians) * radius, 0f, Mathf.Cos(radians) * radius);
        }

        internal static float WalkingDistance(Vector3 player, Vector3 site)
        {
            return new Vector2(player.x - site.x, player.z - site.z).magnitude;
        }

        private Vector3 PositionOf(VoyageSite site)
        {
            if (site.Anchor != null)
                site.LocalPosition = arOrigin.transform.InverseTransformPoint(site.Anchor.transform.position);
            return site.LocalPosition;
        }

        private bool TryReachIthaca(Vector3 player)
        {
            if (ithacaPrefab == null || WalkingDistance(player, PositionOf(ithaca)) > IthacaRevealDistance)
                return false;

            Transform trackingSpace = arOrigin.transform;
            Vector3 position = IthacaFlagPosition();
            Vector3 facing = Vector3.ProjectOnPlane(rearCamera.transform.position - position, trackingSpace.up);
            Quaternion rotation = facing.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(facing, trackingSpace.up) : trackingSpace.rotation;
            ithacaFlag = Instantiate(ithacaPrefab, position, rotation, transform).transform;
            ithacaFlagScale = ithacaFlag.localScale;
            ithacaRevealAt = Time.unscaledTime;
            UpdateIthacaFlag();

            faceCyclops.gameObject.SetActive(false);
            if (cyclopsPreview != null)
                cyclopsPreview.gameObject.SetActive(false);
            resultMessage.text = "You reached Ithaca";
            resultMessage.transform.parent.gameObject.SetActive(true);
            StopApproachAudio(cyclopsProximityAudio);
            StopApproachAudio(medusaProximityAudio);
            var arrival = Resources.Load<AudioClip>("Odyssey/IthacaArrival");
            if (arrival != null)
                OdysseyEncounterAudio.CreateSource(gameObject).PlayOneShot(arrival, 0.75f);
            VoyageState.Complete();
            return true;
        }

        private Vector3 IthacaFlagPosition()
        {
            return arOrigin.transform.TransformPoint(PositionOf(ithaca) - Vector3.up * FlagBaseBelowCamera);
        }

        private void UpdateIthacaFlag()
        {
            float progress = (Time.unscaledTime - ithacaRevealAt) / 0.4f;
            ithacaFlag.localScale = ithacaFlagScale * Mathf.SmoothStep(0.12f, 1f, progress);
            ithacaFlag.position = IthacaFlagPosition();
            CyclopsView.FitSafeArea(safePanel);
        }

        private void UpdateCyclops(Vector3 player)
        {
            if (cyclops.Cleared)
            {
                faceCyclops.gameObject.SetActive(false);
                StopApproachAudio(cyclopsProximityAudio);
                return;
            }

            Vector3 position = PositionOf(cyclops);
            float distance = WalkingDistance(player, position);
            SetApproachVolume(cyclopsProximityAudio, ApproachVolume(distance, CyclopsAudioRange, CyclopsFullVolumeDistance));

            if (cyclopsPreview == null)
                cyclopsPreview = Instantiate(cyclopsPrefab, transform);
            cyclopsPreview.gameObject.SetActive(true);
            Vector3 world = arOrigin.transform.TransformPoint(position);
            cyclopsPreview.ShowPreview(world + arOrigin.transform.up * cyclopsPreviewHeight, rearCamera, arOrigin.transform.up);
            faceCyclops.gameObject.SetActive(distance <= CyclopsEntryDistance);
        }

        internal static float ApproachVolume(float distance, float range, float fullVolumeDistance)
        {
            float falloff = 1f - Mathf.InverseLerp(fullVolumeDistance, range, distance);
            return Mathf.SmoothStep(0f, MaxApproachVolume, falloff);
        }

        private static void SetApproachVolume(AudioSource source, float target)
        {
            if (source == null)
                return;

            source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 2f);
            if (source.volume <= 0f && source.isPlaying)
                source.Stop();
            else if (source.volume > 0f && !source.isPlaying)
                source.Play();
        }

        private static void StopApproachAudio(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.volume = 0f;
        }

        private void EnterCyclops()
        {
            if (!CanExplore || !sitesPlaced || cyclops.Cleared)
                return;
            Vector3 player = arOrigin.transform.InverseTransformPoint(rearCamera.transform.position);
            if (WalkingDistance(player, PositionOf(cyclops)) <= CyclopsEntryDistance)
                OpenEncounter(cyclops, false);
        }

        private void CheckMedusa(Vector3 player)
        {
            if (medusa.Cleared || Services.Face == null)
            {
                SetApproachVolume(medusaProximityAudio, 0f);
                return;
            }

            float distance = WalkingDistance(player, PositionOf(medusa));
            if (distance > MedusaRearmDistance)
                medusa.WaitForExit = false;

            float volume = medusa.WaitForExit ? 0f : ApproachVolume(distance, MedusaAudioRange, MedusaRevealDistance);
            SetApproachVolume(medusaProximityAudio, volume);
            if (medusa.WaitForExit || distance > MedusaRevealDistance)
                return;

            medusa.WaitForExit = true;
            OpenEncounter(medusa, false);
        }

        private void OpenEncounter(VoyageSite site, bool practice)
        {
            if (VoyageState.Won || activeSite != null || (site == medusa && Services.Face == null))
                return;

            BeginEncounter(site, practice);
            if (site == medusa)
            {
                if (practice)
                    medusaEncounter.BeginPractice();
                else
                    medusaEncounter.Begin();
                return;
            }

            if (!practice && cyclopsPreview != null)
            {
                activeCyclops = cyclopsPreview;
                cyclopsPreview = null;
                activeCyclops.gameObject.SetActive(true);
            }
            else
                activeCyclops = Instantiate(cyclopsPrefab, transform);

            activeCyclops.Begin(rearCamera, arOrigin.transform, Application.isEditor, FinishCyclops,
                practice ? "Return to voyage" : "Back to voyage", !practice);
            if (!practice)
                GameEvents.RaiseEncounterStarted(EncounterType.Cyclops);
        }

        private void BeginEncounter(VoyageSite site, bool practice)
        {
            StopApproachAudio(cyclopsProximityAudio);
            StopApproachAudio(medusaProximityAudio);
            activeSite = site;
            practiceFight = practice;
            voyage.SetEncounter(true, practice, site == medusa);
            savedArSession = arSession.enabled;
            savedArCamera = rearCamera.enabled;
            savedMapCamera = mapCamera.enabled;
            savedMapTouch = mapTouch.enabled;
            savedCanvas = storyCanvas.enabled;
            savedRaycaster = storyRaycaster != null && storyRaycaster.enabled;
            savedStory = story.enabled;
            story.enabled = false;
            storyCanvas.enabled = false;
            if (storyRaycaster != null) storyRaycaster.enabled = false;
            mapTouch.enabled = false;
            mapCamera.enabled = false;
            overlay.gameObject.SetActive(false);
            if (cyclopsPreview != null) cyclopsPreview.gameObject.SetActive(false);
            arSession.enabled = true;
            rearCamera.enabled = true;
        }

        private void FinishCyclops(bool won)
        {
            activeCyclops.gameObject.SetActive(false);
            Destroy(activeCyclops.gameObject);
            activeCyclops = null;
            if (!practiceFight)
                GameEvents.RaiseEncounterEnded(EncounterType.Cyclops, won);
            ReturnToVoyage(won);
        }

        private void OnEncounterEnded(EncounterType type, bool won)
        {
            if (type != EncounterType.Medusa || activeSite != medusa)
                return;

            ReturnToVoyage(won);
        }

        private void ReturnToVoyage(bool won)
        {
            if (!practiceFight)
                activeSite.Cleared = won;
            activeSite = null;
            arSession.enabled = savedArSession;
            rearCamera.enabled = savedArCamera;
            storyCanvas.enabled = savedCanvas;
            if (storyRaycaster != null) storyRaycaster.enabled = savedRaycaster;
            story.enabled = savedStory;
            mapCamera.enabled = savedMapCamera;
            mapTouch.enabled = savedMapTouch;
            voyage.SetEncounter(false);
        }

        private void CreateEncounterHud()
        {
            var hud = new GameObject("Nearby encounters", typeof(RectTransform));
            hud.transform.SetParent(transform, false);
            overlay = CyclopsView.Overlay(hud);
            safePanel = OdysseyTheme.Inset(hud.transform, "Safe area", 0);

            var button = OdysseyTheme.Rect(safePanel, "Face Cyclops", Vector2.zero, Vector2.right,
                new Vector2(26f, 34f), new Vector2(-26f, 86f));
            faceCyclops = OdysseyTheme.Button(button, EnterCyclops, true);
            OdysseyTheme.Label(OdysseyTheme.Inset(button, "Label", 4), "Face Cyclops", 22, OdysseyTheme.Ink);

            var result = OdysseyTheme.Rect(safePanel, "Clue", Vector2.up, Vector2.one,
                new Vector2(24f, -162f), new Vector2(-24f, -78f));
            OdysseyTheme.Tint(result, OdysseyTheme.Ink);
            OdysseyTheme.Frame(result, OdysseyTheme.Bronze, 0);
            resultMessage = OdysseyTheme.Label(OdysseyTheme.Inset(result, "Result text", 12), "", 17, OdysseyTheme.Ivory);

            faceCyclops.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            hud.SetActive(false);
        }

        private void OnDisable()
        {
            StopApproachAudio(cyclopsProximityAudio);
            StopApproachAudio(medusaProximityAudio);
        }

        private void OnDestroy()
        {
            GameEvents.EncounterEnded -= OnEncounterEnded;
            if (editorFace != null && ReferenceEquals(Services.Face, editorFace))
                Services.Face = null;
        }

        private sealed class VoyageSite
        {
            public Vector3 LocalPosition;
            public ARAnchor Anchor;
            public bool Cleared;
            public bool WaitForExit;
        }
    }
}
