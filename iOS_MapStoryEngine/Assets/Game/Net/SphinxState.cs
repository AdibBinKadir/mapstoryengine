using System;

// What the phone tells the Sphinx server about the player (sent as JSON; field names are the wire format).
[Serializable]
public class SphinxState
{
    public string @event = "intro";   // "intro" | "taunt"
    public int hp = 100;
    public int max_hp = 100;
    public int deaths;
    public string last_event = "";
    public int riddle_index = -1;     // the current riddle on the server; -1 before the intro
    public int correct_count;         // correct answers so far; the second one is the reveal
}
