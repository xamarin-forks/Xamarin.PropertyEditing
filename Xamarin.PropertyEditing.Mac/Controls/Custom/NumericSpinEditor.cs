using System;
using AppKit;
using CoreGraphics;
using Foundation;
using Xamarin.PropertyEditing.Drawing;

namespace Xamarin.PropertyEditing.Mac
{
	public class NumericSpinEditor : NSView
	{
		NumericTextField numericEditor;
		public NumericTextField NumericEditor
		{
			get { return numericEditor; }
		}

		NSButton incrementButton;
		public NSButton IncrementButton
		{
			get { return incrementButton; }
		}
		NSButton decrementButton;
		public NSButton DecrementButton
		{
			get { return decrementButton; }
		}


		protected bool editing;

		public event EventHandler ValueChanged;
		public event EventHandler EditingEnded;

		public ValidationType NumericMode {
			get { return numericEditor.NumericMode; }
			set {
				numericEditor.NumericMode = value;
				Reset ();
			}
		}

		public string PlaceholderString {
			get { return ((NSTextFieldCell)numericEditor.Cell).PlaceholderString; }
			set { ((NSTextFieldCell)numericEditor.Cell).PlaceholderString = value; }
		}

		public override CGSize IntrinsicContentSize {
			get {
				var baseSize = numericEditor.IntrinsicContentSize;
				return new CGSize (baseSize.Width + 20, baseSize.Height);
			}
		}

		public NSColor BackgroundColor {
			get {
				return numericEditor.BackgroundColor;
			}
			set {
				numericEditor.BackgroundColor = value;
			}
		}

		public override nfloat BaselineOffsetFromBottom {
			get { return numericEditor.BaselineOffsetFromBottom; }
		}

		public int Digits {
			get { return (int)formatter.MaximumFractionDigits; }
			set { formatter.MaximumFractionDigits = value; }
		}

		public double Value {
			get { return numericEditor.DoubleValue; }
			set { SetValue (value); }
		}

		public double MinimumValue {
			get { return formatter.Minimum.DoubleValue; }
			set {
				formatter.Minimum = new NSNumber (value);
			}
		}

		public double MaximumValue {
			get { return formatter.Maximum.DoubleValue; }
			set {
				formatter.Maximum = new NSNumber (value);
			}
		}

		double incrementValue = 1.0f;
		public double IncrementValue {
			get { return incrementValue; }
			set { incrementValue = value; }
		}

		public bool Enabled {
			get {
				return numericEditor.Enabled;
			}
			set {
				numericEditor.Enabled = value;
				incrementButton.Enabled = value;
				decrementButton.Enabled = value;
			}
		}

		NSNumberFormatter formatter;
		public NSNumberFormatter Formatter {
			get {
				return formatter;
			}
			set {
				formatter = value;
				numericEditor.Formatter = formatter;
			}
		}

		public bool IsIndeterminate {
			get {
				return !string.IsNullOrEmpty (numericEditor.StringValue);
			}
			set {
				if (value)
					numericEditor.StringValue = string.Empty;
			}
		}

		public bool Editable {
			get {
				return numericEditor.Editable;
			}
			set {
				numericEditor.Editable = value;
				incrementButton.Enabled = value;
				decrementButton.Enabled = value;
			}
		}

		public NSNumberFormatterStyle NumberStyle {
			get { return formatter.NumberStyle; }
			set {
				formatter.NumberStyle = value;
			}
		}

		public bool AllowRatios
		{
			get {
				return numericEditor.AllowRatios;
			}
			set {
				numericEditor.AllowRatios = value;
			}
		}

		public string StringValue
		{
			get { 
				return numericEditor.StringValue; 
			}
			set {
				numericEditor.StringValue = value;
			}
		}

		protected virtual void OnConfigureNumericTextField ()
		{
			numericEditor.Formatter = formatter;
		}

		public bool AllowNegativeValues
		{
			get {
				return numericEditor.AllowNegativeValues;
			}
			set {
				numericEditor.AllowNegativeValues = value;
			}
		}

		public virtual void Reset ()
		{
		}

		public NumericSpinEditor ()
		{
			TranslatesAutoresizingMaskIntoConstraints = false;
			var controlSize = NSControlSize.Small;

			incrementButton = new NSButton {
				BezelStyle = NSBezelStyle.Rounded,
				ControlSize = NSControlSize.Mini,
				Title = string.Empty,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			decrementButton = new NSButton {
				BezelStyle = NSBezelStyle.Rounded,
				ControlSize = NSControlSize.Mini,
				Title = string.Empty,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			formatter = new NSNumberFormatter {
				FormatterBehavior = NSNumberFormatterBehavior.Version_10_4,
				Locale = NSLocale.CurrentLocale,
				MaximumFractionDigits = 15,
				NumberStyle = NSNumberFormatterStyle.Decimal,
				UsesGroupingSeparator = false,
			};

			numericEditor = new NumericTextField {
				Alignment = NSTextAlignment.Right,
				TranslatesAutoresizingMaskIntoConstraints = false,
				Font = NSFont.FromFontName (PropertyEditorControl.DefaultFontName, PropertyEditorControl.DefaultFontSize),
				ControlSize = controlSize,
			};

			incrementButton.Activated += (sender, e) => { IncrementNumericValue (); };
			decrementButton.Activated += (sender, e) => { DecrementNumericValue (); };

			numericEditor.KeyArrowUp += (sender, e) => { IncrementNumericValue (); };
			numericEditor.KeyArrowDown += (sender, e) => { DecrementNumericValue (); };

			numericEditor.ValidatedEditingEnded += (s, e) => {
				OnEditingEnded (s, e);
			};

			AddSubview (numericEditor);
			AddSubview (incrementButton);
			AddSubview (decrementButton);

			this.DoConstraints (new[] {
				numericEditor.ConstraintTo (this, (n, c) => n.Width == c.Width - 16),
				numericEditor.ConstraintTo (this, (n, c) => n.Height == PropertyEditorControl.DefaultControlHeight - 3),
				incrementButton.ConstraintTo (numericEditor, (s, n) => s.Left == n.Right + 5),
				incrementButton.ConstraintTo (numericEditor, (s, n) => s.Top == n.Top),
				incrementButton.ConstraintTo (numericEditor, (s, n) => s.Width == 9),
				incrementButton.ConstraintTo (numericEditor, (s, n) => s.Height == 9),
				decrementButton.ConstraintTo (numericEditor, (s, n) => s.Left == n.Right + 5),
				decrementButton.ConstraintTo (numericEditor, (s, n) => s.Top == n.Top + 10),
				decrementButton.ConstraintTo (numericEditor, (s, n) => s.Width == 9),
				decrementButton.ConstraintTo (numericEditor, (s, n) => s.Height == 9),
			});
		}

		virtual protected void OnEditingEnded (object sender, EventArgs e)
		{
			if (!editing) {
				editing = true;
				SetValue (numericEditor.StringValue);
				EditingEnded?.Invoke (this, EventArgs.Empty);
				editing = false;
			}
		}

		void SetValue (string value)
		{
			numericEditor.StringValue = value;
			ValueChanged?.Invoke (this, EventArgs.Empty);
		}

		public void SetValue (double value)
		{
			SetValue (value.ToString ());
		}

		public void IncrementNumericValue ()
		{
			if (!editing) {
				editing = true;
				SetIncrementOrDecrementValue (IncrementValue);
				editing = false;
			}
		}

		public void DecrementNumericValue ()
		{
			if (!editing) {
				editing = true;
				SetIncrementOrDecrementValue (-IncrementValue);
				editing = false;
			}
		}

		virtual protected void SetIncrementOrDecrementValue (double incrementValue)
		{
			SetValue (numericEditor.DoubleValue + incrementValue);
		}
	}
}
