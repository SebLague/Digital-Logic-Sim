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
		public static UnityMain instance;
		public bool openSaveDirectory;
		public AudioUnity audioUnity;

		[Header("Dev Settings (editor only)")]
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
		public AudioState.WaveType waveType;
		public int waveIts = 20;
		public bool songTestMode;
		public float overtoneWeight = 1;
		public float[] overtones;
		public int noteIndex;
		public NoteTest[] notes;

		[System.Serializable]
		public struct NoteTest
		{
			public int noteIndex;
			public bool isSharp;
			public float delay;
			public float duration;
		}

		void Awake()
		{
			instance = this;
			ResetStatics();

			AudioState audioState = new();
			audioUnity.audioState = audioState;

			Main.Init(audioState);


			if (openInMainMenu || !Application.isEditor) Main.LoadMainMenu();
			else Main.CreateOrLoadProject(testProjectName, openA ? chipToOpenA : chipToOpenB);
		}

		void Update()
		{
			if (Application.isEditor) EditorDebugUpdate();

			if (songTestMode) SongTest();
			else Main.Update();
		}

		void SongTest()
		{
			audioUnity.audioState.waveIterations = waveIts;
			audioUnity.audioState.waveType = waveType;
			audioUnity.audioState.InitFrame();

			audioUnity.audioState.RegisterNote(noteIndex, false, 15);
			for (int i = 0; i < overtones.Length; i++)
			{
				audioUnity.audioState.RegisterOvertone(6, false, i + 1, overtones[i] * overtoneWeight);
			}

			audioUnity.audioState.NotifyAllNotesRegistered();
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
	}
}