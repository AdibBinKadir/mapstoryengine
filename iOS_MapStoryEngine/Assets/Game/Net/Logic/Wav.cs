using System;
using System.Text;

// 16-bit PCM WAV: the mic recording sent to the server, and the Sphinx's voice coming back. No UnityEngine.
public static class Wav
{
    public static byte[] Encode(float[] samples, int rate)
    {
        int dataBytes = samples.Length * 2;
        var wav = new byte[44 + dataBytes];
        WriteAscii(wav, 0, "RIFF");
        WriteInt(wav, 4, 36 + dataBytes);
        WriteAscii(wav, 8, "WAVE");
        WriteAscii(wav, 12, "fmt ");
        WriteInt(wav, 16, 16);              // fmt chunk size
        WriteShort(wav, 20, 1);             // PCM
        WriteShort(wav, 22, 1);             // mono
        WriteInt(wav, 24, rate);
        WriteInt(wav, 28, rate * 2);        // byte rate
        WriteShort(wav, 32, 2);             // block align
        WriteShort(wav, 34, 16);            // bits per sample
        WriteAscii(wav, 36, "data");
        WriteInt(wav, 40, dataBytes);
        for (int i = 0; i < samples.Length; i++)
        {
            float s = samples[i] > 1f ? 1f : samples[i] < -1f ? -1f : samples[i];
            WriteShort(wav, 44 + i * 2, (short)Math.Round(s * 32767f));
        }
        return wav;
    }

    // Returns interleaved samples in -1..1. Only 16-bit PCM; throws FormatException otherwise.
    public static float[] Decode(byte[] wav, out int rate, out int channels)
    {
        if (wav == null || wav.Length < 12 || Ascii(wav, 0) != "RIFF" || Ascii(wav, 8) != "WAVE")
            throw new FormatException("not a WAV file");

        rate = 0;
        channels = 0;
        int bits = 0;
        int pos = 12;
        while (pos + 8 <= wav.Length)
        {
            string id = Ascii(wav, pos);
            int size = BitConverter.ToInt32(wav, pos + 4);
            int body = pos + 8;
            if (size < 0 || body + size > wav.Length) size = wav.Length - body;   // tolerate streamed sizes
            if (id == "fmt ")
            {
                if (size < 16 || BitConverter.ToInt16(wav, body) != 1) throw new FormatException("WAV is not PCM");
                channels = BitConverter.ToInt16(wav, body + 2);
                rate = BitConverter.ToInt32(wav, body + 4);
                bits = BitConverter.ToInt16(wav, body + 14);
            }
            else if (id == "data")
            {
                if (bits != 16 || channels < 1) throw new FormatException("WAV must be 16-bit PCM");
                var samples = new float[size / 2];
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = BitConverter.ToInt16(wav, body + i * 2) / 32767f;
                return samples;
            }
            pos = body + size + (size & 1);   // chunks are word-aligned
        }
        throw new FormatException("WAV has no data chunk");
    }

    static string Ascii(byte[] b, int at) { return Encoding.ASCII.GetString(b, at, 4); }
    static void WriteAscii(byte[] b, int at, string s) { Encoding.ASCII.GetBytes(s, 0, 4, b, at); }
    static void WriteInt(byte[] b, int at, int v) { Array.Copy(BitConverter.GetBytes(v), 0, b, at, 4); }
    static void WriteShort(byte[] b, int at, short v) { Array.Copy(BitConverter.GetBytes(v), 0, b, at, 2); }
}
