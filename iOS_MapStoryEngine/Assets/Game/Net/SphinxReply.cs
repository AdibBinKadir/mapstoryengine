using System;

// The Sphinx server's reply to /sphinx and /sphinx/talk. Missing fields read as "" / 0 / false.
[Serializable]
public class SphinxReply
{
    public string heard;        // what speech-to-text heard (talk only)
    public string line;         // what she says
    public string riddle;       // a riddle to show, or empty
    public int riddle_index = -1;
    public string verdict;      // "correct" | "wrong" | "none" (talk only)
    public bool reveal;         // true on the second correct answer: the "Not here." reveal
    public string audio;        // base64 WAV of line (+ riddle), or empty when the voice failed
    public bool speechInput;
}
