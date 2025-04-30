using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioState
{
	const int freqCount = 28; // 16 naturals (4bit input) + their sharps
	const int C3_StartIndex = 27; // relative to A0

	readonly int[] naturalToAllMap = new int[16];
	readonly float[] freqsAll;

	readonly float[] targetAmplitudesPerFreq_temp = new float[freqCount];
	readonly float[] targetAmplitudesPerFreq = new float[freqCount];

	public AudioState()
	{
		freqsAll = new float[freqCount];

		for (int i = 0; i < freqsAll.Length; i++)
		{
			freqsAll[i] = CalculateFrequency(C3_StartIndex + i);
		}

		int naturalCount = 0;

		for (int i = 0; i < freqsAll.Length; i++)
		{
			bool isNatural = (i % 12) is 0 or 2 or 4 or 5 or 7 or 9 or 11;
			if (isNatural)
			{
				naturalToAllMap[naturalCount] = i;
				naturalCount++;
			}
		}
	}

	public void InitFrame()
	{
		for (int i = 0; i < targetAmplitudesPerFreq_temp.Length; i++)
		{
			targetAmplitudesPerFreq_temp[i] = 0;
		}
	}

	public void NotifyAllNotesRegistered()
	{
		for (int i = 0; i < targetAmplitudesPerFreq.Length; i++)
		{
			targetAmplitudesPerFreq[i] = targetAmplitudesPerFreq_temp[i];
		}
	}

	public void RegisterNote(int naturalIndex, bool isSharp, uint volume = 1)
	{
		int freqIndex = naturalIndex + (isSharp ? 1 : 0);
		float amplitudeT = MathF.Min(volume / 15f, 1);
		amplitudeT *= amplitudeT;
		
		targetAmplitudesPerFreq_temp[freqIndex] += amplitudeT;
	}

	public float Sample(double time)
	{
		float sum = 0;
		
		for (int i = 0; i < freqsAll.Length; i++)
		{
			float amplitude = targetAmplitudesPerFreq[i];
			double phase = time * 2 * MathF.PI * freqsAll[i];
			sum += SquareWave(phase) * amplitude;
		}
		
		return sum;
	}

	static float Wave(double phase)
	{
		return (float)Math.Sin(phase);
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

		return (float)sum * (4 / MathF.PI);
	}



	static float CalculateFrequency(int numAboveA0)
	{
		const double A0Frequency = 27.5;
		return (float)(A0Frequency * Math.Pow(1.059463094359, numAboveA0));
	}
}