using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xamarin.PropertyEditing.Resources;
using System.Threading.Tasks;

namespace Xamarin.PropertyEditing.ViewModels
{
	internal class PredefinedValuesViewModel<TValue>
		: PropertyViewModel<TValue>
	{
		public PredefinedValuesViewModel (TargetPlatform platform, IPropertyInfo property, IEnumerable<IObjectEditor> editors)
			: base (platform, property, editors)
		{
			this.predefinedValues = property as IHavePredefinedValues<TValue>;
			if (this.predefinedValues == null)
				throw new ArgumentException (nameof (property) + " did not have predefined values", nameof (property));

			if (IsCombinable)
				UpdateValueList ();
			else
				UpdateValueName ();
		}

		public bool IsCombinable
		{
			get { return this.predefinedValues.IsValueCombinable; }
		}

		public IEnumerable<string> PossibleValues
		{
			get { return this.predefinedValues.PredefinedValues.Keys; }
		}

		public string ValueName
		{
			get { return this.valueName; }
			set
			{
				if (value == this.valueName)
					return;

				TValue realValue;
				if (!this.predefinedValues.PredefinedValues.TryGetValue (value, out realValue)) {
					if (this.predefinedValues.IsConstrainedToPredefined) {
						SetError (string.Format (LocalizationResources.InvalidValue, value)); 
						return;
					}
				} else
					Value = realValue;

			}
		}

		public bool IsConstrainedToPredefined => this.predefinedValues.IsConstrainedToPredefined;

		// TODO: Combination (flags) values
		List<string> valueList = new List<string> ();
		public List<string> ValueList {
			get { return this.valueList; }
			set {
				if (this.valueList == value)
					return;
				SetValueFromList (value);
			}
		}

		// TODO: Combination (flags) values
		protected override TValue ValidateValue (TValue validationValue)
		{
			if (!IsConstrainedToPredefined || IsValueDefined (validationValue))
				return validationValue;

			return Value;
		}

		protected override void OnValueChanged ()
		{
			base.OnValueChanged ();
			if (this.predefinedValues == null)
				return;

			if (IsCombinable) 
				UpdateValueList ();
			else
				UpdateValueName ();
		}

		private string valueName;
		private readonly IHavePredefinedValues<TValue> predefinedValues;

		private bool IsValueDefined (TValue value)
		{
			return this.predefinedValues.PredefinedValues.Values.Contains (value);
		}

		private bool TryGetValueName (TValue value, out string name)
		{
			name = null;

			foreach (var kvp in this.predefinedValues.PredefinedValues) {
				if (Equals (kvp.Value, value)) {
					name = kvp.Key;
					return true;
				}
			}
			return false;
		}

		private void UpdateValueName ()
		{
			string newValueName;
			if (TryGetValueName (Value, out newValueName)) {
				this.valueName = newValueName;
				OnPropertyChanged (nameof(ValueName));
			}
		}

		void SetValueFromList (IEnumerable<string> tickedButtons)
		{
			var foundValues = this.predefinedValues.PredefinedValues.Where (x => tickedButtons.Contains (x.Key));
			if (foundValues.Count () > 0) {
				var valuelist = foundValues.Select (y => y.Value).ToList ();
				var vi = new ValueInfo<IReadOnlyList<TValue>> () {
					Source = ValueSource.Local,
					Value = valuelist,
				};
				SetValue (vi);
			} else {
				if (IsConstrainedToPredefined) {
					SetError ("Invalid value"); // TODO: Localize & improve
				}
			}
		}

		async Task UpdateValueList ()
		{
			var values = await GetValues ();
			if (values != null) {
				valueList.Clear ();
				var range = this.predefinedValues.PredefinedValues.Where (x => values.Contains (x.Value)).Select (y => y.Key);
				valueList.AddRange (range);
				OnPropertyChanged (nameof (ValueList));
			}
		}
	}
}
