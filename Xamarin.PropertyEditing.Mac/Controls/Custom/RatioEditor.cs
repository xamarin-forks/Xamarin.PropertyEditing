using System;
using AppKit;
using Foundation;
using Xamarin.PropertyEditing.Drawing;
using Xamarin.PropertyEditing.Mac;

namespace Xamarin.PropertyEditing.Mac
{
	public class RatioEditor : NumericSpinEditor
	{
		new public event EventHandler<RatioEventArgs> ValueChanged;

		public RatioEditor ()
		{
			AllowNegativeValues = false;
			AllowRatios = true;
			BackgroundColor = NSColor.Clear;
			StringValue = string.Empty;
			TranslatesAutoresizingMaskIntoConstraints = false;
		}

		override protected void OnEditingEnded (object sender, EventArgs e)
		{
			if (!editing) {
				editing = true;
				ValueChanged?.Invoke (this, new RatioEventArgs (0, 0, 0));
				editing = false;
			}
		}

		override protected void SetIncrementOrDecrementValue (double incrementValue)
		{
			nint caretLocation = 0;
			nint selectionLength = 0;

			GetEditorCaretLocationAndLength (out caretLocation, out selectionLength);

			// Fire A Value change, so things are updated
			ValueChanged?.Invoke (this, new RatioEventArgs ((int)caretLocation, (int)selectionLength, incrementValue));

			// Resposition our caret so it doesn't jump around.
			SetEditorCaretLocationAndLength (caretLocation, selectionLength);
		}

		void SetEditorCaretLocationAndLength (nint caretLocation, nint selectionLength)
		{
			if (NumericEditor.CurrentEditor != null) {
				NumericEditor.CurrentEditor.SelectedRange = new NSRange (caretLocation, selectionLength);
			}
		}

		void GetEditorCaretLocationAndLength (out nint caretLocation, out nint selectionLength)
		{
			caretLocation = 0;
			selectionLength = 0;
			if (NumericEditor.CurrentEditor != null) {
				caretLocation = NumericEditor.CurrentEditor.SelectedRange.Location;
				selectionLength = NumericEditor.CurrentEditor.SelectedRange.Length;
			}
		}

		public class RatioEventArgs : EventArgs
		{
			public RatioEventArgs (int caretPosition, int selectionLength, double incrementValue)
			{
				CaretPosition = caretPosition;
				SelectionLength = selectionLength;
				IncrementValue = incrementValue;
			}

			public int CaretPosition { get; }
			public int SelectionLength { get; }
			public double IncrementValue { get; }
		}
	}
}
