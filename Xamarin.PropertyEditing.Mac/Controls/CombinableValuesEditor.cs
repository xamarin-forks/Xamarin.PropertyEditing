using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using Foundation;
using AppKit;
using CoreGraphics;

using Xamarin.PropertyEditing.ViewModels;
using Xamarin.PropertyEditing.Mac.Resources;

namespace Xamarin.PropertyEditing.Mac
{
	internal class CombinableValuesEditor<T> : PropertyEditorControl<T>
		where T : struct
	{
		public CombinableValuesEditor ()
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

			this.comboBox.SelectionChanged += (sender, e) => {
				//EditorViewModel.Value = comboBox.SelectedValue.ToString ();
			};

			this.popUpButton = new NSPopUpButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				StringValue = String.Empty,
				ControlSize = NSControlSize.Small,
				Font = NSFont.FromFontName (DefaultFontName, DefaultFontSize),
			};

			popupButtonList = new NSMenu ();
			popUpButton.Menu = popupButtonList;

			popUpButton.Activated += (o, e) => {
				//EditorViewModel.Value = (o as NSPopUpButton).Title;
			};

			UpdateTheme ();
		}

		public override NSView FirstKeyView => firstKeyView;
		public override NSView LastKeyView => lastKeyView;

		protected CombinablePropertyViewModel<T> EditorViewModel => (CombinablePropertyViewModel<T>)ViewModel;

		readonly NSComboBox comboBox;
		readonly NSPopUpButton popUpButton;
		NSMenu popupButtonList;

		bool dataPopulated;
		NSView firstKeyView;
		NSView lastKeyView;

		protected override void HandleErrorsChanged (object sender, DataErrorsChangedEventArgs e)
		{
			UpdateErrorsDisplayed (ViewModel.GetErrors (e.PropertyName));
		}

		protected override void SetEnabled ()
		{
			if (EditorViewModel.IsAvailable) {
				this.popUpButton.Enabled = ViewModel.Property.CanWrite;
			} else {
				this.comboBox.Enabled = ViewModel.Property.CanWrite;
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
			if (!dataPopulated) {
				if (EditorViewModel.IsAvailable) {
					this.popupButtonList.RemoveAllItems ();
					foreach (var item in EditorViewModel.Choices) {
						popupButtonList.AddItem (new NSMenuItem (item.Name));
					}

					AddSubview (this.popUpButton);

					this.DoConstraints (new[] {
						popUpButton.ConstraintTo (this, (pub, c) => pub.Width == c.Width - 34),
						popUpButton.ConstraintTo (this, (pub, c) => pub.Height == DefaultControlHeight + 1),
						popUpButton.ConstraintTo (this, (pub, c) => pub.Left == pub.Left + 4),
						popUpButton.ConstraintTo (this, (pub, c) => pub.Top == pub.Top + 0),
					});

					firstKeyView = this.popUpButton;
					lastKeyView = this.popUpButton;
				} else {
					this.comboBox.RemoveAll ();

					// Once the VM is loaded we need a one time population
					foreach (var item in EditorViewModel.Choices) {
						this.comboBox.Add (new NSString (item.Name));
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

		protected override void UpdateValue ()
		{
			if (EditorViewModel.IsAvailable) {
				//this.popUpButton.Title = EditorViewModel.Value ?? String.Empty;
			} else {
				//this.comboBox.StringValue = EditorViewModel.Value ?? String.Empty;
			}
		}

		protected override void UpdateAccessibilityValues ()
		{
			if (EditorViewModel.IsAvailable) {
				popUpButton.AccessibilityEnabled = popUpButton.Enabled;
				popUpButton.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityCombobox, ViewModel.Property.Name);
			} else {
				comboBox.AccessibilityEnabled = comboBox.Enabled;
				comboBox.AccessibilityTitle = string.Format (LocalizationResources.AccessibilityCombobox, ViewModel.Property.Name);
			}
		}
	}
}
