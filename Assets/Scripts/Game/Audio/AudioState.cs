using System;
using DLS.Game;
using DLS.Simulation;
using UnityEngine;

public class AudioState
{
	public enum WaveType {Sin, Square, Saw}

	const WaveType waveType = WaveType.Square;
	const int waveIterations = 20;
	
	public readonly SimAudio simAudio = new();

	public float Sample(double time)
	{
		float sum = 0;

		for (int i = 0; i < simAudio.freqsAll.Length; i++)
		{
			float amplitude = (float)simAudio.targetAmplitudesPerFreq[i];
			if (amplitude < 0.001f) continue;

			double phase = time * 2 * Math.PI * simAudio.freqsAll[i];
			sum += Wave(phase) * amplitude;
		}

		/*
		if (UnityMain.instance.useRef)
		{
			sum += Wave(time * 2 * Math.PI * UnityMain.instance.refNoteFreq) * 1;
		}
		else sum += Wave(time * 2 * Math.PI * UnityMain.instance.noteFreq) * UnityMain.instance.perceptualGain;
		*/
		
		return sum;
	}

	static float Wave(double phase)
	 {
		 return waveType switch
		 {
			 WaveType.Sin => SinWave(phase),
			 WaveType.Square => SquareWave(phase, waveIterations),
			 WaveType.Saw => SawtoothWave(phase, waveIterations),
			 _ => throw new NotImplementedException()
		 };
	}
	
	static float SinWave(double phase)
	{
		return (float)Math.Sin(phase);
	}
	
	static float SawtoothWave(double t, int numIterations = 20)
	{
		double sum = 0;
		for (int i = 1; i <= numIterations; i++)
		{
			double numerator = Math.Sin(2 * i * t);
			double denominator = i;
			sum += numerator / denominator;
		}
        
		return (float)(sum * 4 / MathF.PI);
	}

	static float SquareWave(double t, int numIterations = 20)
	{
		double sum = 0;
		for (int i = 1; i <= numIterations; i++)
		{
			double numerator = Math.Sin((2 * i - 1) * t);
			double denominator = 2 * i - 1;
			sum += numerator / denominator;
		}

		return (float)(sum * 4 / MathF.PI);
	}
}