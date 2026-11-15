using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Odyssey.Cyclops
{
    public sealed class CyclopsEncounter : MonoBehaviour
    {
        [SerializeField] private Transform boss;
        [SerializeField] private Renderer eye;
        [SerializeField] private float spawnDistance = 2.5f;

        private Camera viewCamera;
        private Collider bossCollider;
        private Transform trackingSpace;
        private Action<bool> onExit;
        private string returnLabel;
        private CyclopsFight fight;
        private bool simulated;
        private bool useVoyageHealth;
        private bool paused;
        private bool placed;
        private float filteredShieldPitch;
        private bool shieldRaised;
        private Vector3? fullScale;
        private CyclopsMotion motion;
        private Animator idleAnimator;
        private AudioSource effects;
        private AudioSource hurtVoice;
        private float recoil;
        private float fallProgress;
        private float feedbackAge;
        private Color feedbackColor;
        private MaterialPropertyBlock eyeProperties;
        private RectTransform safePanel;
        private VoyageHealthBar practiceHealth;
        private Text cyclopsHealth;
        private Text instruction;
        private Text shieldStatus;

        private Image bossBar;
        private Button resultButton;
        private CanvasGroup hudVisibility;
        private CyclopsShieldGraphic shield;
        private Text feedback;
        private Image flash;
        private Image timingBar;
        private CyclopsFight.Phase? previousPhase;

        private static AudioClip warningCue;
        private static AudioClip strikeCue;
        private static AudioClip guardCue;
        private static AudioClip counterCue;
        private static AudioClip victoryCue;
        private static readonly Color Amber = OdysseyTheme.Gold;
        private static readonly Color Mint = new Color32(139, 199, 178, 255);
        private static readonly Color Red = OdysseyTheme.Red;

        internal void ShowPreview(Vector3 position, Camera camera, Vector3 up)
        {
            if (!fullScale.HasValue)
            {
                fullScale = boss.localScale;
                boss.localScale = fullScale.Value * 0.32f;
                boss.GetComponent<Collider>().enabled = false;
            }

            boss.position = position;
            Vector3 facing = Vector3.ProjectOnPlane(camera.transform.position - position, up);
            if (facing.sqrMagnitude > 0.01f)
                boss.rotation = Quaternion.LookRotation(facing, up);
            boss.gameObject.SetActive(true);
        }

        internal void Begin(Camera camera, Transform arOrigin, bool simulate, Action<bool> finished,
            string resultExitLabel = "Back to Map", bool useVoyageHealth = false)
        {
            viewCamera = camera;
            bossCollider = boss.GetComponent<Collider>();
            if (fullScale.HasValue)
            {
                boss.localScale = fullScale.Value;
                bossCollider.enabled = true;
            }
            trackingSpace = arOrigin;
            simulated = simulate;
            onExit = finished;
            returnLabel = resultExitLabel;
            this.useVoyageHealth = useVoyageHealth;
            fight = new CyclopsFight(useVoyageHealth);
            eyeProperties = new MaterialPropertyBlock();
            idleAnimator = boss.GetComponentInChildren<Animator>(true);
            boss.gameObject.SetActive(false);
            CreateHud();
            CreateEffects();
        }

        internal static bool FilterShieldPitch(ref float filteredPitch, Vector3 forward, Vector3 up,
            bool raised, float deltaTime)
        {
            float vertical = Vector3.Dot(forward, up);
            float pitch = Mathf.Asin(Mathf.Clamp(vertical, -1f, 1f)) * Mathf.Rad2Deg;
            filteredPitch = Mathf.Lerp(filteredPitch, pitch, 1f - Mathf.Exp(-deltaTime / 0.045f));
            float pitchThreshold = raised ? 10f : 18f;
            return filteredPitch >= pitchThreshold;
        }

        private bool PlaceBoss()
        {
            Vector3 forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, trackingSpace.up);
            if (forward.sqrMagnitude < 0.04f)
                return false;

            forward.Normalize();
            boss.position = viewCamera.transform.position + forward * spawnDistance - trackingSpace.up * 1.3f;
            boss.rotation = Quaternion.LookRotation(-forward, trackingSpace.up);
            boss.gameObject.SetActive(true);
            motion = new CyclopsMotion(boss);
            placed = true;
            return true;
        }

        private void Update()
        {
            if (fight == null)
                return;

            hudVisibility.alpha = Mathf.MoveTowards(hudVisibility.alpha, 1, Time.unscaledDeltaTime * 5f);
            motion?.RestoreIdle();
            CyclopsView.FitSafeArea(safePanel);
            practiceHealth?.Render(fight.PlayerHp, CyclopsFight.PlayerMax);
            bool tracking = !paused && Application.isFocused
                && (simulated || ARSession.state == ARSessionState.SessionTracking);
            if (idleAnimator != null)
                idleAnimator.speed = tracking && !fight.Finished ? 1 : 0;

            if (fight.Finished)
            {
                shield.SetRaised(false, Time.unscaledDeltaTime);
                if (tracking)
                {
                    fallProgress = Mathf.Min(1, fallProgress + Time.deltaTime / 0.95f);
                    AdvanceFeedback(Time.deltaTime);
                }
                return;
            }

            if (!tracking)
            {
                shieldRaised = false;
                filteredShieldPitch = 0f;
                shield.SetRaised(false, Time.unscaledDeltaTime);
                instruction.text = placed ? "AR paused. Hold still while tracking returns." : "Point the rear camera at a well-lit area.";
                shieldStatus.text = "Shield waiting";
                SetEye(Color.black);
                return;
            }

            if (!placed && !PlaceBoss())
            {
                instruction.text = "Hold your phone upright to place the Cyclops.";
                return;
            }

            if (simulated)
                shieldRaised = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            else
                shieldRaised = FilterShieldPitch(ref filteredShieldPitch,
                    viewCamera.transform.forward, trackingSpace.up, shieldRaised,
                    Time.unscaledDeltaTime);

            AdvanceFeedback(Time.deltaTime);
            fight.Tick(Time.deltaTime, shieldRaised);
            PresentPhaseChange();

            if (fight.Current == CyclopsFight.Phase.Recovery && ReadTap(out Vector2 position))
            {
                Ray ray = viewCamera.ScreenPointToRay(position);
                if (bossCollider.Raycast(ray, out _, 12f))
                {
                    int damage = fight.TryHit();
                    if (damage > 0)
                    {
                        recoil = 1;
                        if (hurtVoice != null)
                        {
                            hurtVoice.Stop();
                            hurtVoice.Play();
                        }
                        bool powered = damage > 10;
                        if (fight.Current == CyclopsFight.Phase.Won)
                            ShowFeedback("FINISHING BLOW", Amber, victoryCue);
                        else
                            ShowFeedback(powered ? "POWER COUNTER  −" + damage : "HIT  −" + damage,
                                powered ? Amber : Mint, hurtVoice == null ? counterCue : null);
                    }
                }
            }

            UpdateHud(shieldRaised);
            shield.SetRaised(shieldRaised && !fight.Finished, Time.unscaledDeltaTime);
            if (fight.Finished)
            {
                resultButton.gameObject.SetActive(true);
                resultButton.GetComponentInChildren<Text>().text = returnLabel;
            }
        }

        private void LateUpdate()
        {
            motion?.Render(fight, recoil, fallProgress);
        }

        private void PresentPhaseChange()
        {
            if (previousPhase == fight.Current)
                return;

            previousPhase = fight.Current;
            if (fight.Current == CyclopsFight.Phase.Warning)
            {
                effects.PlayOneShot(warningCue, 0.50f);
                return;
            }

            if (fight.Current != CyclopsFight.Phase.Recovery && fight.Current != CyclopsFight.Phase.Lost)
                return;

            switch (fight.LastImpact)
            {
                case CyclopsFight.Impact.PerfectBlock:
                    ShowFeedback("PERFECT BLOCK", Amber, guardCue);
                    recoil = 0.65f;
                    break;
                case CyclopsFight.Impact.Block:
                    ShowFeedback("BLOCKED", Mint, guardCue);
                    recoil = 0.35f;
                    break;
                case CyclopsFight.Impact.Hit:
                    ShowFeedback("−25 HP", Red, strikeCue);
                    break;
            }
        }

        private void UpdateHud(bool raised)
        {
            cyclopsHealth.text = "CYCLOPS  " + fight.BossHp;
            CyclopsView.SetBar(bossBar, (float)fight.BossHp / CyclopsFight.BossMax);
            shieldStatus.text = fight.Finished ? "" : raised ? "Shield up" : "Shield down";
            shieldStatus.color = raised ? Mint : OdysseyTheme.Ivory;

            bool warning = fight.Current == CyclopsFight.Phase.Warning;
            bool recovery = fight.Current == CyclopsFight.Phase.Recovery;
            timingBar.color = warning ? Amber : Mint;
            CyclopsView.SetBar(timingBar, warning || recovery ? 1 - fight.Progress : 0);

            Color glow = Color.black;
            switch (fight.Current)
            {
                case CyclopsFight.Phase.Warning:
                    string attack = fight.CurrentAttack == CyclopsFight.Attack.Slam ? "Heavy slam" : "Quick swipe";
                    instruction.text = attack + (raised ? "  •  Hold shield" : fight.PerfectWindow ? "  •  Tilt up now!" : "  •  Ready your shield");
                    glow = fight.PerfectWindow ? new Color(5f, 2.1f, 0.45f)
                        : new Color(3f, 0.36f, 0.07f) * (0.65f + 0.35f * Mathf.Sin(fight.Progress * 18));
                    break;
                case CyclopsFight.Phase.Recovery:
                    instruction.text = fight.CountersLeft == 0
                        ? "The Cyclops is recovering"
                        : "Exposed! Tap to counter  •  " + fight.CountersLeft + " left";
                    glow = new Color(0.07f, 0.55f, 0.36f);
                    break;
                case CyclopsFight.Phase.Won:
                    instruction.text = useVoyageHealth ? "Cyclops defeated. A clue awaits." : "Cyclops defeated";
                    break;
                case CyclopsFight.Phase.Lost:
                    instruction.text = "The Cyclops defeated you";
                    break;
                default:
                    instruction.text = "Time your shield for the gold flash.";
                    break;
            }

            SetEye(glow);
        }

        private void SetEye(Color glow)
        {
            if (eye == null)
                return;

            eye.GetPropertyBlock(eyeProperties);
            eyeProperties.SetColor("_EmissionColor", glow);
            eye.SetPropertyBlock(eyeProperties);
        }

        private void ShowFeedback(string message, Color color, AudioClip sound)
        {
            feedback.text = message;
            feedbackColor = color;
            feedbackAge = 0;
            feedback.gameObject.SetActive(true);
            AdvanceFeedback(0);
            if (sound != null)
                effects.PlayOneShot(sound, 0.65f);
        }

        private void AdvanceFeedback(float seconds)
        {
            recoil = Mathf.Max(0, recoil - seconds * 4);
            if (!feedback.gameObject.activeSelf)
                return;

            feedbackAge += seconds;
            if (feedbackAge >= 0.68f)
            {
                feedback.gameObject.SetActive(false);
                flash.color = Color.clear;
                return;
            }

            float opacity = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.23f, 0.68f, feedbackAge));
            feedback.color = new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b, opacity);
            feedback.rectTransform.anchoredPosition = new Vector2(0, Mathf.Min(feedbackAge * 36, 20));
            feedback.rectTransform.localScale = Vector3.one * (1 + Mathf.Max(0, 1 - feedbackAge * 7) * 0.16f);
            flash.color = new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b,
                Mathf.Max(0, 0.18f * (1 - feedbackAge * 6)));
        }

        private void CreateEffects()
        {
            effects = OdysseyEncounterAudio.CreateSource(gameObject);
            var hurtClip = Resources.Load<AudioClip>("Odyssey/CyclopsHurt");
            if (hurtClip != null)
            {
                hurtVoice = OdysseyEncounterAudio.CreateSource(gameObject);
                hurtVoice.clip = hurtClip;
                hurtVoice.volume = 0.58f;
            }
            if (warningCue == null)
            {
                warningCue = OdysseyEncounterAudio.CyclopsWarning;
                strikeCue = MakeCue("Cyclops impact", 120, 36, 0.28f);
                guardCue = Resources.Load<AudioClip>("Odyssey/ShieldBlock")
                    ?? MakeCue("Cyclops guard", 520, 940, 0.22f);
                counterCue = MakeCue("Cyclops counter", 270, 150, 0.14f);
                victoryCue = OdysseyEncounterAudio.Victory;
            }
        }

        private static AudioClip MakeCue(string name, float startHz, float endHz, float seconds)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.RoundToInt(seconds * sampleRate)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / (samples.Length - 1);
                float envelope = Mathf.Min(1, t * 30) * (1 - t) * (1 - t);
                phase += 2 * Mathf.PI * Mathf.Lerp(startHz, endHz, t) / sampleRate;
                samples[i] = (Mathf.Sin(phase) + 0.23f * Mathf.Sin(phase * 2)) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static bool ReadTap(out Vector2 position)
        {
            position = Vector2.zero;
            EventSystem events = EventSystem.current;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (Input.touchCount != 1 || touch.phase != TouchPhase.Ended
                    || (events != null && events.IsPointerOverGameObject(touch.fingerId)))
                    return false;

                position = touch.position;
                return true;
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonUp(0) && (events == null || !events.IsPointerOverGameObject()))
            {
                position = Input.mousePosition;
                return true;
            }
#endif
            return false;
        }

        private void CreateHud()
        {
            var hud = new GameObject("Cyclops HUD", typeof(RectTransform));
            hud.transform.SetParent(transform, false);
            CyclopsView.Overlay(hud);
            hudVisibility = hud.AddComponent<CanvasGroup>();
            hudVisibility.alpha = 0;
            Vector2 top = new Vector2(0.5f, 1);
            Vector2 bottom = new Vector2(0.5f, 0);
            Vector2 center = new Vector2(0.5f, 0.5f);
            RectTransform flashRect = CyclopsView.Place(hud.transform, "Impact tint", Vector2.zero,
                Vector2.zero, Vector2.zero, Vector2.zero);
            flashRect.anchorMax = Vector2.one;
            Vector2 shieldPivot = new Vector2(0.5f, 1f);
            var shieldRect = CyclopsView.Place(hud.transform, "Raised aspis", bottom, shieldPivot,
                Vector2.zero, Vector2.zero);
            shield = shieldRect.gameObject.AddComponent<CyclopsShieldGraphic>();
            shield.Initialize((RectTransform)hud.transform);

            // Keep the impact flash above the shield and the readable HUD above both.
            flashRect.SetAsLastSibling();
            flash = CyclopsView.Background(flashRect, Color.clear);
            safePanel = CyclopsView.Place(hud.transform, "Safe area", top, top, Vector2.zero, Vector2.zero);
            CyclopsView.FitSafeArea(safePanel);

            if (!useVoyageHealth)
                practiceHealth = new VoyageHealthBar(safePanel);
            var header = CyclopsView.Place(safePanel, "Combat HUD", top, top, new Vector2(0, -76), new Vector2(0, 116));
            header.anchorMin = new Vector2(0.04f, 1);
            header.anchorMax = new Vector2(0.96f, 1);
            CyclopsView.Background(header, OdysseyTheme.Ink);
            OdysseyTheme.Frame(header, OdysseyTheme.Bronze, 0);
            bossBar = HealthDisplay(header, "Cyclops", "CYCLOPS  60", 0.05f, 0.95f,
                OdysseyTheme.Red, out cyclopsHealth);

            instruction = CyclopsView.Label(StretchRow(header, "Instruction", -48, 24),
                "Find a clear space to fight.", 16);
            instruction.resizeTextForBestFit = true;
            instruction.resizeTextMinSize = 12;
            instruction.resizeTextMaxSize = 16;
            shieldStatus = CyclopsView.Label(StretchRow(header, "Shield", -74, 19), "Shield waiting", 13);
            timingBar = CyclopsView.Bar(StretchRow(header, "Timing", -101, 4), Amber);
            CyclopsView.SetBar(timingBar, 0);

            Vector2 feedbackAnchor = new Vector2(0.5f, 0.64f);
            feedback = CyclopsView.Label(CyclopsView.Place(safePanel, "Fight feedback", feedbackAnchor, center,
                Vector2.zero, new Vector2(310, 52)), "", 25);
            var shadow = feedback.gameObject.AddComponent<UnityEngine.UI.Outline>();
            shadow.effectColor = new Color(0.05f, 0.06f, 0.08f, 0.9f);
            shadow.effectDistance = new Vector2(1.7f, -1.7f);
            feedback.gameObject.SetActive(false);

            resultButton = CyclopsView.Button(CyclopsView.Place(safePanel, "Continue after fight", bottom, bottom,
                new Vector2(0, 20), new Vector2(178, 48)), useVoyageHealth ? returnLabel : "Leave practice", ContinueAfterFight);
            resultButton.gameObject.SetActive(!useVoyageHealth);
        }

        private static Image HealthDisplay(RectTransform header, string name, string text,
            float left, float right, Color color, out Text label)
        {
            Vector2 top = new Vector2(0.5f, 1);
            var column = CyclopsView.Place(header, name, top, top, new Vector2(0, -9), new Vector2(0, 37));
            column.anchorMin = new Vector2(left, 1);
            column.anchorMax = new Vector2(right, 1);
            label = CyclopsView.Label(StretchRow(column, "Hit points", 0, 20), text, 14);
            label.color = OdysseyTheme.Ivory;
            return CyclopsView.Bar(StretchRow(column, "Health bar", -24, 5), color);
        }

        private static RectTransform StretchRow(RectTransform parent, string name, float y, float height)
        {
            Vector2 top = new Vector2(0.5f, 1);
            var row = CyclopsView.Place(parent, name, top, top, new Vector2(0, y), new Vector2(-16, height));
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            return row;
        }

        private void ContinueAfterFight()
        {
            if (fight == null || (useVoyageHealth && !fight.Finished))
                return;

            resultButton.interactable = false;
            onExit?.Invoke(fight.Current == CyclopsFight.Phase.Won);
        }

        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
            if (isPaused && shield != null)
                shield.HideImmediately();
        }
    }
}
