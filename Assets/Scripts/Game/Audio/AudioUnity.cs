using System;
using UnityEngine;

public class AudioUnity : MonoBehaviour
{
	public float gain = 0.05f;
	public float gainThreshold = 0.15f;

	[Header("Info")]
	public int sampleRate;

	public int numChannels;
	public int bufferLength;
	public int numBuffers;
	public int batchesPerSecond;
	public long numTicks;
	public double audioTime;

	double samplesPerMillisecond;

	public AudioState audioState;


	void Awake()
	{
		sampleRate = AudioSettings.outputSampleRate;

		AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
		batchesPerSecond = sampleRate / bufferLength;
		numChannels = (AudioSettings.speakerMode == AudioSpeakerMode.Stereo) ? 2 : 1;

		samplesPerMillisecond = sampleRate / 1000;
	}
	void OnAudioFilterRead(float[] data, int numChannels)
	{
		for (int i = 0; i < data.Length; i += numChannels)
		{
			float sample = gain * audioState.Sample(Time);
			//float sample = (float)gain * MathF.Sin((float)Time * MathF.PI * 300);
			if (MathF.Abs(sample) > gainThreshold) sample = gainThreshold * Mathf.Sign(sample);

			data[i] = sample;

			// Copy data to second channel (if stereo)
			if (numChannels == 2) data[i + 1] = sample;

			numTicks++;
		}

		audioTime = ((int)(Time * 1000)) / 1000.0;
	}

	public double Time
	{
		get
		{
			return numTicks / (double)sampleRate;
		}
	}
}