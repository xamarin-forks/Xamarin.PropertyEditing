﻿using System;
using System.Collections;
using AppKit;
using CoreGraphics;

namespace Xamarin.PropertyEditing.Mac
{
	internal class ErrorMessageView : BasePopOverControl
	{
		NSTextField ErrorMessages; public ErrorMessageView (IEnumerable errors) : base ("Errors", "action-warning-16")
		{
			if (errors == null)
				throw new ArgumentNullException (nameof (errors));

			Frame = new CGRect (CGPoint.Empty, new CGSize (240, 480));

			ErrorMessages = new NSTextField {
				BackgroundColor = NSColor.Clear,
				Editable = false,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			foreach (var error in errors) {
				ErrorMessages.StringValue += error + "\n";
			}

			AddSubview (ErrorMessages); this.DoConstraints (new[] {
				ErrorMessages.ConstraintTo (this, (s, c) => s.Top == c.Top + 35),
				ErrorMessages.ConstraintTo (this, (s, c) => s.Left == c.Left + 5),
				ErrorMessages.ConstraintTo (this, (s, c) => s.Width == c.Width - 10),
				ErrorMessages.ConstraintTo (this, (s, c) => s.Height == c.Height - 40),
			});
		}
	}
}
