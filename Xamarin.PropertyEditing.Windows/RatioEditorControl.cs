using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Windows
{
	class RatioEditorControl : PropertyEditorControl
	{
		private RatioViewModel ViewModel => DataContext as RatioViewModel;
		TextBoxEx ratioTextBox;

		protected override void OnKeyUp (KeyEventArgs e)
		{
			ratioTextBox = e.Source as TextBoxEx;
			switch (e.Key) {
			case Key.Up:
				// Increment Value
				ValueChanged (ratioTextBox.Text, ratioTextBox.CaretIndex, ratioTextBox.SelectionLength, 1);
				break;
			case Key.Down:
				// Decrement Value
				ValueChanged (ratioTextBox.Text, ratioTextBox.CaretIndex, ratioTextBox.SelectionLength, -1);
				break;
			default:
				break;
			}
			base.OnKeyUp (e);
		}

		void ValueChanged (string inputedText, int caretPosition, int selectionLength, double incrementValue)
		{
			ViewModel.ValueChanged (inputedText, caretPosition, selectionLength, incrementValue);
			ratioTextBox.CaretIndex = caretPosition;
			ratioTextBox.SelectionLength = selectionLength;
		}
	}
}
