using System.Collections;
using System.Collections.Generic;
using Seb.Helpers;
using UnityEngine;
using static System.Math;

public class AudioTest : MonoBehaviour
{

	public enum WaveType
	{
		Sin,
		Square,
		SquareDigital,
		ReverseSmoothSaw
	}
	int seriesSteps = 20;
	public WaveType waveType;

	public double middleFrequency = 440;

	AudioUnity audioSystem;

	//string keyString = "qwertyuiop[]asdfghjkl;'zxcvbnm,./";
	Note[] notes;

	const int A0KeyIndex = 21; // index of lowest note on my keyboard (yamaha p45) = A0
	const int C8KeyIndex = 108; // index of highest note on my keyboard = C8

	const double A0Frequency = 27.5;


	void Start()
	{
		audioSystem = FindObjectOfType<AudioUnity>();

		audioSystem.SetSampler(Sample);

		int numNotes = C8KeyIndex - A0KeyIndex;
		notes = new Note[numNotes];

		

		for (int i = 0; i < notes.Length; i++)
		{
			int middleNoteIndex = notes.Length / 2;
			//	double noteFrequency = middleFrequency * Pow(2, 1.0 / 7.0 * (i - middleNoteIndex));
			double noteFrequency = A0Frequency * Pow(1.059463094359, i);
			notes[i] = new Note(noteFrequency);
			//Debug.Log(i + "  " + noteFrequency);
		}
		
	}

	/*
	void OnNotePlayed(MidiChannel channel, int noteID, float velocity)
	{
		if (velocity > 0)
		{
			renderer.material.color = Color.red;
			notes[noteID - A0KeyIndex].PressNote(audioSystem.Time, velocity);
			Debug.Log("Play " + notes[noteID - A0KeyIndex].frequency + " velocity = " + velocity);
		}
	}

	void OnNoteReleased(MidiChannel channel, int noteID)
	{
		renderer.material.color = Color.black;
		notes[noteID - A0KeyIndex].ReleaseNote(audioSystem.Time);
		Debug.Log("Release " + notes[noteID - A0KeyIndex].frequency);
	}
*/

	
	void Update()
	{

		
		if (InputHelper.IsKeyDownThisFrame(KeyCode.Space))
		{
			notes[27].PressNote(audioSystem.Time);
		}
		if (InputHelper.IsKeyUpThisFrame(KeyCode.Space))
		{
			notes[27].ReleaseNote(audioSystem.Time);
		}
	}

	public double Sample(double time)
	{
		double sum = 0;

		for (int noteIndex = 0; noteIndex < notes.Length; noteIndex++)
		{
			Note note = notes[noteIndex];
			if (note.playing)
			{
				double amplitude = note.GetAmplitude(time);

				double phase = time * 2 * PI * note.frequency;
				sum += Wave(phase) * amplitude;

			}

		}


		return sum;
	}

	double Wave(double phase)
	{
		switch (waveType)
		{
			case WaveType.Sin:
				return SinWave(phase);
			case WaveType.Square:
				return SquareWave(phase, seriesSteps);
			case WaveType.SquareDigital:
				return Sign(Sin(phase));
			case WaveType.ReverseSmoothSaw:
				return ReverseSmoothSawWave(phase, seriesSteps);
		}
		return 0.0;
	}

	public double SinWave(double t)
	{
		return Sin(t);
	}

	public double SquareWave(double t, int numIterations = 10)
	{
		double sum = 0;
		for (int i = 1; i <= numIterations; i++)
		{
			double numerator = Sin((2 * i - 1) * t);
			double denominator = 2 * i - 1;
			sum += numerator / denominator;
		}

		return sum * (4 / PI);
	}

	public double ReverseSmoothSawWave(double t, int numIterations = 10)
	{
		double sum = 0;
		for (int i = 1; i <= numIterations; i++)
		{
			double numerator = Sin((2 * i) * t);
			double denominator = 2 * i - 1;
			sum += numerator / denominator;
		}

		return sum * (4 / PI);
	}

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
			return Max(0, Min(1, x));
		}
	}
}
