using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using CoreGraphics;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	internal class CustomExpressionView : BasePopOverViewModelControl
	{
		public CustomExpressionView (PropertyViewModel viewModel) : base (viewModel,  Properties.Resources.CustomExpression, "property-button-default-mac-active-10")
		{
			Frame = new CGRect (CGPoint.Empty, new CGSize (250, 60));

			var customExpressionField = new NSTextField {
				StringValue = string.Empty,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};

			customExpressionField.Changed += (sender, e) => {
				//ViewModel.CustomExpression = customExpressionField.StringValue;
			};

			AddSubview (customExpressionField);

			this.DoConstraints (new[] {
				customExpressionField.ConstraintTo (this, (s, c) => s.Top == c.Top + 30),
				customExpressionField.ConstraintTo (this, (s, c) => s.Left == c.Left + 30),
				customExpressionField.ConstraintTo (this, (s, c) => s.Width == c.Width - 37),
				customExpressionField.ConstraintTo (this, (s, c) => s.Height == 24),
			});

			// TODO make customExpressionField accept focus by default.
			customExpressionField.BecomeFirstResponder ();
		}
	}
}
