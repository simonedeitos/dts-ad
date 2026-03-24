using System;
using NAudio.Wave;

namespace AirDirector.Forms
{
    public class SimpleLevelMeter : ISampleProvider
    {
        private readonly ISampleProvider _source;
        public event EventHandler<float[]> LevelMeterUpdated;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public SimpleLevelMeter(ISampleProvider source)
        {
            _source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            if (samplesRead > 0)
            {
                float[] levels = CalculateLevels(buffer, offset, samplesRead);
                LevelMeterUpdated?.Invoke(this, levels);
            }

            return samplesRead;
        }

        private float[] CalculateLevels(float[] buffer, int offset, int count)
        {
            float leftLevel = 0;
            float rightLevel = 0;
            int channels = WaveFormat.Channels;

            for (int i = offset; i < offset + count; i += channels)
            {
                if (i < buffer.Length)
                {
                    leftLevel = Math.Max(leftLevel, Math.Abs(buffer[i]));
                }

                if (channels > 1 && i + 1 < buffer.Length)
                {
                    rightLevel = Math.Max(rightLevel, Math.Abs(buffer[i + 1]));
                }
            }

            return new float[] { leftLevel, channels > 1 ? rightLevel : leftLevel };
        }
    }
}