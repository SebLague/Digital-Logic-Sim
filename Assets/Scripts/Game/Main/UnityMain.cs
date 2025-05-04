using DLS.Graphics;
using DLS.Simulation;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Game
{
	public class UnityMain : MonoBehaviour
	{
		[Header("Dev Settings (editor only)")]
		public bool openSaveDirectory;
		public bool openInMainMenu;

		public string testProjectName;
		public bool openA = true;
		public string chipToOpenA;
		public string chipToOpenB;

		[Header("Temp test vars")] public Vector2 testVecA;
		public Vector2 testVecB;
		public Vector2 testVecC;
		public Vector2 testVecD;
		public Color testColA;
		public Color testColB;
		public Color testColC;
		public Color testColD;
		public ButtonTheme testButtonTheme;
		public bool testbool;
		public Anchor testAnchor;


		public string testString;
		public string testString2;
		public uint testUint;
		public uint testUint2;
		public bool removeZeros;
		public ushort testUshort;

		[Header("Audio test")]
		[Range(0,255)]public int noteIndex;
		public int numNoteDivisions;
		public double noteFreq;
		public double refNoteFreq;
		public bool useRef;
		public int refIndex;
		public float perceptualGain;
		
		public AudioState.WaveType waveType;

		public bool restart;
		public float speed = 1;
		public float staccatoDelay;
		public int waveIts = 20;
		public bool songTestMode;
		public NoteTest[] notes;
		
		// References
		public static UnityMain instance;
		AudioUnity audioUnity;

		void Awake()
		{
			instance = this;
			audioUnity = FindFirstObjectByType<AudioUnity>();
			ResetStatics();

			AudioState audioState = new();
			audioUnity.audioState = audioState;

			Main.Init(audioState);


			if (openInMainMenu || !Application.isEditor) Main.LoadMainMenu();
			else Main.CreateOrLoadProject(testProjectName, openA ? chipToOpenA : chipToOpenB);

		}

		void Update()
		{
			noteFreq = SimAudio.CalculateFrequency(noteIndex / (double)numNoteDivisions);
			refNoteFreq = SimAudio.CalculateFrequency(refIndex / (double)numNoteDivisions);
			if (Application.isEditor) EditorDebugUpdate();

			if (songTestMode) SongTest();
			else Main.Update();
		}

		void SongTest()
		{
			/*
			audioUnity.audioState.waveIterations = waveIts;
			audioUnity.audioState.waveType = waveType;
			audioUnity.audioState.InitFrame();

			if (restart)
			{
				restart = false;
				time = -0.2f;
			}

			float playT = 0;
			time += Time.deltaTime * speed;
			int i = 0;
			foreach (NoteTest n in notes)
			{
				float startTime = playT + n.delay + staccatoDelay;
				float endTime = startTime + n.duration;
				if (time > startTime && time < endTime)
				{
					audioUnity.audioState.RegisterNote(n.noteIndex, n.isSharp, 15);
					Debug.Log(i + ": " + n.noteIndex + (n.isSharp ? " Sharp " : "Natural") + $"  Freq = {audioUnity.audioState.GetFrequency(n.noteIndex, n.isSharp):0.00}");
				}

				i++;
				playT = endTime;
			}

			audioUnity.audioState.NotifyAllNotesRegistered();
			*/
		}

		void EditorDebugUpdate()
		{
			if (InputHelper.AltIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.Return))
			{
				if (InteractionState.PinUnderMouse != null)
				{
					SimPin simPin = Project.ActiveProject.rootSimChip.GetSimPinFromAddress(InteractionState.PinUnderMouse.Address);
					ushort bitData = PinState.GetBitStates(simPin.State);
					ushort tristateFlags = PinState.GetTristateFlags(simPin.State);
					string bitString = StringHelper.CreateBinaryString(bitData, true);
					string triStateString = StringHelper.CreateBinaryString(tristateFlags, true);

					string displayString = "";
					for (int i = 0; i < bitString.Length; i++)
					{
						if (triStateString[i] == '1')
						{
							displayString += bitString[i] == '1' ? "?" : "x";
						}
						else
						{
							displayString += bitString[i];
						}
					}

					Debug.Log($"Pin state: {displayString}");
				}
			}
		}

		void OnDestroy()
		{
			if (Project.ActiveProject != null) Project.ActiveProject.NotifyExit();
		}

		void OnValidate()
		{
			if (openSaveDirectory)
			{
				openSaveDirectory = false;
				Main.OpenSaveDataFolderInFileBrowser();
			}
		}

		// Ensure static stuff gets properly reset (on account of domain-reloading being disabled in editor)
		static void ResetStatics()
		{
			Simulator.Reset();
			UIDrawer.Reset();
			InteractionState.Reset();
			CameraController.Reset();
			WorldDrawer.Reset();
		}

		[System.Serializable]
		public struct NoteTest
		{
			public int noteIndex;
			public bool isSharp;
			public float delay;
			public float duration;
		}
	}
}