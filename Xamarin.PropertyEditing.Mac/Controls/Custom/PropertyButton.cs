using System;
using System.Windows.Input;
using AppKit;
using CoreGraphics;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	public class PropertyButton : NSButton
	{
		NSMenu popUpContextMenu;

		PropertyViewModel viewModel;
		internal PropertyViewModel ViewModel
		{
			get { return viewModel; }
			set {
				if (viewModel != null)
					viewModel.PropertyChanged -= OnPropertyChanged;

				viewModel = value;
				viewModel.PropertyChanged += OnPropertyChanged;

				// No point showing myself if you can't do anything with me.
				Hidden = !viewModel.Property.CanWrite;

				ValueSourceChanged (viewModel.ValueSource);
			}
		}

		public PropertyButton ()
		{
			AlternateImage = NSImage.ImageNamed ("property-button-default-mac-active-10");
			Cell = new NSButtonCell {
				HighlightsBy = 1,
			};
			Bordered = false;
			Enabled = true;
			Image = NSImage.ImageNamed ("property-button-default-mac-10");
			ImageScaling = NSImageScale.AxesIndependently;
			Title = string.Empty;
			ToolTip = Properties.Resources.Default;
			TranslatesAutoresizingMaskIntoConstraints = false;

			Activated += (sender, e) => {
				if (this.popUpContextMenu == null) {
					this.popUpContextMenu = new NSMenu ();

					// TODO If we add more menu items consider making the Label/Command a dictionary that we can iterate over to populate everything.
					this.popUpContextMenu.AddItem (new CommandMenuItem (Properties.Resources.Reset, viewModel.ClearValueCommand));
				}

				var menuOrigin = this.Superview.ConvertPointToView (new CGPoint (Frame.Location.X - 1, Frame.Location.Y + Frame.Size.Height + 4), null);

				var popupMenuEvent = NSEvent.MouseEvent (NSEventType.LeftMouseDown, menuOrigin, (NSEventModifierMask)0x100, 0, this.Window.WindowNumber, this.Window.GraphicsContext, 0, 1, 1);

				NSMenu.PopUpContextMenu (popUpContextMenu, popupMenuEvent, this);
			};
		}

		private void ValueSourceChanged (ValueSource valueSource)
		{
			switch (valueSource) {
				case ValueSource.Binding:
					AlternateImage = NSImage.ImageNamed ("property-button-bound-mac-active-10");
					Image = NSImage.ImageNamed ("property-button-bound-mac-10");
					ToolTip = Properties.Resources.Binding;
					break;

				case ValueSource.Default:
					AlternateImage = NSImage.ImageNamed ("property-button-default-mac-active-10");
					Image = NSImage.ImageNamed ("property-button-default-mac-10");
					ToolTip = Properties.Resources.Default;
					return;

				case ValueSource.Local:
					AlternateImage = NSImage.ImageNamed ("property-button-local-mac-active-10");
					Image = NSImage.ImageNamed ("property-button-local-mac-10");
					ToolTip = Properties.Resources.Local;
					break;

				case ValueSource.Inherited:
					AlternateImage = NSImage.ImageNamed ("property-button-inherited-mac-active-10");
					Image = NSImage.ImageNamed ("property-button-inherited-mac-10");
					ToolTip = Properties.Resources.Inherited;
					break;
				case ValueSource.DefaultStyle:
				case ValueSource.Style:
				case ValueSource.Resource:
					ToolTip = null;
					break;
			}
		}

		private void OnPropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ValueSource") {
				ValueSourceChanged (viewModel.ValueSource);
			}
		}
	}
}
