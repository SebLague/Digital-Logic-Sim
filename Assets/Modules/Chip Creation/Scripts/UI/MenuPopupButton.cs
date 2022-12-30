using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation.UI
{
	public class MenuPopupButton : CustomButton
	{
		[SerializeField] TMPro.TMP_Text shortcutText;

		protected override void UpdateTextColour(Color col)
		{
			base.UpdateTextColour(col);
			shortcutText.color = col;
		}
	}
}