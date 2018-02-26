using System;
using AppKit;
using CoreGraphics;

namespace Xamarin.PropertyEditing.Mac
{
	internal class BasePopOverControl : NSView
	{
		const int DefaultIconButtonSize = 24;
		public BasePopOverControl (string title, string imageNamed)
		{
			if (title == null)
				throw new ArgumentNullException ("title");

			if (imageNamed == null)
				throw new ArgumentNullException ("imageNamed");

			Frame = new CGRect (CGPoint.Empty, new CGSize (250, 60));

			var iconView = new NSButton (new CGRect (5, Frame.Height - 25, DefaultIconButtonSize, DefaultIconButtonSize)) {
				Bordered = false,
				Image = NSImage.ImageNamed (imageNamed),
				Title = string.Empty,
			};

			AddSubview (iconView);

			var viewTitle = new UnfocusableTextField (new CGRect (30, Frame.Height - 26, 120, 24), title);

			AddSubview (viewTitle);

			this.DoConstraints (new[] {
				iconView.ConstraintTo (this, (iv, c) => iv.Top == c.Top + 10),
				iconView.ConstraintTo (this, (iv, c) => iv.Left == c.Left + 10),
				iconView.ConstraintTo (this, (iv, c) => iv.Width == DefaultIconButtonSize),
				iconView.ConstraintTo (this, (iv, c) => iv.Height == DefaultIconButtonSize),
				viewTitle.ConstraintTo (this, (vt, c) => vt.Top == c.Top + 15),
				viewTitle.ConstraintTo (iconView, (vt, iv) => vt.Left == iv.Left + iv.Width + 10),
				viewTitle.ConstraintTo (this, (vt, c) => vt.Width == 120),
				viewTitle.ConstraintTo (this, (vt, c) => vt.Height == 24),
			});

			this.Appearance = PropertyEditorPanel.ThemeManager.CurrentAppearance;
		}
	}
}
