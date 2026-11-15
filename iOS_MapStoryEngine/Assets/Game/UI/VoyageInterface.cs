using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace Odyssey
{
    internal sealed class VoyageInterface : MonoBehaviour
    {
        private enum Page { None, Oracle, Clues, Rules, Locations }

        private static readonly string[] PageNames = { "", "Oracle", "Clues", "Rules", "Included Locations" };
        private const string IncludedLocations = "SWSH\n\nUH\n\nSIER\n\nPKH";
        private const string Rules =
            "• You play as Odysseus. Like your mythical counterpart, you start from Troy, but your journey back to Ithaca is less than comfortable.\n\n" +
            "• Troy is automatically set as your starting landmark. Which landmark represents Ithaca is a mystery.\n\n" +
            "• Try your luck by visiting the landmarks listed in Included Locations. You might find Ithaca or stumble upon an eldritch horror.\n\n" +
            "• Face one of the monsters at an included location to earn a clue. Your journal keeps the clues you earn.\n\n" +
            "• If you die, return to the designated Troy landmark to restart the game.\n\n" +
            "• The story ends when you reach Ithaca.\n\n" +
            "• Ask the Oracle of Delphi if you have questions.\n\n" +
            "Approach the Cyclops and tap Face Cyclops. Tilt your phone up to raise the shield. Match the gold flash for a perfect block, then tap the exposed Cyclops.";

        private RectTransform safeArea;
        private RectTransform landingSafeArea;
        private RectTransform scrollArea;
        private RectTransform modalPanel;
        private RectTransform clueToast;
        private Canvas hudCanvas;
        private OracleChat oracle;
        private ClueJournalView journal;
        private float clueToastUntil;
        private RectTransform healthRoot;
        private CanvasGroup landing;
        private CanvasGroup navigation;
        private CanvasGroup modal;
        private VoyageHealthBar health;
        private UnityEngine.UI.Text tracking;
        private UnityEngine.UI.Text heading;
        private UnityEngine.UI.Text content;
        private UnityEngine.UI.Text actionLabel;
        private UnityEngine.UI.Button action;
        private ScrollRect scroll;
        private ARSession session;
        private ARCameraManager cameraManager;
        private Camera rearCamera;
        private AudioSource backgroundMusic;
        private UnityAction practice;
        private UnityAction practiceMedusa;
        private Page page;
        private bool launching;
        private bool fighting;
        private bool cameraUnavailable;
        private Rect previousArea;
        private Vector2Int previousScreen;
        private string trackingMessage;
        private float nextPageRefresh;
        private UnityAction pageAction;

        internal bool CanExplore => VoyageState.Started && !VoyageState.Won && !fighting
            && !cameraUnavailable && session.enabled && rearCamera.enabled
            && page == Page.None;

        internal static VoyageInterface Create(ARSession session, Camera camera,
            UnityAction practice, UnityAction practiceMedusa)
        {
            var owner = new GameObject("Return to Ithaca UI", typeof(RectTransform));
            var ui = owner.AddComponent<VoyageInterface>();
            ui.session = session;
            ui.rearCamera = camera;
            ui.cameraManager = camera.GetComponent<ARCameraManager>();
            ui.practice = practice;
            ui.practiceMedusa = practiceMedusa;
            ui.Build(owner);
            ui.backgroundMusic = OdysseyEncounterAudio.CreateLoop(owner, OdysseyEncounterAudio.VoyageBackground, 0f);
            if (ui.backgroundMusic != null)
                ui.backgroundMusic.Play();
            if (VoyageState.Started)
                ui.Play();
            return ui;
        }

        private void Build(GameObject owner)
        {
            hudCanvas = owner.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 120;
            var scaler = owner.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            owner.AddComponent<GraphicRaycaster>();

            safeArea = OdysseyTheme.Inset(transform, "Safe area", 0);
            healthRoot = OdysseyTheme.Inset(safeArea, "Health HUD", 0);
            health = new VoyageHealthBar(healthRoot);
            healthRoot.gameObject.SetActive(false);
            var status = OdysseyTheme.Rect(healthRoot, "Tracking status", Vector2.up, Vector2.one,
                new Vector2(16, -102), new Vector2(-16, -72));
            OdysseyTheme.Tint(status, OdysseyTheme.Ink);
            tracking = OdysseyTheme.Label(OdysseyTheme.Inset(status, "Status text", 1), "", 14, OdysseyTheme.Ivory);
            status.gameObject.SetActive(false);

            BuildNavigation();
            BuildModal();
            healthRoot.SetAsLastSibling();
            clueToast = OdysseyTheme.Rect(safeArea, "New clue notification", Vector2.up, Vector2.one,
                new Vector2(16, -112), new Vector2(-16, -72));
            OdysseyTheme.Tint(clueToast, OdysseyTheme.DeepSea);
            OdysseyTheme.Frame(clueToast, OdysseyTheme.Bronze, 0);
            OdysseyTheme.Label(OdysseyTheme.Inset(clueToast, "Message", 5),
                "New Clue Discovered", 17, OdysseyTheme.Gold);
            clueToast.gameObject.SetActive(false);
            GameEvents.ClueUnlocked += ShowClueNotification;
            BuildLanding();
            FitScreen();

            session.enabled = false;
            rearCamera.enabled = true;
            rearCamera.backgroundColor = OdysseyTheme.Ink;
            if (cameraManager != null)
                cameraManager.enabled = false;
        }

        private void BuildLanding()
        {
            RectTransform screen = OdysseyTheme.Inset(transform, "Landing", 0);
            landing = screen.gameObject.AddComponent<CanvasGroup>();
            OdysseyTheme.Tint(screen, OdysseyTheme.Ink, true);
            var artwork = Resources.Load<Texture2D>("Odyssey/IthacaVoyage");
            if (artwork != null)
            {
                var image = OdysseyTheme.Inset(screen, "Ionian sea", 0).gameObject.AddComponent<RawImage>();
                image.texture = artwork;
                image.rectTransform.pivot = new Vector2(0.5f, 0.29f);
                image.raycastTarget = false;
                var fit = image.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = (float)artwork.width / artwork.height;
            }

            var landingSafe = OdysseyTheme.Inset(screen, "Landing safe area", 0);
            landingSafeArea = landingSafe;
            OdysseyTheme.Frame(landingSafe, OdysseyTheme.Bronze, 12);
            OdysseyTheme.Frame(landingSafe, new Color(0.82f, 0.69f, 0.41f, 0.40f), 17);

            var title = OdysseyTheme.Rect(landingSafe, "Return to Ithaca", new Vector2(0, 0.55f),
                new Vector2(1, 0.55f), new Vector2(28, -46), new Vector2(-28, 76));
            OdysseyTheme.Label(OdysseyTheme.Rect(title, "Return to", Vector2.up, Vector2.one,
                new Vector2(0, -44), Vector2.zero), "R E T U R N   T O", 22, OdysseyTheme.Ivory);
            var ithaca = OdysseyTheme.Label(OdysseyTheme.Rect(title, "Ithaca", Vector2.zero, Vector2.right,
                Vector2.zero, new Vector2(0, 84)), "Ithaca", 76, OdysseyTheme.Ivory);
            ithaca.resizeTextForBestFit = true;
            ithaca.resizeTextMinSize = 44;
            ithaca.resizeTextMaxSize = 76;
            var shadow = ithaca.gameObject.AddComponent<Shadow>();
            shadow.effectColor = OdysseyTheme.Ink;
            shadow.effectDistance = new Vector2(1, -2);

            var center = new Vector2(0.5f, 0.55f);
            OdysseyOrnament.Draw(OdysseyTheme.Rect(landingSafe, "Greek key", center, center,
                new Vector2(-93, -58), new Vector2(93, -48)), OdysseyOrnament.Design.Meander, OdysseyTheme.Gold);
            var play = OdysseyTheme.Rect(landingSafe, "Play", center, center,
                new Vector2(-100, -134), new Vector2(100, -78));
            OdysseyTheme.Button(play, Play, true);
            OdysseyTheme.Label(OdysseyTheme.Inset(play, "Play label", 5), "Play", 24, OdysseyTheme.Ink);
            var cyclopsTest = OdysseyTheme.Rect(landingSafe, "Test Cyclops", center, center,
                new Vector2(-122, -189), new Vector2(-5, -145));
            OdysseyTheme.Button(cyclopsTest, () => StartCoroutine(StartPractice(practice)));
            OdysseyTheme.Label(OdysseyTheme.Inset(cyclopsTest, "Label", 4), "Test Cyclops", 16, OdysseyTheme.Ivory);
            var medusaTest = OdysseyTheme.Rect(landingSafe, "Test Medusa", center, center,
                new Vector2(5, -189), new Vector2(122, -145));
            OdysseyTheme.Button(medusaTest, () => StartCoroutine(StartPractice(practiceMedusa)));
            OdysseyTheme.Label(OdysseyTheme.Inset(medusaTest, "Label", 4), "Test Medusa", 16, OdysseyTheme.Ivory);
            landing.alpha = 0;
            StartCoroutine(FadeInLanding());
        }

        private IEnumerator FadeInLanding()
        {
            // The opaque backdrop is present on the first frame. Only the content eases in.
            landing.alpha = 1;
            CanvasGroup contentGroup = landingSafeArea.gameObject.AddComponent<CanvasGroup>();
            contentGroup.alpha = 0;
            while (!launching && contentGroup.alpha < 1)
            {
                contentGroup.alpha = Mathf.MoveTowards(contentGroup.alpha, 1, Time.unscaledDeltaTime * 2.5f);
                yield return null;
            }
            contentGroup.alpha = 1;
        }

        private void BuildNavigation()
        {
            var rail = OdysseyTheme.Rect(safeArea, "AR navigation", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-98, -148), new Vector2(-14, 148));
            navigation = rail.gameObject.AddComponent<CanvasGroup>();
            SetVisible(navigation, false);
            var stack = rail.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 7;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = true;
            stack.childControlWidth = stack.childControlHeight = true;

            for (int i = 1; i < PageNames.Length; i++)
            {
                Page target = (Page)i;
                var slot = OdysseyTheme.Inset(rail, PageNames[i], 0);
                OdysseyTheme.Button(slot, () => Open(target));
                var top = new Vector2(0.5f, 1);
                OdysseyOrnament.Draw(OdysseyTheme.Rect(slot, "Icon", top, top,
                    new Vector2(-12, -32), new Vector2(12, -8)), (OdysseyOrnament.Design)i, OdysseyTheme.Gold);
                OdysseyTheme.Label(OdysseyTheme.Rect(slot, "Label", Vector2.zero, Vector2.right,
                    new Vector2(4, 3), new Vector2(-4, 36)),
                    PageNames[i], 13, OdysseyTheme.Ivory);
            }
        }

        private void BuildModal()
        {
            var overlay = OdysseyTheme.Inset(safeArea, "Voyage panel", 0);
            modal = overlay.gameObject.AddComponent<CanvasGroup>();
            var backdrop = OdysseyTheme.Inset(overlay, "Close backdrop", 0);
            var dim = OdysseyTheme.Tint(backdrop, new Color(0.02f, 0.06f, 0.07f, 0.52f), true);
            var dismiss = backdrop.gameObject.AddComponent<UnityEngine.UI.Button>();
            dismiss.targetGraphic = dim;
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(() => Open(Page.None));

            var panel = OdysseyTheme.Rect(overlay, "Journal page", Vector2.zero, new Vector2(1, 0.60f),
                new Vector2(14, 14), new Vector2(-14, 0));
            modalPanel = panel;
            OdysseyTheme.Tint(panel, OdysseyTheme.Ink, true);
            OdysseyTheme.Frame(panel, OdysseyTheme.Bronze, 0);
            OdysseyOrnament.Draw(OdysseyTheme.Rect(panel, "Meander", Vector2.up, Vector2.one,
                new Vector2(16, -74), new Vector2(-16, -64)), OdysseyOrnament.Design.Meander, OdysseyTheme.Bronze);
            heading = OdysseyTheme.Label(OdysseyTheme.Rect(panel, "Panel title", Vector2.up, Vector2.one,
                new Vector2(16, -60), new Vector2(-86, -12)), "", 26, OdysseyTheme.Ivory, TextAnchor.MiddleLeft);
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 18;
            heading.resizeTextMaxSize = 26;
            var close = OdysseyTheme.Rect(panel, "Close", Vector2.one, Vector2.one,
                new Vector2(-74, -58), new Vector2(-13, -12));
            OdysseyTheme.Button(close, () => Open(Page.None));
            OdysseyTheme.Label(OdysseyTheme.Inset(close, "Close label", 2), "Close", 14, OdysseyTheme.Ivory);

            scrollArea = OdysseyTheme.Rect(panel, "Scroll area", Vector2.zero, Vector2.one,
                new Vector2(17, 75), new Vector2(-17, -84));
            scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = OdysseyTheme.Inset(scrollArea, "Viewport", 0);
            OdysseyTheme.Tint(viewport, Color.clear, true);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = OdysseyTheme.Rect(viewport, "Text", Vector2.up, Vector2.one, Vector2.zero, Vector2.zero);
            text.pivot = new Vector2(0.5f, 1);
            content = OdysseyTheme.Label(text, "", 19, OdysseyTheme.Ivory, TextAnchor.UpperLeft);
            content.lineSpacing = 1.16f;
            content.supportRichText = false;
            var fit = text.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = text;
            journal = new ClueJournalView(viewport);

            var footer = OdysseyTheme.Rect(panel, "Panel action", Vector2.zero, Vector2.right,
                new Vector2(16, 14), new Vector2(-16, 64));
            action = OdysseyTheme.Button(footer, () => pageAction?.Invoke(), true);
            actionLabel = OdysseyTheme.Label(OdysseyTheme.Inset(footer, "Action label", 6), "", 18, OdysseyTheme.Ink);
            oracle = gameObject.AddComponent<OracleChat>();
            oracle.Build(panel);
            SetVisible(modal, false);
        }

        private void Play()
        {
            if (launching || fighting)
                return;
            launching = true;
            VoyageState.Begin();
            landing.interactable = false;
            StartCoroutine(StartCamera());
        }

        private IEnumerator StartCamera()
        {
            yield return EnableCamera();
            healthRoot.gameObject.SetActive(true);
            SetVisible(navigation, true);

            while (landing.alpha > 0)
            {
                landing.alpha = Mathf.MoveTowards(landing.alpha, 0, Time.unscaledDeltaTime * 3.5f);
                yield return null;
            }
            landing.gameObject.SetActive(false);
            launching = false;
        }

        private IEnumerator StartPractice(UnityAction encounter)
        {
            if (launching || fighting)
                yield break;

            launching = true;
            bool fromHome = !VoyageState.Started;
            if (fromHome)
                yield return EnableCamera();
            launching = false;
            if (cameraUnavailable)
                yield break;

            encounter();
        }

        private IEnumerator EnableCamera()
        {
#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                var settings = XRGeneralSettings.Instance;
                var manager = settings != null ? settings.Manager : null;
                if (manager != null && manager.activeLoader == null)
                {
                    yield return manager.InitializeLoader();
                    if (manager.activeLoader != null)
                    {
                        manager.StartSubsystems();
                        Application.quitting += () =>
                        {
                            manager.StopSubsystems();
                            manager.DeinitializeLoader();
                        };
                    }
                }

                cameraUnavailable = manager == null || manager.activeLoader == null;
            }
#endif
            rearCamera.enabled = true;
            session.enabled = !cameraUnavailable;
            if (cameraManager != null)
                cameraManager.enabled = !cameraUnavailable;
            yield break;
        }

        internal void SetEncounter(bool active, bool practiceFight = false, bool fullScreen = false)
        {
            fighting = active;
            if (!VoyageState.Started)
            {
                SetVisible(landing, !active);
                if (!active)
                {
                    session.enabled = false;
                    if (cameraManager != null) cameraManager.enabled = false;
                }
                return;
            }

            Open(Page.None);
            navigation.blocksRaycasts = navigation.interactable = !active;
            healthRoot.gameObject.SetActive(!active || !(practiceFight || fullScreen));
        }

        private void Open(Page next)
        {
            page = next;
            oracle.SetVisible(next == Page.Oracle);
            modal.interactable = next != Page.None;
            if (next == Page.None)
                return;
            modal.blocksRaycasts = true;
            modalPanel.anchorMax = next == Page.Oracle ? Vector2.one : new Vector2(1, 0.60f);
            modalPanel.offsetMax = new Vector2(-14, next == Page.Oracle ? -120 : 0);
            FitModalToKeyboard();
            RefreshPage();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
        }

        private void RefreshPage()
        {
            nextPageRefresh = Time.unscaledTime + 0.25f;
            bool isOracle = page == Page.Oracle;
            bool isJournal = page == Page.Clues;
            heading.text = isOracle ? "The Oracle" : PageNames[(int)page];
            scrollArea.gameObject.SetActive(!isOracle);
            journal.Content.gameObject.SetActive(isJournal);
            content.gameObject.SetActive(!isJournal);
            RectTransform displayContent = isJournal ? journal.Content : content.rectTransform;
            if (scroll.content != displayContent)
            {
                scroll.StopMovement();
                scroll.content = displayContent;
            }

            string label = null;
            pageAction = null;
            if (isJournal)
                journal.Refresh();
            else if (page == Page.Locations)
                content.text = IncludedLocations;
            else if (page == Page.Rules)
            {
                content.text = Rules;
                if (VoyageState.Won || VoyageState.Health == 0)
                {
                    label = "Begin a new voyage";
                    pageAction = () =>
                    {
                        VoyageState.End();
                        OracleChat.ResetConversation();
                        SceneManager.LoadScene(gameObject.scene.buildIndex);
                    };
                }
                else if (!cameraUnavailable)
                {
                    label = "Practice Cyclops";
                    pageAction = () => { Open(Page.None); practice(); };
                }
            }
            action.gameObject.SetActive(pageAction != null);
            scrollArea.offsetMin = new Vector2(17, pageAction != null ? 75 : 18);
            actionLabel.text = label;
        }

        private void FitModalToKeyboard()
        {
            float keyboard = page == Page.Oracle && TouchScreenKeyboard.visible
                ? Mathf.Max(0f, TouchScreenKeyboard.area.height - Screen.safeArea.yMin) / hudCanvas.scaleFactor
                : 0f;
            var target = new Vector2(14, 14 + keyboard);
            if (modalPanel.offsetMin != target)
                modalPanel.offsetMin = target;
        }

        private void ShowClueNotification(string clue)
        {
            clueToastUntil = Time.unscaledTime + 3.5f;
        }

        private void OnDestroy()
        {
            GameEvents.ClueUnlocked -= ShowClueNotification;
        }

        private void FitScreen()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect area = Screen.safeArea;
            if (size == previousScreen && area == previousArea)
                return;
            previousScreen = size;
            previousArea = area;
            OdysseyTheme.FitSafeArea(safeArea);
            OdysseyTheme.FitSafeArea(landingSafeArea);
        }

        private void Update()
        {
            FitScreen();
            if (backgroundMusic != null)
            {
                float musicTarget = VoyageState.Won ? 0f : fighting ? 0.04f : 0.16f;
                backgroundMusic.volume = Mathf.MoveTowards(backgroundMusic.volume, musicTarget, Time.unscaledDeltaTime * 0.4f);
            }
            if (!VoyageState.Started)
                return;

            health.Render(VoyageState.Health, VoyageState.MaxHealth);
            FitModalToKeyboard();
            bool showClue = !fighting && clueToastUntil > Time.unscaledTime;
            if (clueToast.gameObject.activeSelf != showClue)
                clueToast.gameObject.SetActive(showClue);
            string status = "";
#if !UNITY_EDITOR
            if (!fighting && cameraUnavailable)
                status = "AR camera unavailable on this device";
            else if (!fighting && ARSession.state != ARSessionState.SessionTracking)
                status = "Point the camera at a well-lit area";
#endif
            if (showClue) status = "";
            if (status != trackingMessage)
            {
                trackingMessage = status;
                tracking.text = status;
                tracking.transform.parent.gameObject.SetActive(status.Length != 0);
            }
            if (page != Page.None && Time.unscaledTime >= nextPageRefresh)
                RefreshPage();
            float railAlpha = fighting || page != Page.None ? 0 : 1;
            navigation.alpha = Mathf.MoveTowards(navigation.alpha, railAlpha, Time.unscaledDeltaTime * 6f);
            float target = page == Page.None ? 0 : 1;
            modal.alpha = Mathf.MoveTowards(modal.alpha, target, Time.unscaledDeltaTime * 6f);
            if (modal.alpha == 0 && page == Page.None)
                modal.blocksRaycasts = false;
        }

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1 : 0;
            group.blocksRaycasts = group.interactable = visible;
        }
    }
}
