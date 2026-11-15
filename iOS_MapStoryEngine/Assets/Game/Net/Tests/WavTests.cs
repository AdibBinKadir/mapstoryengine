using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

public class WavTests
{
    static int I32(byte[] b, int at) { return BitConverter.ToInt32(b, at); }
    static short I16(byte[] b, int at) { return BitConverter.ToInt16(b, at); }

    [Test]
    public void EncodeHasRiffHeader()
    {
        byte[] wav = Wav.Encode(new float[] { 0f, 0.5f }, 16000);
        Assert.AreEqual("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.AreEqual("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
        Assert.AreEqual(1, I16(wav, 22));        // channels
        Assert.AreEqual(16000, I32(wav, 24));    // sample rate
        Assert.AreEqual(16, I16(wav, 34));       // bits per sample
        Assert.AreEqual(4, I32(wav, 40));        // data bytes
        Assert.AreEqual(48, wav.Length);
    }

    [Test]
    public void RoundTripKeepsSamples()
    {
        var input = new float[] { 0f, 0.25f, -0.5f, 0.999f, -1f };
        int rate, channels;
        float[] output = Wav.Decode(Wav.Encode(input, 22050), out rate, out channels);
        Assert.AreEqual(22050, rate);
        Assert.AreEqual(1, channels);
        Assert.AreEqual(input.Length, output.Length);
        for (int i = 0; i < input.Length; i++) Assert.AreEqual(input[i], output[i], 1f / 32767f + 1e-6f);
    }

    [Test]
    public void ClipsOutOfRangeSamples()
    {
        byte[] wav = Wav.Encode(new float[] { 2f, -2f }, 16000);
        Assert.AreEqual(32767, I16(wav, 44));
        Assert.AreEqual(-32767, I16(wav, 46));
    }

    [Test]
    public void DecodeRejectsNonWav()
    {
        int rate, channels;
        Assert.Throws<FormatException>(() => Wav.Decode(Encoding.ASCII.GetBytes("not a wav file at all, sorry"), out rate, out channels));
        Assert.Throws<FormatException>(() => Wav.Decode(new byte[4], out rate, out channels));
    }

    [Test]
    public void DecodeSkipsExtraChunks()
    {
        byte[] plain = Wav.Encode(new float[] { 0.5f }, 16000);
        var list = new List<byte>();
        list.AddRange(plain.AsSpanSafe(0, 36));
        list.AddRange(Encoding.ASCII.GetBytes("LIST"));
        list.AddRange(BitConverter.GetBytes(4));
        list.AddRange(new byte[] { 1, 2, 3, 4 });
        list.AddRange(plain.AsSpanSafe(36, plain.Length - 36));
        int rate, channels;
        float[] samples = Wav.Decode(list.ToArray(), out rate, out channels);
        Assert.AreEqual(1, samples.Length);
        Assert.AreEqual(0.5f, samples[0], 1e-4f);
    }
}

static class ByteArrayTestExtensions
{
    public static byte[] AsSpanSafe(this byte[] b, int start, int count)
    {
        var r = new byte[count];
        Array.Copy(b, start, r, 0, count);
        return r;
    }
}
