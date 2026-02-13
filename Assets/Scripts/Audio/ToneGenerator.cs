// ToneGenerator.cs — 程序化生成简单音调AudioClip（无需外部音频文件）
using UnityEngine;

namespace StarPipe.Audio
{
    public static class ToneGenerator
    {
        private const int SAMPLE_RATE = 44100;

        /// <summary>生成正弦波AudioClip</summary>
        public static AudioClip CreateTone(float frequency, float duration, float volume = 0.5f)
        {
            int sampleCount = (int)(SAMPLE_RATE * duration);
            var clip = AudioClip.Create("Tone", sampleCount, 1, SAMPLE_RATE, false);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                // 正弦波 + 快速衰减包络
                float envelope = 1f - (float)i / sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }
            clip.SetData(samples,0);
            return clip;
        }
    }
}
