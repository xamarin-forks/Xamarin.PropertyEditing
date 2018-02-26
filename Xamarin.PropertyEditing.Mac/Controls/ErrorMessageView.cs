using System;
using System.Collections;
using AppKit;
using CoreGraphics;
using Xamarin.PropertyEditing.Mac.Resources;

namespace Xamarin.PropertyEditing.Mac
{
	internal class ErrorMessageView : BasePopOverControl
	{
		NSTextField ErrorMessages;

		public ErrorMessageView (IEnumerable errors) : base (LocalizationResources.PropertyErrorsLabel, "action-warning-16")
		{
			if (errors == null)
				throw new ArgumentNullException ("errors");

			Frame = new CGRect (CGPoint.Empty, new CGSize (320, 200));

			ErrorMessages = new NSTextField {
				TranslatesAutoresizingMaskIntoConstraints = false,
				BackgroundColor = NSColor.Clear,
				Editable = false,
			};

			foreach (var error in errors) {
				ErrorMessages.StringValue += error + "\n";
			}

			AddSubview (ErrorMessages);

			this.DoConstraints (new[] {ErrorMessages.ConstraintTo (this, (s, c) => s.Top == c.Top + 50),
				ErrorMessages.ConstraintTo (this, (s, c) => s.Width == c.Width - 5),
				ErrorMessages.ConstraintTo (this, (s, c) => s.Height == c.Height - 40),
			});
		}
	}
}
