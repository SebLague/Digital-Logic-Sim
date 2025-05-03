using System;
using UnityEngine;

namespace DLS.Simulation
{
	public class SimAudio
	{
		public const int freqCount = 256;
		public readonly float[] freqsAll;

		readonly double[] targetAmplitudesPerFreq_temp = new double[freqCount];
		public readonly double[] targetAmplitudesPerFreq = new double[freqCount];
		bool hasInputSinceLastInit;
		bool isSmoothing;

		public SimAudio()
		{
			freqsAll = new float[freqCount];

			for (int i = 0; i < freqsAll.Length; i++)
			{
				freqsAll[i] = CalculateFrequency(i / 4.0);
			}
		}

		public void InitFrame()
		{
			if (!hasInputSinceLastInit) return;
			hasInputSinceLastInit = false;

			for (int i = 0; i < targetAmplitudesPerFreq_temp.Length; i++)
			{
				targetAmplitudesPerFreq_temp[i] = 0;
			}
		}
		
		public void RegisterNote(int index, uint volume)
		{
			if (volume == 0) return;

			hasInputSinceLastInit = true;
			float amplitudeT = MathF.Min(volume / 15f, 1);
			targetAmplitudesPerFreq_temp[index] += amplitudeT;
		}

		public void NotifyAllNotesRegistered(double deltaTime)
		{
			if (!hasInputSinceLastInit && !isSmoothing) return;

			const float smoothSpeed = 30f;
			double step = Math.Min(1, deltaTime * smoothSpeed);
			isSmoothing = false;
		
			for (int i = 0; i < targetAmplitudesPerFreq.Length; i++)
			{
				// Crude smoothing to avoid jarring frequency jumps
				double curr = targetAmplitudesPerFreq[i];
				double target = targetAmplitudesPerFreq_temp[i];
				double delta = target - curr;
				double valNew = curr + delta * step;
				double error = Math.Abs(valNew - target);
				
				if (error <= 0.0001) valNew = target;
				targetAmplitudesPerFreq[i] = valNew;
				
				isSmoothing |= valNew > 0;
			}
			
		}
		

		public static float CalculateFrequency(double numAboveA0)
		{
			const double A0Frequency = 27.5;
			return (float)(A0Frequency * Math.Pow(1.059463094359, numAboveA0));
		}
	}
}