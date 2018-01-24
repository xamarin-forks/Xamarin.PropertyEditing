using System;
using System.Windows.Input;
using AppKit;

namespace Xamarin.PropertyEditing.Mac
{
	public class CommandMenuItem : NSMenuItem
	{
		ICommand command;

		public ICommand Command {
			get { return command; }
			set {
				if (this.command != null)
					this.command.CanExecuteChanged -= CanExecuteChanged;

				this.command = value;
				this.command.CanExecuteChanged += CanExecuteChanged;
			}
		}

		public CommandMenuItem (string title) : base (title)
		{
			HookUpCommandEvents ();
		}

		public CommandMenuItem (string title, ICommand command) : this (title)
		{
			this.Command = command;

			HookUpCommandEvents ();
		}

		private void HookUpCommandEvents ()
		{
			Activated += (object sender, EventArgs e) => {
				if (this.command != null)
					this.command.Execute (null);
			};

			ValidateMenuItem = ValidatePropertyMenuItem;
		}

		private bool ValidatePropertyMenuItem (NSMenuItem menuItem)
		{
			if (this.command != null)
				return this.command.CanExecute (null);

			return false;
		}

		private void CanExecuteChanged (object sender, EventArgs e)
		{
			var cmd = sender as ICommand;
			if(cmd != null)
				this.Enabled = cmd.CanExecute (null);
		}
	}
}
