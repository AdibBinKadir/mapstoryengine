using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Press-and-hold for the Speak button: Down when the finger lands, Up when it lifts or slides off.
public class SphinxHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Action Down;
    public Action Up;
    public bool Interactable = true;
    bool held;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable || held) return;
        held = true;
        if (Down != null) Down();
    }

    public void OnPointerUp(PointerEventData eventData) { Release(); }
    public void OnPointerExit(PointerEventData eventData) { Release(); }
    void OnDisable() { Release(); }

    void Release()
    {
        if (!held) return;
        held = false;
        if (Up != null) Up();
    }
}
