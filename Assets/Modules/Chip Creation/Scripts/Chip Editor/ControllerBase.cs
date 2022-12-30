using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	public abstract class ControllerBase : MonoBehaviour
	{
		protected ChipEditor chipEditor;

		public virtual void SetUp(ChipEditor editor)
		{
			this.chipEditor = editor;
		}

		public virtual bool IsBusy()
		{
			return false;
		}
	}
}