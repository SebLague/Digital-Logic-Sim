using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipCreation;

namespace DLS.ChipCreation
{
	[CreateAssetMenu(menuName = "DLS/Name Input Validator")]
	public class NameInputValidator : TMPro.TMP_InputValidator
	{

		public const int characterLimit = 18;

		public override char Validate(ref string text, ref int pos, char ch)
		{
			if (text.Length >= characterLimit)
			{
				return '\0';
			}
			if (NameValidationHelper.ForbiddenChars.Contains(ch))
			{
				return '\0';
			}
			if (text.Length > pos)
			{
				text = text.Insert(pos, ch.ToString());
			}
			else
			{
				text += ch;
			}
			//
			pos++;
			return ch;
		}
	}
}