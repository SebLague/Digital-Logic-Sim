using System;
using UnityEngine;

public class AudioUnity : MonoBehaviour
{
	public float gain = 0.05f;
	public float clipThreshold = 0.1f;

	[Header("Info")]
	public int sampleRate;
	public int numChannels;
	public int bufferLength;
	public int numBuffers;
	public int batchesPerSecond;
    [Space()]
    public float maxRawSampleLastBatch;
    public float maxProcessedSampleLastBatch;
	public double audioTime;

	public AudioState audioState;

	void Awake()
	{
		sampleRate = AudioSettings.outputSampleRate;
		AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
		batchesPerSecond = sampleRate / bufferLength;
		numChannels = (AudioSettings.speakerMode == AudioSpeakerMode.Stereo) ? 2 : 1;

	}
    
	void OnAudioFilterRead(float[] data, int numChannels)
	{
        double audioDeltaTime = 1.0 / sampleRate;
        maxRawSampleLastBatch = 0;
        maxProcessedSampleLastBatch = 0;
        
        
		for (int i = 0; i < data.Length; i += numChannels)
		{
			float sampleRaw = gain * audioState.Sample(audioTime);
            float amplitude = MathF.Abs(sampleRaw);
            
            // Clip
            float sample = sampleRaw;
            if (amplitude > clipThreshold) sample = clipThreshold * MathF.Sign(sampleRaw);
			
            maxRawSampleLastBatch = MathF.Max(maxRawSampleLastBatch, amplitude);
            maxProcessedSampleLastBatch = MathF.Max(maxProcessedSampleLastBatch, MathF.Abs(sample));

			data[i] = sample;

			// Copy data to second channel (if stereo)
			if (numChannels == 2) data[i + 1] = sample;

			audioTime += audioDeltaTime;
		}
	}

}