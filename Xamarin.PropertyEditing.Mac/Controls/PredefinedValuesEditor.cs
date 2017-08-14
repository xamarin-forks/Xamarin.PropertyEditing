using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using ObjCRuntime;
using Foundation;
using AppKit;
using CoreGraphics;

using Xamarin.PropertyEditing.ViewModels;
using Xamarin.PropertyEditing.Mac.Resources;
using System.Collections.Generic;
using System.Linq;

namespace Xamarin.PropertyEditing.Mac
{
	internal class PredefinedValuesEditor<T>
		: PropertyEditorControl
	{
		const string setBezelColorSelector = "setBezelColor:";

		private readonly NSComboBox comboBox;
		List<NSButton> combinableList = new List<NSButton> ();
		bool dataPopulated;
		NSView firstKeyView;
		NSView lastKeyView;

		public PredefinedValuesEditor ()
		{
			base.TranslatesAutoresizingMaskIntoConstraints = false;

			this.comboBox = new NSComboBox {
				TranslatesAutoresizingMaskIntoConstraints = false,
				BackgroundColor = NSColor.Clear,
				StringValue = String.Empty,
				ControlSize = NSControlSize.Small,
				Editable = false,
				Font = NSFont.FromFontName(DefaultFontName, DefaultFontSize),
			};

            this.comboBox.SelectionChanged += SelectionChanged;

			this.popUpButton = new NSPopUpButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				StringValue = String.Empty,
				ControlSize = NSControlSize.Small,
				Font = NSFont.FromFontName (DefaultFontName, DefaultFontSize),
			};

			popupButtonList = new NSMenu ();
			popUpButton.Menu = popupButtonList;

			popUpButton.Activated += (o, e) => {
				EditorViewModel.ValueName = (o as NSPopUpButton).Title;
			};

			UpdateTheme ();
		}

		public override NSView FirstKeyView => firstKeyView;
		public override NSView LastKeyView => lastKeyView;
		public List<NSButton> CombinableList => this.combinableList;

		protected PredefinedValuesViewModel<T> EditorViewModel => (PredefinedValuesViewModel<T>)ViewModel;

		readonly NSPopUpButton popUpButton;
		NSMenu popupButtonList;

		protected override void HandleErrorsChanged (object sender, DataErrorsChangedEventArgs e)
		{
			UpdateErrorsDisplayed (ViewModel.GetErrors (e.PropertyName));
		}

		protected override void SetEnabled ()
		{
			if (EditorViewModel.IsCombinable) {
				foreach (var item in combinableList) {
					item.Enabled = ViewModel.Property.CanWrite;
				}
			} else {
				this.comboBox.Editable = ViewModel.Property.CanWrite;
			}
		}

		protected override void UpdateErrorsDisplayed (IEnumerable errors)
		{
			if (EditorViewModel.IsCombinable) {
				foreach (var item in combinableList) {
					if (ViewModel.HasErrors) {
						if (item.RespondsToSelector (new Selector (setBezelColorSelector))) {
							item.BezelColor = NSColor.Red;
						}
					} else {
						if (item.RespondsToSelector (new Selector (setBezelColorSelector))) {
							item.BezelColor = NSColor.Clear;
						}
					}
				}
			} else {
				if (ViewModel.HasErrors) {
					this.comboBox.BackgroundColor = NSColor.Red;
				} else {
					comboBox.BackgroundColor = NSColor.Clear;
				}
			}

			Debug.WriteLine ("Your input triggered an error:");
			foreach (var error in errors) {
				Debug.WriteLine (error.ToString () + "\n");
			}

			SetEnabled ();
		}

		protected override void OnViewModelChanged (PropertyViewModel oldModel)
		{
			if (!dataPopulated) {
				if (EditorViewModel.IsCombinable) {
					combinableList.Clear ();

					var top = 0;
					foreach (var item in EditorViewModel.PossibleValues) {
						var BooleanEditor = new NSButton (new CGRect (0, top, 200, 24)) { TranslatesAutoresizingMaskIntoConstraints = false };
						BooleanEditor.SetButtonType (NSButtonType.Switch);
						BooleanEditor.Title = item;
						BooleanEditor.Activated += SelectionChanged;

						AddSubview (BooleanEditor);
						combinableList.Add (BooleanEditor);
						top += 24;
					}

					// Set our new RowHeight
					RowHeight = top;
					firstKeyView = combinableList.First ();
					lastKeyView = combinableList.Last ();
				} else {
					this.comboBox.RemoveAll ();

					// Once the VM is loaded we need a one time population
					foreach (var item in EditorViewModel.PossibleValues) {
						this.comboBox.Add (new NSString (item));
					}

					AddSubview (this.comboBox);

					this.DoConstraints (new[] {
						comboBox.ConstraintTo (this, (cb, c) => cb.Width == c.Width - 35),
						comboBox.ConstraintTo (this, (cb, c) => cb.Height == DefaultControlHeight),
						comboBox.ConstraintTo (this, (cb, c) => cb.Left == cb.Left + 4),
						comboBox.ConstraintTo (this, (cb, c) => cb.Top == cb.Top + 0),
					});

					firstKeyView = this.comboBox;
					lastKeyView = this.comboBox;
				}

				dataPopulated = true;
			}

			base.OnViewModelChanged (oldModel);
		}

		void SelectionChanged (object sender, EventArgs e)
		{
			if (EditorViewModel.IsCombinable) {
				var tickedButtons = combinableList.Where (y => y.State == NSCellStateValue.On).Select (x => x.Title).ToList ();

				EditorViewModel.ValueList = tickedButtons;
			} else {
				EditorViewModel.ValueName = comboBox.SelectedValue.ToString ();
			}

			dataPopulated = false;
		}

		protected override void UpdateValue ()
		{
			if (EditorViewModel.IsCombinable) {
				foreach (var item in combinableList) {
					if (EditorViewModel.ValueList.Count () > 0) {
						item.State = EditorViewModel.ValueList.Contains (item.Title) ? NSCellStateValue.On : NSCellStateValue.Off;
					}
					else {
						item.State = NSCellStateValue.Off;
					}
				}
			} else {
				this.comboBox.StringValue = EditorViewModel.ValueName ?? String.Empty;
			}
		}

		protected override void UpdateAccessibilityValues ()
		{
			if (EditorViewModel.IsConstrainedToPredefined) {
				popUpButton.AccessibilityEnabled = popUpButton.Enabled;
				popUpButton.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityCombobox, ViewModel.Property.Name);
			} else {
				comboBox.AccessibilityEnabled = comboBox.Enabled;
				comboBox.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityCombobox, ViewModel.Property.Name);
			}
		}
	}
}
