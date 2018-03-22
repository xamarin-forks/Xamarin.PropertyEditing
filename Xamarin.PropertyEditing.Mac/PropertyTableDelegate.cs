﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AppKit;
using Foundation;
using Xamarin.PropertyEditing.Drawing;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	internal class PropertyTableDelegate
		: NSOutlineViewDelegate
	{
		bool goldenRatioApplied = false;

		public PropertyTableDelegate (PropertyTableDataSource datasource)
		{
			this.dataSource = datasource;
		}

		public void UpdateExpansions (NSOutlineView outlineView)
		{
			this.isExpanding = true;

			if (!String.IsNullOrWhiteSpace (this.dataSource.DataContext.FilterText)) {
				outlineView.ExpandItem (null, true);
			} else {
				foreach (IGrouping<string, EditorViewModel> g in this.dataSource.DataContext.ArrangedEditors) {
					NSObject item;
					if (!this.dataSource.TryGetFacade (g, out item))
						continue;

					if (this.dataSource.DataContext.GetIsExpanded (g.Key))
						outlineView.ExpandItem (item);
					else
						outlineView.CollapseItem (item);
				}
			}
			this.isExpanding = false;
		}

		// the table is looking for this method, picks it up automagically
		public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			var facade = (NSObjectFacade)item;
			var vm = facade.Target as PropertyViewModel;
			var group = facade.Target as IGroupingList<string, EditorViewModel>;
			string cellIdentifier = (group == null) ? vm.GetType ().Name : group.Key;

			// Let's make the columns look pretty
			if (!goldenRatioApplied) {
				int middleColumnWidth = 5;
				nfloat rightColumnWidth = (outlineView.Frame.Width - middleColumnWidth) / 1.618f;
				nfloat leftColumnWidth = outlineView.Frame.Width - rightColumnWidth - middleColumnWidth;
				outlineView.TableColumns ()[0].Width = leftColumnWidth;
				outlineView.TableColumns ()[1].Width = rightColumnWidth;
				goldenRatioApplied = true;
			}

			// Setup view based on the column
			switch (tableColumn.Identifier) {
				case PropertyEditorPanel.PropertyListColId:
					var view = (UnfocusableTextField)outlineView.MakeView ("label", this);
					if (view == null) {
						view = new UnfocusableTextField {
							Identifier = "label",
							Alignment = NSTextAlignment.Right,
						};
					}

					view.StringValue = ((group == null) ? vm.Property.Name + ":" : group.Key) ?? String.Empty;

					// Set tooltips only for truncated strings
					var stringWidth = view.AttributedStringValue.Size.Width + 30;
					if (stringWidth > tableColumn.Width) {
						view.ToolTip = vm.Property.Name;
					}

					return view;

				case PropertyEditorPanel.PropertyEditorColId:
					if (vm == null)
						return null;

					var editor = (PropertyEditorControl)outlineView.MakeView (cellIdentifier + "edits", this);
					if (editor == null) {
						editor = GetEditor (vm, outlineView);
						// If still null we have no editor yet.
						if (editor == null)
							return new NSView ();
					}

					// we must reset these every time, as the view may have been reused
					editor.TableRow = outlineView.RowForItem (item);

					// Force a row update due to new height, but only when we are non-default
					if (editor.TriggerRowChange)
						outlineView.NoteHeightOfRowsWithIndexesChanged (new NSIndexSet (editor.TableRow));

					return editor;
			}

			throw new Exception ("Unknown column identifier: " + tableColumn.Identifier);
		}

		PropertyEditorControl GetEditor (EditorViewModel vm, NSOutlineView outlineView)
		{
			Type[] genericArgs = null;
			Type controlType;
			Type propertyType = vm.GetType ();
			if (!ViewModelTypes.TryGetValue (propertyType, out controlType)) {
				if (propertyType.IsConstructedGenericType) {
					genericArgs = propertyType.GetGenericArguments ();
					propertyType = propertyType.GetGenericTypeDefinition ();
					ViewModelTypes.TryGetValue (propertyType, out controlType);
				}
			}
			if (controlType == null)
				return null;

			if (controlType.IsGenericTypeDefinition) {
				if (genericArgs == null)
					genericArgs = propertyType.GetGenericArguments ();
				controlType = controlType.MakeGenericType (genericArgs);
			}

			return SetUpEditor (controlType, vm, outlineView);
		}

		public override bool ShouldSelectItem (NSOutlineView outlineView, NSObject item)
		{
			return (!(item is NSObjectFacade) || !(((NSObjectFacade)item).Target is IGroupingList<string, EditorViewModel>));
		}

		public override void ItemDidExpand (NSNotification notification)
		{
			if (this.isExpanding)
				return;

			NSObjectFacade facade = notification.UserInfo.Values[0] as NSObjectFacade;
			var group = facade.Target as IGroupingList<string, EditorViewModel>;
			if (group != null)
				this.dataSource.DataContext.SetIsExpanded (group.Key, isExpanded: true);
		}

		public override void ItemDidCollapse (NSNotification notification)
		{
			if (this.isExpanding)
				return;

			NSObjectFacade facade = notification.UserInfo.Values[0] as NSObjectFacade;
			var group = facade.Target as IGroupingList<string, EditorViewModel>;
			if (group != null)
				this.dataSource.DataContext.SetIsExpanded (group.Key, isExpanded: false);
		}

		public override nfloat GetRowHeight (NSOutlineView outlineView, NSObject item)
		{
			var facade = (NSObjectFacade)item;
			var group = facade.Target as IGroupingList<string, EditorViewModel>;
			if (group != null) {
				return 30;
			}

			var vm = (EditorViewModel)facade.Target;
			var editor = (PropertyEditorControl)outlineView.MakeView (vm.GetType ().Name + "edits", this);
			if (editor == null) {
				editor = GetEditor (vm, outlineView);
				// If still null we have no editor yet.
				if (editor == null) {
					return 22;
				}
			}
			return editor.RowHeight;
		}

		private PropertyTableDataSource dataSource;
		private bool isExpanding;

		// set up the editor based on the type of view model
		private PropertyEditorControl SetUpEditor (Type controlType, EditorViewModel property, NSOutlineView outline)
		{
			var view = (PropertyEditorControl)Activator.CreateInstance (controlType);
			view.Identifier = property.GetType ().Name;
			view.TableView = outline;
			view.ViewModel = (PropertyViewModel)property;

			return view;
		}

		private static readonly Dictionary<Type, Type> ViewModelTypes = new Dictionary<Type, Type> {
			{typeof (StringPropertyViewModel), typeof (StringEditorControl)},
			{typeof (NumericPropertyViewModel<>), typeof (NumericEditorControl<>)},
			{typeof (PropertyViewModel<bool?>), typeof (BooleanEditorControl)},
			{typeof (PredefinedValuesViewModel<>), typeof(PredefinedValuesEditor<>)},
			{typeof (CombinablePropertyViewModel<>), typeof(CombinablePropertyEditor<>)},
			{typeof (PropertyViewModel<CoreGraphics.CGPoint>), typeof (CGPointEditorControl)},
			{typeof (PropertyViewModel<CoreGraphics.CGRect>), typeof (CGRectEditorControl)},
			{typeof (PropertyViewModel<CoreGraphics.CGSize>), typeof (CGSizeEditorControl)},
			{typeof (PointPropertyViewModel), typeof (CommonPointEditorControl) },
			{typeof (RectanglePropertyViewModel), typeof (CommonRectangleEditorControl) },
			{typeof (SizePropertyViewModel), typeof (CommonSizeEditorControl) },
			{typeof (PropertyViewModel<Point>), typeof (SystemPointEditorControl)},
			{typeof (PropertyViewModel<Size>), typeof (SystemSizeEditorControl)},
			{typeof (PropertyViewModel<Rectangle>), typeof (SystemRectangleEditorControl)}
		};
	}
}
