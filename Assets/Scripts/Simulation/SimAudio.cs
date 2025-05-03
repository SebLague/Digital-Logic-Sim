using System;

namespace DLS.Simulation
{
	public class SimAudio
	{
		public const int freqCount = 28; // 16 naturals (4bit input) + their sharps
		const int C3_StartIndex = 27; // relative to A0

		readonly int[] naturalToAllMap = new int[16];
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
			if (!hasInputSinceLastInit) return;
			hasInputSinceLastInit = false;

			for (int i = 0; i < targetAmplitudesPerFreq_temp.Length; i++)
			{
				targetAmplitudesPerFreq_temp[i] = 0;
			}
		}

		public void NotifyAllNotesRegistered(double deltaTime)
		{
			if (!hasInputSinceLastInit && !isSmoothing) return;

			const float smoothSpeed = 30f;
			double step = deltaTime * smoothSpeed;
			isSmoothing = false;

			for (int i = 0; i < targetAmplitudesPerFreq.Length; i++)
			{
				// Crude smoothing to avoid jarring frequency jumps
				double curr = targetAmplitudesPerFreq[i];
				double target = targetAmplitudesPerFreq_temp[i];
				double valNew = curr + (target - curr) * step;
				if (target == 0 && valNew <= 0.0001) valNew = 0;

				targetAmplitudesPerFreq[i] = valNew;

				isSmoothing |= valNew > 0;
			}
		}

		public void RegisterNote(int naturalIndex, bool isSharp, uint volume)
		{
			if (volume == 0) return;

			hasInputSinceLastInit = true;
			int freqIndex = GetFrequencyIndex(naturalIndex, isSharp);
			float amplitudeT = MathF.Min(volume / 15f, 1);

			targetAmplitudesPerFreq_temp[freqIndex] += amplitudeT;
		}

		public int GetFrequencyIndex(int naturalIndex, bool isSharp)
		{
			int freqIndex = naturalToAllMap[naturalIndex] + (isSharp ? 1 : 0);
			return freqIndex;
		}

		static float CalculateFrequency(int numAboveA0)
		{
			const double A0Frequency = 27.5;
			return (float)(A0Frequency * Math.Pow(1.059463094359, numAboveA0));
		}
	}
}