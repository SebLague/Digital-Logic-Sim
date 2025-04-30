using System;
using UnityEngine;

public class AudioState
{
	public class Note
	{
		public readonly double frequency;
		public double attackDuration = 20 / 1000.0;
		public double releaseDuration = 40 / 1000.0;

		public double notePressedTime;
		public double noteReleasedTime;
		public bool noteIsHeldDown;

		double amplitudeAtRelease;

		public bool playing;
		double velocity;

		public Note(double frequency)
		{
			this.frequency = frequency;
		}

		public void PressNote(double time, double velocity = 1)
		{
			notePressedTime = time;
			noteIsHeldDown = true;
			playing = true;
			this.velocity = velocity;
		}

		public void ReleaseNote(double time)
		{
			amplitudeAtRelease = CalculateAttackAmplitude(time);
			noteReleasedTime = time;
			noteIsHeldDown = false;
		}

		public double GetAmplitude(double time)
		{
			if (noteIsHeldDown)
			{
				return CalculateAttackAmplitude(time) * velocity;
			}
			else
			{
				double a = CalculateReleaseAmplitude(time) * velocity;
				playing = a > 0;
				return a;
			}
		}

		double CalculateAttackAmplitude(double time)
		{
			double timeSinceNotePressed = time - notePressedTime;
			return Clamp01(timeSinceNotePressed / attackDuration);
		}

		double CalculateReleaseAmplitude(double time)
		{
			double timeSinceNoteReleased = time - noteReleasedTime;
			double releaseT = Clamp01(timeSinceNoteReleased / releaseDuration);
			return amplitudeAtRelease * (1 - releaseT);
		}

		double Clamp01(double x)
		{
			return Math.Clamp(x, 0, 1);
		}
	}
}