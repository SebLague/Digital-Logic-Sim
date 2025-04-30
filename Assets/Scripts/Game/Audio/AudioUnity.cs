using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Math;

//[RequireComponent(typeof(AudioSource))]
public class AudioUnity : MonoBehaviour
{
	public double gain = 0.2;
	public float[] data { get; private set; }

	[Header("Info")]
	public int sampleRate;
	public int numChannels;
	public int bufferLength;
	public int numBuffers;
	public int batchesPerSecond;
	public long numTicks;
	public double audioTime;

	double samplesPerMillisecond;

	System.Func<double, double> sampler;
	bool hasSampler;
	public event System.Action OnRead;

	public void SetSampler(System.Func<double, double> sampler)
	{
		this.sampler = sampler;
		hasSampler = true;
	}

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
			if (hasSampler)
			{
				data[i] = (float)(gain * sampler(Time));
			}
			else
			{
				data[i] = 0;
			}

			// Copy data to second channel (if stereo)
			if (numChannels == 2)
			{
				data[i + 1] = data[i];
			}

			numTicks++;
		}

		this.data = data;
		audioTime = ((int)(Time * 1000)) / 1000.0;
		OnRead?.Invoke();
	}

	public double Time
	{
		get
		{
			return numTicks / (double)sampleRate;
		}
	}


}
