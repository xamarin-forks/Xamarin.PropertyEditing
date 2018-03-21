using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using Foundation;
using AppKit;
using CoreGraphics;

using Xamarin.PropertyEditing.ViewModels;
using Xamarin.PropertyEditing.Mac.Resources;
using System.Collections.Generic;
using System.Linq;

namespace Xamarin.PropertyEditing.Mac
{
	internal class CombinablePropertyEditor<T>
		: PropertyEditorControl
	{
		public CombinablePropertyEditor ()
		{
			base.TranslatesAutoresizingMaskIntoConstraints = false;

			UpdateTheme ();
		}

		public override NSView FirstKeyView => firstKeyView;
		public override NSView LastKeyView => lastKeyView;

		internal new CombinablePropertyViewModel<T> ViewModel
		{
			get { return (CombinablePropertyViewModel<T>)base.ViewModel; }
			set { base.ViewModel = value; }
		}

		List<NSButton> combinableList = new List<NSButton> ();
		NSView firstKeyView;
		NSView lastKeyView;

		protected override void HandleErrorsChanged (object sender, DataErrorsChangedEventArgs e)
		{
			UpdateErrorsDisplayed (ViewModel.GetErrors (e.PropertyName));
		}

		protected override void SetEnabled ()
		{
			foreach (var item in combinableList) {
				item.Enabled = ViewModel.Property.CanWrite;
			}
		}

		protected override void UpdateErrorsDisplayed (IEnumerable errors)
		{
			if (ViewModel.HasErrors) {
				SetErrors (errors);
			} else {
				SetErrors (null);
				SetEnabled ();
			}
		}

		protected override void OnViewModelChanged (PropertyViewModel oldModel)
		{
			combinableList.Clear ();

			const float controlHeight = 22;

			// Set our new RowHeight
			RowHeight = (ViewModel.Choices.Count * controlHeight);

			float top = controlHeight;
			foreach (var item in ViewModel.Choices) {
				var BooleanEditor = new NSButton (new CGRect (4, RowHeight - top, Frame.Width - 4, controlHeight)) {
					AllowsMixedState = true,
					ControlSize = NSControlSize.Small,
					Font = NSFont.FromFontName (DefaultFontName, DefaultFontSize),
					Title = item.Name,
					TranslatesAutoresizingMaskIntoConstraints = false,
				};
				BooleanEditor.SetButtonType (NSButtonType.Switch);
				BooleanEditor.Activated += SelectionChanged;

				AddSubview (BooleanEditor);
				combinableList.Add (BooleanEditor);
				top += controlHeight;
			}

			// Set our tabable order
			firstKeyView = combinableList.First ();
			lastKeyView = combinableList.Last ();

			base.OnViewModelChanged (oldModel);
		}

		protected override void UpdateValue ()
		{
			foreach (var item in combinableList) {
				if (ViewModel.Choices.Count > 0) {
					var flag = ViewModel.Choices.FirstOrDefault (x => x.Name == item.Title);
					if (flag.IsFlagged.HasValue) {
						item.State = flag.IsFlagged.Value ? NSCellStateValue.On : NSCellStateValue.Off;
						item.AllowsMixedState = false;
					} else {
						item.State = NSCellStateValue.Mixed;
					}
				} else {
					item.State = NSCellStateValue.Off;
				}
			}
		}

		protected override void UpdateAccessibilityValues ()
		{
			foreach (var item in combinableList) {
				item.AccessibilityEnabled = item.Enabled;
				item.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityCombobox, ViewModel.Property.Name);
			}
		}

		void SelectionChanged (object sender, EventArgs e)
		{
			if (sender is NSButton button) {
				var choice = ViewModel.Choices.First (x => x.Name == button.Title);
				switch (button.State) {
					case NSCellStateValue.Off:
						choice.IsFlagged = false;
						break;
					case NSCellStateValue.On:
						choice.IsFlagged = true;
						break;
				}
			}
		}
	}
}
