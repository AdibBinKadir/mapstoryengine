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
        [SerializeField] private float shieldHeight = 0.15f;
        [SerializeField] private float spawnDistance = 2.5f;

        private Camera viewCamera;
        private Collider bossCollider;
        private Transform trackingSpace;
        private Action<bool> onExit;
        private string returnLabel;
        private CyclopsFight fight;
        private bool simulated;
        private bool paused;
        private bool placed;
        private float initialHeight;
        private Quaternion standing;
        private MaterialPropertyBlock eyeProperties;
        private RectTransform safePanel;
        private Text health;
        private Text instruction;
        private Text shieldStatus;
        private Image playerBar;
        private Image bossBar;
        private Button exit;
        private CyclopsFight.Phase? previousPhase;

        internal void Begin(Camera camera, Transform arOrigin, bool simulate, Action<bool> finished,
            string resultExitLabel = "Back to Map")
        {
            viewCamera = camera;
            bossCollider = boss.GetComponent<Collider>();
            trackingSpace = arOrigin;
            simulated = simulate;
            onExit = finished;
            returnLabel = resultExitLabel;
            fight = new CyclopsFight();
            eyeProperties = new MaterialPropertyBlock();
            boss.gameObject.SetActive(false);
            CreateHud();
        }

        private float Height()
        {
            return trackingSpace.InverseTransformPoint(viewCamera.transform.position).y;
        }

        private bool PlaceBoss()
        {
            Vector3 forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, trackingSpace.up);
            if (forward.sqrMagnitude < 0.04f)
                return false;

            forward.Normalize();
            initialHeight = Height();
            boss.position = viewCamera.transform.position + forward * spawnDistance - trackingSpace.up * 1.3f;
            standing = Quaternion.LookRotation(-forward, trackingSpace.up);
            boss.rotation = standing;
            boss.gameObject.SetActive(true);
            placed = true;
            return true;
        }

        private void Update()
        {
            if (fight == null)
                return;

            CyclopsView.FitSafeArea(safePanel);
            if (fight.Finished)
            {
                if (fight.Current == CyclopsFight.Phase.Won)
                {
                    Quaternion fallen = standing * Quaternion.Euler(-86, 0, 0);
                    boss.rotation = Quaternion.RotateTowards(boss.rotation, fallen, Time.deltaTime * 140);
                }
                return;
            }

            if (paused || !Application.isFocused || (!simulated && ARSession.state != ARSessionState.SessionTracking))
            {
                instruction.text = placed ? "AR paused. Hold still while tracking returns." : "Point the rear camera at a well-lit area.";
                shieldStatus.text = "Shield waiting";
                return;
            }

            if (!placed && !PlaceBoss())
            {
                instruction.text = "Hold your phone upright to place the Cyclops.";
                return;
            }

            bool raised = simulated ? Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                : Height() - initialHeight >= shieldHeight;
            fight.Tick(Time.deltaTime, raised);
            if (fight.Current == CyclopsFight.Phase.Recovery && ReadTap(out Vector2 position))
            {
                Ray ray = viewCamera.ScreenPointToRay(position);
                if (bossCollider.Raycast(ray, out _, 12f))
                    fight.TryHit();
            }

            UpdateHud(raised);
        }

        private void UpdateHud(bool raised)
        {
            health.text = "YOU  " + fight.PlayerHp + "     CYCLOPS  " + fight.BossHp;
            CyclopsView.SetBar(playerBar, (float)fight.PlayerHp / CyclopsFight.PlayerMax);
            CyclopsView.SetBar(bossBar, (float)fight.BossHp / CyclopsFight.BossMax);
            shieldStatus.text = fight.Finished ? "" : raised ? "Shield up" : "Shield down";
            shieldStatus.color = raised ? new Color(0.44f, 0.95f, 0.72f) : Color.white;

            switch (fight.Current)
            {
                case CyclopsFight.Phase.Warning:
                    instruction.text = "Raise your phone to block!";
                    break;
                case CyclopsFight.Phase.Recovery:
                    instruction.text = fight.LastHitBlocked ? "Blocked! Tap the Cyclops." : "You took a hit. Tap the Cyclops.";
                    break;
                case CyclopsFight.Phase.Won:
                    instruction.text = "Cyclops defeated";
                    break;
                case CyclopsFight.Phase.Lost:
                    instruction.text = "The Cyclops defeated you";
                    break;
                default:
                    instruction.text = "Watch the Cyclops’s eye.";
                    break;
            }

            if (previousPhase == fight.Current)
                return;

            if (eye != null)
            {
                eye.GetPropertyBlock(eyeProperties);
                eyeProperties.SetColor("_EmissionColor", fight.Current == CyclopsFight.Phase.Warning
                    ? new Color(3f, 0.36f, 0.07f) : Color.black);
                eye.SetPropertyBlock(eyeProperties);
            }
            if (fight.Finished)
                exit.GetComponentInChildren<Text>().text = returnLabel;

            previousPhase = fight.Current;
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
            Vector2 top = new Vector2(0.5f, 1);
            Vector2 bottom = new Vector2(0.5f, 0);
            safePanel = CyclopsView.Place(hud.transform, "Safe area", top, top, Vector2.zero, Vector2.zero);

            var header = CyclopsView.Place(safePanel, "Health", top, top, new Vector2(0, -12), new Vector2(344, 65));
            CyclopsView.Background(header, new Color(0.07f, 0.08f, 0.11f, 0.93f));
            health = CyclopsView.Label(CyclopsView.Place(header, "Hit points", top, top,
                new Vector2(0, -5), new Vector2(344, 25)), "YOU  100     CYCLOPS  60");
            playerBar = CyclopsView.Bar(CyclopsView.Place(header, "Player", top, top,
                new Vector2(-77, -36), new Vector2(135, 9)), new Color(0.37f, 0.83f, 0.62f));
            bossBar = CyclopsView.Bar(CyclopsView.Place(header, "Boss", top, top,
                new Vector2(77, -36), new Vector2(135, 9)), new Color(0.99f, 0.32f, 0.24f));

            var footer = CyclopsView.Place(safePanel, "Fight controls", bottom, bottom,
                new Vector2(0, 14), new Vector2(344, 135));
            CyclopsView.Background(footer, new Color(0.07f, 0.08f, 0.11f, 0.93f));
            instruction = CyclopsView.Label(CyclopsView.Place(footer, "Instruction", top, top,
                new Vector2(0, -9), new Vector2(322, 39)), "Find a clear space to fight.", 17);
            shieldStatus = CyclopsView.Label(CyclopsView.Place(footer, "Shield", top, top,
                new Vector2(0, -48), new Vector2(322, 22)), "Shield waiting");
            exit = CyclopsView.Button(CyclopsView.Place(footer, "Exit", bottom, bottom,
                new Vector2(0, 12), new Vector2(158, 44)), "Leave Cave",
                () => onExit?.Invoke(fight.Current == CyclopsFight.Phase.Won));
        }

        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
        }
    }
}
