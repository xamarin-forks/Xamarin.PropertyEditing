using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AppKit;
using CoreGraphics;
using Foundation;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	internal class ResourceTableDataSource
		: NSTableViewDataSource
	{
		private ResourceSelectorViewModel viewModel;
		internal ResourceTableDataSource (ResourceSelectorViewModel propertyViewModel)
		{
			if (propertyViewModel == null)
				throw new ArgumentNullException (nameof (propertyViewModel));

			this.viewModel = propertyViewModel;
		}

		public ResourceSelectorViewModel DataContext => this.viewModel;

		public override nint GetRowCount (NSTableView tableView)
		{
			return 4;//this.viewModel.Resources.Count ();
		}
	}

	internal class ResourceTableDelegate
		: NSTableViewDelegate
	{
		private ResourceTableDataSource datasource;

		public event EventHandler ResourceSelected;

		public ResourceTableDelegate (ResourceTableDataSource datasource)
		{
			this.datasource = datasource;
		}

		// the table is looking for this method, picks it up automagically
		public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
			// Setup view based on the column
			switch (tableColumn.Identifier) {
				case ResourceSelectorPanel.ResourceImageColId:
					var iconView = (UnfocusableTextField)tableView.MakeView ("icon", this);
					if (iconView == null) {
						iconView = new UnfocusableTextField {
							Identifier = "icon",
							Alignment = NSTextAlignment.Right,
							StringValue = "Icon"
						};
					}
					return iconView;
				case ResourceSelectorPanel.ResourceTypeColId:
					var typeView = (UnfocusableTextField)tableView.MakeView ("type", this);
					if (typeView == null) {
						typeView = new UnfocusableTextField {
							Identifier = "type",
							Alignment = NSTextAlignment.Right,
							StringValue = "Type"
						};
					}
					return typeView;
				case ResourceSelectorPanel.ResourceNameColId:
					var nameView = (UnfocusableTextField)tableView.MakeView ("name", this);
					if (nameView == null) {
						nameView = new UnfocusableTextField {
							Identifier = "name",
							Alignment = NSTextAlignment.Left,
							StringValue = "Name"
						};
					}
					return nameView;
				case ResourceSelectorPanel.ResourceValueColId:
					var valueView = tableView.MakeView ("value", this);

					return valueView;
				default:
					return base.GetViewForItem (tableView, tableColumn, row);
			}
		}

        public override void SelectionDidChange(NSNotification notification)
        {
			ResourceSelected?.Invoke (this, EventArgs.Empty);
        }
    }

	internal class FirstResponderTableView : NSTableView
	{
		[Export ("validateProposedFirstResponder:forEvent:")]
		public bool validateProposedFirstResponder (NSResponder responder, NSEvent ev)
		{
			return true;
		}
	}

	internal class ResourceSelectorPanel : NSView
	{
		internal const string ResourceImageColId = "ResourceImage";
		internal const string ResourceTypeColId = "ResourceType";
		internal const string ResourceNameColId = "ResourceName";
		internal const string ResourceValueColId = "ResourceValue";

		private NSTableView resourceTable;
		private ResourceTableDataSource dataSource;

		private ResourceSelectorViewModel viewModel;
		public ResourceSelectorViewModel ViewModel => this.viewModel;

		NSProgressIndicator progressIndicator;

		public event EventHandler ResourceSelected;

		public ResourceSelectorPanel (ResourceSelectorViewModel viewModel)
		{
			this.viewModel = viewModel;
			this.viewModel.PropertyChanged += OnPropertyChanged;
			Initialize ();
		}

		private void OnPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (viewModel.IsLoading)) {
				if (viewModel.IsLoading) {
					AddSubview (progressIndicator);
					progressIndicator.StartAnimation (null);
				} else {
					progressIndicator.StopAnimation (null);
					progressIndicator.RemoveFromSuperview ();
				}
			}
		}

		private void Initialize ()
		{
			Frame = new CGRect (5, 5, 635, 375);

			var FrameHeightHalf = Frame.Height / 2;
			var FrameWidthThird = Frame.Width / 3;

			AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;

			progressIndicator = new NSProgressIndicator (new CGRect (5, FrameHeightHalf, 24, 24)) {
				Style = NSProgressIndicatorStyle.Spinning,
			};
			AddSubview (progressIndicator);
			progressIndicator.StartAnimation (null);

			resourceTable = new FirstResponderTableView {
				RefusesFirstResponder = true,
				AutoresizingMask = NSViewResizingMask.WidthSizable,
				SelectionHighlightStyle = NSTableViewSelectionHighlightStyle.Regular,
				//HeaderView = null,
			};

			dataSource = new ResourceTableDataSource (viewModel);
			resourceTable.Delegate = new ResourceTableDelegate (dataSource);
			resourceTable.DataSource = dataSource;

			NSTableColumn resourceImages = new NSTableColumn (ResourceImageColId) { Title = "Icon" };
			resourceImages.Width = 16;
			resourceTable.AddColumn (resourceImages);

			NSTableColumn resourceTypes = new NSTableColumn (ResourceTypeColId) { Title = "Type" };
			resourceTypes.Width = 80;
			resourceTable.AddColumn (resourceTypes);

			NSTableColumn resourceName = new NSTableColumn (ResourceTypeColId) { Title = "Name" };
			resourceName.Width = 100;
			resourceTable.AddColumn (resourceName);

			NSTableColumn resourceValue = new NSTableColumn (ResourceValueColId) { Title = "Value" };
			resourceValue.Width = 100;
			resourceTable.AddColumn (resourceValue);

			// create a table view and a scroll view
			var tableContainer = new NSScrollView (new CGRect (10, Frame.Height - 210, resourceImages.Width + resourceTypes.Width + resourceName.Width, Frame.Height - 55)) {
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			// add the panel to the window
			tableContainer.DocumentView = resourceTable;
			AddSubview (tableContainer);

			this.DoConstraints (new NSLayoutConstraint[] {
				/*tableContainer.ConstraintTo (this, (t, c) => t.Top == c.Top + 30),
				tableContainer.ConstraintTo (this, (t, c) => t.Width == c.Width - 20),
				tableContainer.ConstraintTo (this, (t, c) => t.Height == c.Height - 40),*/
				//progressIndicator.ConstraintTo (this, (t, c) => t.Left == c.Width / 3),
				//progressIndicator.ConstraintTo (this, (t, c) => t.Top == c.Height / 2),
				progressIndicator.ConstraintTo (this, (t, c) => t.Width == 24),
				progressIndicator.ConstraintTo (this, (t, c) => t.Height == 24),
			});

			this.resourceTable.ReloadData ();
		}
	}

	internal class ResourceSelectorView : BasePopOverDataContextControl
	{
		NSTextField noPreviewAvailable;
		NSSearchField searchResources;
		NSSegmentedControl segmentedControl;
		ResourceSelectorPanel resourceSelectorPanel;

		public Resource SelectedResource { get; internal set; }

		private bool showPreview;
		public bool ShowPreview {
			get { return showPreview; }
			set {
				showPreview = value;
				if (showPreview) {
					Frame = new CGRect (CGPoint.Empty, new CGSize (640, 380));
				} else {
					Frame = new CGRect (CGPoint.Empty, new CGSize (426, 380));
				}
			}
		}

		public ResourceSelectorView (PropertyViewModel viewModel) : base (viewModel, Properties.Resources.ResourceEllipsis, "property-button-inherited-mac-active-10")
		{
			Initialize (viewModel);
		}

		private void Initialize (PropertyViewModel viewModel)
		{
			ShowPreview = true;

			var FrameWidthThird = Frame.Width / 3;
			var FrameWidthHalf = Frame.Width / 2;
			var FrameHeightHalf = Frame.Height / 2;

			NSControlSize controlSize = NSControlSize.Small;

			searchResources = new NSSearchField (new CGRect (FrameWidthThird, Frame.Height - 25, (Frame.Width - (FrameWidthThird)) - 10, 30)) {
				ControlSize = controlSize,
				Font = NSFont.FromFontName (PropertyEditorControl.DefaultFontName, PropertyEditorControl.DefaultFontSize),
				PlaceholderString = Properties.Resources.SearchResourcesTitle,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			searchResources.Changed += OnSearchResourcesChanged;

			AddSubview (searchResources);

			noPreviewAvailable = new UnfocusableTextField (new CGRect (FrameWidthThird * 2, FrameHeightHalf, FrameWidthThird, 40), Properties.Resources.NoPreviewAvailable) {
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			AddSubview (noPreviewAvailable);

			resourceSelectorPanel = new ResourceSelectorPanel (new ResourceSelectorViewModel (viewModel.ResourceProvider, viewModel.Editors.Select (ed => ed.Target), viewModel.Property));
			AddSubview (resourceSelectorPanel);


			segmentedControl = NSSegmentedControl.FromLabels (new string[] { Properties.Resources.AllResources, Properties.Resources.Local, Properties.Resources.Shared }, NSSegmentSwitchTracking.SelectOne, () => {
				//Switch Resource Types
				switch (segmentedControl.SelectedSegment) {
					case 0:
						resourceSelectorPanel.ViewModel.ShowBothResourceTypes = true;
						break;
					case 1:
						resourceSelectorPanel.ViewModel.ShowOnlyLocalResources = true;
						break;
					case 2:
						resourceSelectorPanel.ViewModel.ShowOnlySystemResources = true;
						break;
				}

			});
			segmentedControl.Frame = new CGRect ((FrameWidthThird - (segmentedControl.Bounds.Width) / 2), 5, (Frame.Width - (FrameWidthThird)) - 10, 24);
			segmentedControl.Font = NSFont.FromFontName (PropertyEditorControl.DefaultFontName, PropertyEditorControl.DefaultFontSize);
			segmentedControl.TranslatesAutoresizingMaskIntoConstraints = false;
			segmentedControl.SetSelected (true, 0);
			//resourceSelectorPanel.ViewModel.ShowBothResourceTypes = true;

			AddSubview (segmentedControl);

			var showPreviewImage = new NSButton (new CGRect (Frame.Width - 35, 10, 24, 24)) {
				Bordered = false,
				ControlSize = controlSize,
				Image = NSImage.ImageNamed (NSImageName.QuickLookTemplate),
				Title = string.Empty,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			showPreviewImage.Activated += (o, e) => {
				ShowPreview = !ShowPreview;
			};

			AddSubview (showPreviewImage);

			/* this.DoConstraints (new[] {
				resourceImage.ConstraintTo( this, (ri, c) => ri.Left == 5),
				resourceImage.ConstraintTo( this, (ri, c) => ri.Top == 5),
				resourceImage.ConstraintTo( this, (ri, c) => ri.Width == 24),
				resourceImage.ConstraintTo( this, (ri, c) => ri.Height == 24),

				//noPreviewAvailable.ConstraintTo (this, (s, c) => s.Width == 240),
				//noPreviewAvailable.ConstraintTo (this, (s, c) => s.Height == c.Height - 5),
			});*/
			//resourceTable.ReloadData ();
		}

		private void OnSearchResourcesChanged (object sender, EventArgs e)
		{
			resourceSelectorPanel.ViewModel.FilterText = searchResources.Cell.Title;
		}
	}
}
