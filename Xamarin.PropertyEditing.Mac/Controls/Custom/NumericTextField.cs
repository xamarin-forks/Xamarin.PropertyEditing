using System;
using AppKit;
using Foundation;
using ObjCRuntime;

namespace Xamarin.PropertyEditing.Mac
{
	public class NumericTextField : NSTextField
	{
		NSText CachedCurrentEditor {
			get; set;
		}

		string cachedValueString;

		public bool AllowNegativeValues {
			get; set;
		}

		public bool AllowRatios {
			get; set;
		}

		public ValidationType NumericMode {
			get; set;
		}

		public event EventHandler KeyArrowUp;
		public event EventHandler KeyArrowDown;
		public event EventHandler ValidatedEditingEnded;

		public NumericTextField ()
		{
			AllowNegativeValues = true;

			var keyUpDownDelegate = new KeyUpDownDelegate ();
			keyUpDownDelegate.KeyArrowUp += (sender, e) => { OnKeyArrowUp (); };
			keyUpDownDelegate.KeyArrowDown += (sender, e) => { OnKeyArrowDown (); };
			Delegate = keyUpDownDelegate;
		}

		public override bool ShouldBeginEditing (NSText textObject)
		{
			CachedCurrentEditor = textObject;
			cachedValueString = textObject.Value;

			if (AllowRatios)
				CachedCurrentEditor.Delegate = new RatioValidateDelegate (this);
			else {
				CachedCurrentEditor.Delegate = new NumericValidationDelegate (this);
			}
			return true;
		}

		protected virtual void OnKeyArrowUp ()
		{
			KeyArrowUp?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnKeyArrowDown ()
		{
			KeyArrowDown?.Invoke (this, EventArgs.Empty);
		}

		public virtual void RaiseValidatedEditingEnded ()
		{
			ValidatedEditingEnded?.Invoke (this, EventArgs.Empty);
		}

		public virtual void ResetInvalidInput ()
		{
			this.StringValue = cachedValueString;
		}

		public static bool CheckIfNumber (string finalString, ValidationType mode, bool allowNegativeValues)
		{
			return mode == ValidationType.Decimal ?
				ValidateDecimal (finalString, allowNegativeValues) :
				ValidateInteger (finalString, allowNegativeValues);
		}

		public static bool ValidateDecimal (string finalString, bool allowNegativeValues)
		{
			double value;
			//Checks parsing to number
			if (!double.TryParse (finalString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentUICulture, out value))
				return false;
			//Checks if needs to be possitive value
			if (!allowNegativeValues && value < 0)
				return false;
			//Checks a common validation
			return ViewModels.RatioViewModel.IsValidInput (finalString);
		}

		public static bool ValidateInteger (string finalString, bool allowNegativeValues)
		{
			int value;
			//Checks parsing to number
			if (!int.TryParse (finalString, out value))
				return false;
			//Checks if needs to be possitive value
			if (!allowNegativeValues && value < 0)
				return false;
			//Checks a common validation
			return ViewModels.RatioViewModel.IsValidInput (finalString);
		}

		public static bool CheckIfRatio (string finalString, ValidationType mode, bool allowNegativeValues)
		{
			var parts = finalString.Split (ViewModels.RatioViewModel.SplitSeparators, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 2) {
				bool parsed = true;

				if (!CheckIfNumber (parts[0], mode, allowNegativeValues))
					parsed = false;

				if (!String.IsNullOrEmpty (parts[1]) && !CheckIfNumber (parts[1], mode, allowNegativeValues))
					parsed = false;

				if (parsed)
					return true;
			} else if (parts.Length == 1) { // We have a potential whole number, let's make sure
				if (CheckIfNumber (parts[0], mode, allowNegativeValues)) {
					return true;
				}
			}
			return false;
		}
	}

	class KeyUpDownDelegate : NSTextFieldDelegate
	{
		public event EventHandler KeyArrowUp;
		public event EventHandler KeyArrowDown;

		public override bool DoCommandBySelector (NSControl control, NSTextView textView, Selector commandSelector)
		{
			switch (commandSelector.Name) {
				case "moveUp:":
					OnKeyArrowUp ();
					break;
				case "moveDown:":
					OnKeyArrowDown ();
					break;
				default:
					return false;
			}

			return true;
		}

		protected virtual void OnKeyArrowUp ()
		{
			KeyArrowUp?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnKeyArrowDown ()
		{
			KeyArrowDown?.Invoke (this, EventArgs.Empty);
		}
	}

	public abstract class TextViewValidationDelegate : NSTextViewDelegate
	{
		protected NumericTextField TextField {
			get; set;
		}

		public TextViewValidationDelegate (NumericTextField textField)
		{
			TextField = textField;
		}

		public override bool TextShouldBeginEditing (NSText textObject)
		{
			return TextField.ShouldBeginEditing (textObject);
		}

		public override bool TextShouldEndEditing (NSText textObject)
		{
			bool shouldEndEditing = false;
			if (!ValidateFinalString (textObject.Value)) {
				TextField.ResetInvalidInput ();
				AppKitFramework.NSBeep ();
				TextField.ShouldEndEditing (textObject);
			} else {
				shouldEndEditing = TextField.ShouldEndEditing (textObject);
			}

			return shouldEndEditing;
		}

		public override void TextDidEndEditing (NSNotification notification)
		{
			TextField.RaiseValidatedEditingEnded ();
			TextField.DidEndEditing (notification);
		}

		protected abstract bool ValidateFinalString (string value);
	}

	public class NumericValidationDelegate : TextViewValidationDelegate
	{
		public NumericValidationDelegate (NumericTextField textField)
			: base (textField)
		{

		}

		protected override bool ValidateFinalString (string value)
		{
			return TextField.NumericMode == ValidationType.Decimal ?
				            NumericTextField.ValidateDecimal (value, TextField.AllowNegativeValues) :
				            NumericTextField.ValidateInteger (value, TextField.AllowNegativeValues);
		}
	}

	public class RatioValidateDelegate : TextViewValidationDelegate
	{
		public RatioValidateDelegate (NumericTextField textField)
			: base (textField)
		{

		}

		protected override bool ValidateFinalString (string value)
		{
			if (NumericTextField.CheckIfRatio (value, ValidationType.Decimal, false) || NumericTextField.CheckIfNumber (value, ValidationType.Decimal, false))
				return true;
			return false;
		}
	}
}
