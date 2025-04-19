using System;
using Seb.Helpers;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Seb.Vis.UI
{
	public class WheelSelectorState
	{
		public int index;
	}

	public class ScrollBarState
	{
		public float dragScrollOffset;
		public bool isDragging;
		public float scrollY;
	}

	public struct SliderState
	{
		public float progressT;
		public bool handleSelected;
	}

	public class ColourPickerState
	{
		public float hue;
		public float sat;
		public float val;
		public bool satValHandleSelected;
		public bool hueHandleSelected;

		public Color GetRGB() => Color.HSVToRGB(hue, sat, val);

		public void SetRGB(Color col)
		{
			(hue, sat, val) = ColHelper.GetHSV(col);
		}
	}


	public class ButtonState
	{
		int buttonPressFrame;
		int buttonReleaseFrame;
		public bool isDown;
		public bool toggleState;

		public bool ButtonPressedThisFrame => buttonPressFrame == Time.frameCount;
		public bool ButtonReleasedThisFrame => buttonReleaseFrame == Time.frameCount;

		public void NotifyPressed()
		{
			isDown = true;
			buttonPressFrame = Time.frameCount;
		}

		public void NotifyReleased()
		{
			isDown = false;
			buttonReleaseFrame = Time.frameCount;
		}

		public void NotifyCancelled()
		{
			isDown = false;
		}
	}

	public class InputFieldState
	{
		public TriggerState arrowKeyTrigger;
		public TriggerState backspaceTrigger;
		public int cursorBeforeCharIndex;
		public TriggerState deleteTrigger;
		public bool isMouseDownInBounds;
		public bool isSelecting;
		public float lastInputTime;
		public int selectionStartIndex;
		public string text { get; private set; } = string.Empty;
		public bool focused { get; private set; }

		public int SelectionMinIndex => Mathf.Min(selectionStartIndex, cursorBeforeCharIndex);
		public int SelectionMaxIndex => Mathf.Max(selectionStartIndex, cursorBeforeCharIndex);

		public void ClearText()
		{
			text = string.Empty;
			cursorBeforeCharIndex = 0;
			isSelecting = false;
		}

		public void SetFocus(bool newFocusState)
		{
			if (newFocusState != focused)
			{
				focused = newFocusState;
				lastInputTime = Time.time;

				if (newFocusState == false)
				{
					isSelecting = false;
				}
			}
		}

		public void Delete(bool deleteLeft, Func<string, bool> validation = null)
		{
			if (text.Length == 0) return;

			if (isSelecting)
			{
				isSelecting = false;
				int deleteStartIndex = Math.Min(cursorBeforeCharIndex, selectionStartIndex);
				int deleteCount = Math.Abs(cursorBeforeCharIndex - selectionStartIndex);
				string newText = text.Remove(deleteStartIndex, deleteCount);

				if (validation == null || validation(newText))
				{
					text = newText;
					if (cursorBeforeCharIndex > selectionStartIndex)
					{
						SetCursorIndex(selectionStartIndex);
					}
				}
			}
			else
			{
				if (deleteLeft && cursorBeforeCharIndex > 0)
				{
					string newText = text.Remove(cursorBeforeCharIndex - 1, 1);
					if (validation == null || validation(newText))
					{
						text = newText;
						DecrementCursor();
					}
				}
				else if (!deleteLeft && cursorBeforeCharIndex < text.Length)
				{
					string newText = text.Remove(cursorBeforeCharIndex, 1);
					if (validation == null || validation(newText)) text = newText;
				}
			}

			UpdateLastInputTime();
		}

		public void SelectAll()
		{
			SetCursorIndex(0);
			SetCursorIndex(text.Length, true);
		}

		public void SetCursorIndex(int i, bool select = false)
		{
			if (select && !isSelecting)
			{
				isSelecting = true;
				selectionStartIndex = cursorBeforeCharIndex;
			}

			cursorBeforeCharIndex = i;
			cursorBeforeCharIndex = Mathf.Clamp(cursorBeforeCharIndex, 0, text.Length);
			UpdateLastInputTime();

			if (cursorBeforeCharIndex == selectionStartIndex || !select)
			{
				isSelecting = false;
			}
		}

		public void UpdateLastInputTime()
		{
			lastInputTime = Time.time;
		}

		public void SetText(string newText, bool focus = true)
		{
			if (string.IsNullOrEmpty(newText)) text = string.Empty;
			else text = newText;

			SetCursorIndex(text.Length);
			SetFocus(focus);
		}

		public void TryInsertText(string textToAdd, Func<string, bool> validation = null)
		{
			string originalText = text;

			if (isSelecting) Delete(true);

			string newText = text;
			if (cursorBeforeCharIndex == text.Length) newText += textToAdd;
			else newText = newText.Insert(cursorBeforeCharIndex, textToAdd);

			if (validation == null || validation(newText))
			{
				text = newText;
				SetCursorIndex(cursorBeforeCharIndex + textToAdd.Length);
			}
			else text = originalText;
		}

		public void IncrementCursor(bool select = false)
		{
			if (isSelecting && !select) SetCursorIndex(SelectionMaxIndex); // jump to end of active selection
			else SetCursorIndex(cursorBeforeCharIndex + 1, select);
		}

		public void DecrementCursor(bool select = false)
		{
			if (isSelecting && !select) SetCursorIndex(SelectionMinIndex); // jump to start of active selection
			else SetCursorIndex(cursorBeforeCharIndex - 1, select);
		}

		public int NextWordEndIndex()
		{
			bool hasEncounteredNonSpaceChar = false;

			for (int i = cursorBeforeCharIndex; i < text.Length; i++)
			{
				if (char.IsWhiteSpace(text[i]) && hasEncounteredNonSpaceChar) return i;
				if (!char.IsWhiteSpace(text[i])) hasEncounteredNonSpaceChar = true;
			}

			return text.Length;
		}

		public int PrevWordIndex()
		{
			bool hasEncounteredNonSpaceChar = false;

			for (int i = cursorBeforeCharIndex - 1; i >= 0; i--)
			{
				if (char.IsWhiteSpace(text[i]) && hasEncounteredNonSpaceChar) return i + 1;
				if (!char.IsWhiteSpace(text[i])) hasEncounteredNonSpaceChar = true;
			}

			return 0;
		}

		public struct TriggerState
		{
			public float lastManualTime;
			public float lastAutoTiggerTime;
		}
	}

	public class TextAreaState
	{
		public TriggerState arrowKeyTrigger;
		public TriggerState backspaceTrigger;
		public int cursorBeforeCharIndex;
		public int cursorLineIndex;
		public TriggerState deleteTrigger;
		public bool isMouseDownInBounds;
		public bool isSelecting;
		public float lastInputTime;
		public int selectionStartIndex;
		public List<string> lines { get; private set; } = new();
		public bool focused { get; private set; }
		public int maxCharsPerLine = 0;

		public int SelectionMinIndex => Mathf.Min(selectionStartIndex, cursorBeforeCharIndex);
		public int SelectionMaxIndex => Mathf.Max(selectionStartIndex, cursorBeforeCharIndex);

		public void ClearText()
		{
			lines.Clear();
			lines.Add(string.Empty);
			cursorBeforeCharIndex = 0;
			cursorLineIndex = 0;
			isSelecting = false;
		}

		public void SetFocus(bool newFocusState)
		{
			if (newFocusState != focused)
			{
				focused = newFocusState;
				lastInputTime = Time.time;

				if (!newFocusState)
				{
					isSelecting = false;
				}
			}
		}

		public void Delete(bool deleteLeft, Func<string, bool> validation = null)
		{
			if (lines.Count == 0) return;

			string currentLine = lines[cursorLineIndex];

			if (isSelecting)
			{
				isSelecting = false;
				DeleteSelection(validation);
			}
			else
			{
				if (deleteLeft && cursorBeforeCharIndex > 0)
				{
					// Delete a character to the left of the cursor
					string newLine = currentLine.Remove(cursorBeforeCharIndex - 1, 1);
					if (validation == null || validation(newLine))
					{
						lines[cursorLineIndex] = newLine;
						DecrementCursor();
					}
				}
				else if (!deleteLeft && cursorBeforeCharIndex < currentLine.Length)
				{
					// Delete a character to the right of the cursor
					string newLine = currentLine.Remove(cursorBeforeCharIndex, 1);
					if (validation == null || validation(newLine))
					{
						lines[cursorLineIndex] = newLine;
					}
				}
				else if (deleteLeft && cursorBeforeCharIndex == 0 && cursorLineIndex > 0)
				{
					// Merge with the previous line
					string previousLine = lines[cursorLineIndex - 1];
					lines[cursorLineIndex - 1] = previousLine + currentLine;
					lines.RemoveAt(cursorLineIndex);
					cursorLineIndex--;
					cursorBeforeCharIndex = previousLine.Length;
				}
				else if (!deleteLeft && cursorBeforeCharIndex == currentLine.Length && cursorLineIndex < lines.Count - 1)
				{
					// Merge with the next line
					string nextLine = lines[cursorLineIndex + 1];
					lines[cursorLineIndex] += nextLine;
					lines.RemoveAt(cursorLineIndex + 1);
				}
			}

			// Ensure the current line is valid and handle edge cases
			if (lines.Count == 0)
			{
				lines.Add(string.Empty);
				cursorLineIndex = 0;
				cursorBeforeCharIndex = 0;
			}
			else if (cursorLineIndex >= lines.Count)
			{
				cursorLineIndex = lines.Count - 1;
				cursorBeforeCharIndex = lines[cursorLineIndex].Length;
			}
			else if (cursorBeforeCharIndex > lines[cursorLineIndex].Length)
			{
				cursorBeforeCharIndex = lines[cursorLineIndex].Length;
			}

			UpdateLastInputTime();
		}

		private void DeleteSelection(Func<string, bool> validation = null)
		{
			int startLine = Mathf.Min(cursorLineIndex, selectionStartIndex / maxCharsPerLine);
			int endLine = Mathf.Max(cursorLineIndex, selectionStartIndex / maxCharsPerLine);

			int startChar = Mathf.Min(cursorBeforeCharIndex, selectionStartIndex % maxCharsPerLine);
			int endChar = Mathf.Max(cursorBeforeCharIndex, selectionStartIndex % maxCharsPerLine);

			if (startLine == endLine)
			{
				string line = lines[startLine];
				string newLine = line.Remove(startChar, endChar - startChar);
				if (validation == null || validation(newLine))
				{
					lines[startLine] = newLine;
					cursorBeforeCharIndex = startChar;
				}
			}
			else
			{
				string startLineText = lines[startLine].Substring(0, startChar);
				string endLineText = lines[endLine].Substring(endChar);
				lines[startLine] = startLineText + endLineText;

				for (int i = endLine; i > startLine; i--)
				{
					lines.RemoveAt(i);
				}

				cursorLineIndex = startLine;
				cursorBeforeCharIndex = startChar;
			}
		}

		public void NewLine()
		{
			string currentLine = lines[cursorLineIndex];
			string newLine = currentLine.Substring(cursorBeforeCharIndex); // Text after the caret
			lines[cursorLineIndex] = currentLine.Substring(0, cursorBeforeCharIndex) + "\n"; // Text before the caret with \n

			// Insert the new line into the list
			lines.Insert(cursorLineIndex + 1, newLine);

			// Move the caret to the start of the new line
			cursorLineIndex++;
			cursorBeforeCharIndex = 0;

			UpdateLastInputTime();
		}

		public void SelectAll()
		{
			cursorLineIndex = 0;
			cursorBeforeCharIndex = 0;
			selectionStartIndex = lines.Sum(line => line.Length + 1) - 1; // Account for line breaks
			isSelecting = true;
		}

		public void SetCursorIndex(int charIndex, int lineIndex, bool select = false)
		{
			if (select && !isSelecting)
			{
				isSelecting = true;
				selectionStartIndex = cursorBeforeCharIndex + cursorLineIndex * maxCharsPerLine;
			}

			cursorLineIndex = Mathf.Clamp(lineIndex, 0, lines.Count - 1);
			cursorBeforeCharIndex = Mathf.Clamp(charIndex, 0, lines[cursorLineIndex].Length);

			UpdateLastInputTime();

			if (cursorBeforeCharIndex == selectionStartIndex || !select)
			{
				isSelecting = false;
			}
		}

		public void UpdateLastInputTime()
		{
			lastInputTime = Time.time;
		}

		public void SetText(List<string> newLines, bool focus = true)
		{
			lines = newLines ?? new List<string> { string.Empty };
			cursorLineIndex = 0;
			cursorBeforeCharIndex = 0;
			SetFocus(focus);
		}

		public void TryInsertText(string textToAdd, Func<string, bool> validation = null)
		{
			if (isSelecting) Delete(true);

			string currentLine = lines[cursorLineIndex];
			string newLine = currentLine.Insert(cursorBeforeCharIndex, textToAdd);

			if (validation == null || validation(newLine))
			{
				lines[cursorLineIndex] = newLine;
				cursorBeforeCharIndex += textToAdd.Length;

				// Handle wrapping if maxCharsPerLine is set
				if (maxCharsPerLine > 0)
				{
					while (lines[cursorLineIndex].Length > maxCharsPerLine)
					{
						string currentText = lines[cursorLineIndex];
						int wrapIndex = FindWrapIndex(currentText);

						// Split the line at the wrap index
						string overflowText = currentText.Substring(wrapIndex);
						lines[cursorLineIndex] = currentText.Substring(0, wrapIndex);

						// Add overflow text to the next line
						if (cursorLineIndex + 1 < lines.Count)
						{
							lines[cursorLineIndex + 1] = overflowText + lines[cursorLineIndex + 1];
						}
						else
						{
							lines.Add(overflowText);
						}

						// Adjust cursor position
						cursorLineIndex++;
						cursorBeforeCharIndex = overflowText.Length;
					}
				}
			}
		}

		private int FindWrapIndex(string text)
		{
			if (text.Length <= maxCharsPerLine) return text.Length;

			// Look for the last whitespace within the maxCharsPerLine limit
			for (int i = maxCharsPerLine; i > 0; i--)
			{
				if (char.IsWhiteSpace(text[i]))
				{
					return i + 1; // Include the space in the wrap
				}
			}

			// If no whitespace is found, split at maxCharsPerLine
			return maxCharsPerLine;
		}

		public void IncrementCursor(bool select = false)
		{
			if (cursorBeforeCharIndex < lines[cursorLineIndex].Length)
			{
				SetCursorIndex(cursorBeforeCharIndex + 1, cursorLineIndex, select);
			}
			else if (cursorLineIndex < lines.Count - 1)
			{
				SetCursorIndex(0, cursorLineIndex + 1, select);
			}
		}

		public void DecrementCursor(bool select = false)
		{
			if (cursorBeforeCharIndex > 0)
			{
				SetCursorIndex(cursorBeforeCharIndex - 1, cursorLineIndex, select);
			}
			else if (cursorLineIndex > 0)
			{
				SetCursorIndex(lines[cursorLineIndex - 1].Length, cursorLineIndex - 1, select);
			}
		}

		public void IncrementLine(bool select = false)
		{
			if (cursorLineIndex == lines.Count)
			{
				SetCursorIndex(lines[cursorLineIndex].Length, cursorLineIndex, select);
			}
			else if (cursorLineIndex < lines.Count - 1)
			{
				if (lines[cursorLineIndex + 1].Length < cursorBeforeCharIndex)
				{
					cursorBeforeCharIndex = lines[cursorLineIndex + 1].Length;
				}
				SetCursorIndex(cursorBeforeCharIndex, cursorLineIndex + 1, select);
			}
		}

		public void DecrementLine(bool select = false)
		{
			if (cursorLineIndex == 0)
			{
				SetCursorIndex(0, cursorLineIndex, select);
			}
			else if (cursorLineIndex > 0)
			{
				if (lines[cursorLineIndex - 1].Length < cursorBeforeCharIndex)
				{
					cursorBeforeCharIndex = lines[cursorLineIndex - 1].Length;
				}
				SetCursorIndex(cursorBeforeCharIndex, cursorLineIndex - 1, select);
			}
		}

		public int NextWordEndIndex()
		{
			string currentLine = lines[cursorLineIndex];
			for (int i = cursorBeforeCharIndex; i < currentLine.Length; i++)
			{
				if (char.IsWhiteSpace(currentLine[i])) return i;
			}

			return currentLine.Length;
		}

		public int PrevWordIndex()
		{
			string currentLine = lines[cursorLineIndex];
			for (int i = cursorBeforeCharIndex - 1; i >= 0; i--)
			{
				if (char.IsWhiteSpace(currentLine[i])) return i + 1;
			}

			return 0;
		}

		public struct TriggerState
		{
			public float lastManualTime;
			public float lastAutoTiggerTime;
		}
	}
}