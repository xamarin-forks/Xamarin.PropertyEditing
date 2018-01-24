using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	internal class BasePopOverDataContextControl : BasePopOverControl
	{
		private IEnumerable<object> targets;
		internal IEnumerable<object> Targets => this.targets;

		private IPropertyInfo property;
		internal IPropertyInfo Property => this.property;

		public BasePopOverDataContextControl (PropertyViewModel viewModel, string title, string imageNamed) : base (title, imageNamed)
		{
			targets = viewModel.Editors.Select (ed => ed.Target);
			property = viewModel.Property;
		}
	}
}
