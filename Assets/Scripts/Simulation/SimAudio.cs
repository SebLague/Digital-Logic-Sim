using System;
using Seb.Helpers;
using UnityEngine;

namespace DLS.Simulation
{
	public class SimAudio
	{
		public const int freqCount = 256;

		public readonly float[] freqsAll = new float[freqCount];
		readonly double[] targetAmplitudesPerFreq_temp = new double[freqCount];
		public readonly double[] targetAmplitudesPerFreq = new double[freqCount];
		// Very crude correction factors to make different frequencies sound more equal in volume
		// (boosts amplitude of low frequencies)
		readonly float[] perceptualGainCorrection = new float[freqCount];
		
		// ---- State ----
		bool hasInputSinceLastInit;
		bool isSmoothing;

		public SimAudio()
		{
			for (int i = 0; i < freqsAll.Length; i++)
			{
				freqsAll[i] = CalculateFrequency(i / 3.0);
				float freqT = i / 255f;
				perceptualGainCorrection[i] = Maths.Lerp(2, 0.35f, Maths.EaseQuadInOut(freqT));
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
			targetAmplitudesPerFreq_temp[index] += amplitudeT * perceptualGainCorrection[index];
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