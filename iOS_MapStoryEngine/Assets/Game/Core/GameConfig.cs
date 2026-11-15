using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Odyssey/Game Config")]
public class GameConfig : ScriptableObject
{
    public string serverUrl;

    public LatLon troy;
    public LatLon ithaca;
    public LatLon sphinx;
    public LatLon medusa;
    public LatLon cyclops;
    public LatLon[] hydraZones = Array.Empty<LatLon>();
    public LatLon[] respawnPoints = Array.Empty<LatLon>();

    public int maxHp = 100;
    public float winRadiusM = 20f;
    public float respawnRadiusM = 20f;
    public float hydraRadiusM = 30f;
    public float encounterRadiusM = 25f;
    public float vagueClueIntervalSec = 300f;
    public bool demoMode = true;

    public GameObject sphinxPrefab;
    public GameObject medusaPrefab;
    public GameObject cyclopsPrefab;
    public GameObject hydraPrefab;
}
