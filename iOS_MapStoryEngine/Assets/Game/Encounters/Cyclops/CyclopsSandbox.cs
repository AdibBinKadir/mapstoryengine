#if UNITY_EDITOR
using System;
using Mapbox.Unity.Location;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Odyssey.Cyclops
{
    public sealed class CyclopsSandbox : MonoBehaviour
    {
        private readonly Vector2d cave = new Vector2d(32.73, -97.11);
        private readonly CyclopsLocation gate = new CyclopsLocation();
        private Text status;
        private Text instructions;
        private Button enter;
        private Canvas panel;
        private double simulatedNow = 100;
        private CyclopsEncounter activeFight;

        private void Start()
        {
            var ui = new GameObject("Sandbox UI", typeof(RectTransform));
            panel = CyclopsView.Overlay(ui);
            Vector2 top = new Vector2(0.5f, 1);
            status = CyclopsView.Label(CyclopsView.Place(ui.transform, "GPS status", top, top,
                new Vector2(0, -24), new Vector2(345, 55)), "Cyclops cave simulation");
            instructions = CyclopsView.Label(CyclopsView.Place(ui.transform, "Controls", top, top,
                new Vector2(0, -92), new Vector2(350, 92)),
                "1: 50 m away  •  2: 15 m away\n3: inaccurate fix  •  4: expire latest fix\nIn combat, hold Shift to shield and click the boss.");
            enter = CyclopsView.Button(CyclopsView.Place(ui.transform, "Enter", top, top,
                new Vector2(0, -193), new Vector2(180, 44)), "Enter Cave", Enter);
            ApplyFix(50, 8);
        }

        private void Update()
        {
            if (activeFight != null)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyFix(50, 8);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyFix(15, 8);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyFix(15, 60);
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                simulatedNow += 21;
                status.text = "GPS fix expired";
            }
            enter.interactable = gate.CanEnter(simulatedNow);
        }

        private void ApplyFix(double meters, float accuracy)
        {
            var fix = new Location
            {
                LatitudeLongitude = new Vector2d(cave.x + meters / 111195, cave.y),
                IsLocationServiceEnabled = true,
                Accuracy = accuracy,
                Timestamp = ++simulatedNow
            };
            gate.Update(fix, cave, simulatedNow);
            status.text = Math.Round(meters) + " m from cave  ·  ±" + accuracy + " m accuracy";
        }

        private void Enter()
        {
            if (!gate.CanEnter(simulatedNow))
                return;

            var prefab = Resources.Load<CyclopsEncounter>("Odyssey/CyclopsEncounter");
            panel.gameObject.SetActive(false);
            activeFight = Instantiate(prefab);
            activeFight.Begin(Camera.main, transform, true, won =>
            {
                activeFight.gameObject.SetActive(false);
                Destroy(activeFight.gameObject);
                activeFight = null;
                panel.gameObject.SetActive(true);
                ApplyFix(50, 8);
            });
        }
    }
}
#endif
