using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using DLS.ChipData;
using System.Linq;
using SebInput;

namespace DLS.ChipCreation
{
	public abstract class ChipBase : MonoBehaviour
	{

		public event System.Action<ChipBase> ChipDeleted;
		public MouseInteraction<ChipBase> MouseInteraction { get; protected set; }

		public string Name => Description.Name;
		public ChipDescription Description { get; private set; }
		public ReadOnlyCollection<Pin> InputPins { get; private set; }
		public ReadOnlyCollection<Pin> OutputPins { get; private set; }
		public ReadOnlyCollection<Pin> AllPins { get; private set; }
		public Vector2 Size { get; protected set; }
		public int ID { get; private set; }

		Dictionary<int, Pin> pinsByID;

		// Load chip from save file
		public virtual void Load(ChipDescription description, ChipInstanceData instanceData)
		{
			Init(description, instanceData.ID);
		}

		// Start placing chip from the editor
		public virtual void StartPlacing(ChipDescription description, int id)
		{
			Init(description, id);
		}

		// Finished placing a chip in the editor
		public virtual void FinishPlacing() { }

		// Get subchip instance data
		public abstract ChipInstanceData GetInstanceData();

		public virtual Bounds GetBounds()
		{
			return new Bounds(Vector3.zero, Vector2.one);
		}


		public virtual void Delete()
		{
			if (AllPins is not null)
			{
				foreach (Pin pin in AllPins)
				{
					pin.NotifyOfDeletion();
				}
			}
			ChipDeleted?.Invoke(this);
			Destroy(gameObject);
		}

		public virtual void SetHighlightState(bool highlighted)
		{
		}

		public virtual void NotifyMoved()
		{

		}

		public Pin GetPinByID(int id)
		{
			return pinsByID[id];
		}

		protected void SetPins(Pin[] inputPins, Pin[] outputPins)
		{
			InputPins = new ReadOnlyCollection<Pin>(inputPins);
			OutputPins = new ReadOnlyCollection<Pin>(outputPins);
			AllPins = new ReadOnlyCollection<Pin>(inputPins.Concat(outputPins).ToArray());

			foreach (var pin in inputPins)
			{
				pinsByID.Add(pin.ID, pin);
			}
			foreach (var pin in outputPins)
			{
				pinsByID.Add(pin.ID, pin);
			}
		}

		void Init(ChipDescription description, int id)
		{
			Description = description;
			ID = id;
			pinsByID = new Dictionary<int, Pin>();
		}

	}
}