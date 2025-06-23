using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] fileBytes, string clipName = "wav", bool stream = false)
    {
        using (MemoryStream streamData = new MemoryStream(fileBytes))
        using (BinaryReader reader = new BinaryReader(streamData))
        {
            // Read WAV header
            string riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
            {
                Debug.LogError("Invalid WAV file: Missing 'RIFF' header");
                return null;
            }

            reader.ReadInt32(); // Chunk size
            string wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
            {
                Debug.LogError("Invalid WAV file: Missing 'WAVE' header");
                return null;
            }

            // Read fmt chunk
            string fmt = new string(reader.ReadChars(4));
            if (fmt != "fmt ")
            {
                Debug.LogError("Invalid WAV file: Missing 'fmt ' chunk");
                return null;
            }

            int subChunk1Size = reader.ReadInt32();
            ushort audioFormat = reader.ReadUInt16();
            ushort numChannels = reader.ReadUInt16();
            int sampleRate = reader.ReadInt32();
            int byteRate = reader.ReadInt32();
            ushort blockAlign = reader.ReadUInt16();
            ushort bitsPerSample = reader.ReadUInt16();

            if (audioFormat != 1)
            {
                Debug.LogError("WAV file is compressed; only PCM supported.");
                return null;
            }

            // Skip any extra bytes in subchunk1
            if (subChunk1Size > 16)
                reader.ReadBytes(subChunk1Size - 16);

            // Read data chunk header
            string dataID = new string(reader.ReadChars(4));
            while (dataID != "data")
            {
                int chunkSize = reader.ReadInt32();
                reader.ReadBytes(chunkSize);
                dataID = new string(reader.ReadChars(4));
            }

            int dataSize = reader.ReadInt32();
            byte[] data = reader.ReadBytes(dataSize);

            int sampleCount = dataSize / (bitsPerSample / 8);
            float[] audioData = new float[sampleCount];

            int offset = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                if (bitsPerSample == 16)
                {
                    short sample = BitConverter.ToInt16(data, offset);
                    audioData[i] = sample / 32768.0f;
                    offset += 2;
                }
                else if (bitsPerSample == 8)
                {
                    byte sample = data[offset];
                    audioData[i] = (sample - 128) / 128.0f;
                    offset += 1;
                }
                else
                {
                    Debug.LogError("Only 8-bit and 16-bit WAV formats are supported.");
                    return null;
                }
            }

            AudioClip audioClip = AudioClip.Create(clipName, sampleCount / numChannels, numChannels, sampleRate, stream);
            audioClip.SetData(audioData, 0);
            return audioClip;
        }
    }
    public static byte[] FromAudioClip(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();

        int sampleCount = clip.samples * clip.channels;
        int sampleRate = clip.frequency;
        int channels = clip.channels;

        // Convert float samples to 16-bit PCM
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);
        short[] intData = new short[samples.Length];

        byte[] bytesData = new byte[samples.Length * 2];
        const float rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        // WAV header
        stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(36 + bytesData.Length), 0, 4);
        stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); // Audio format (PCM)
        stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
        stream.Write(BitConverter.GetBytes(sampleRate * channels * 2), 0, 4); // Byte rate
        stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2); // Block align
        stream.Write(BitConverter.GetBytes((ushort)16), 0, 2); // Bits per sample
        stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(bytesData.Length), 0, 4);
        stream.Write(bytesData, 0, bytesData.Length);

        return stream.ToArray();
    }

}
