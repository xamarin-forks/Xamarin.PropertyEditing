﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Tests
{
	internal abstract class PredefinedValuesViewModelTests<T>
		: PropertyViewModelTests<T, PredefinedValuesViewModel<T>>
	{
		[Test]
		public void NameMatches ()
		{
			T testValue = GetRandomTestValue ();
			string name = GetName (testValue);

			var vm = GetBasicTestModel (testValue);
			Assert.That (vm.ValueName, Is.EqualTo (name));
		}

		[Test]
		public void NewValueNameMatches ()
		{
			T testValue = GetNonDefaultRandomTestValue ();
			string testValueName = GetName (testValue);

			var vm = GetBasicTestModel ();
			Assume.That (vm.Value, Is.Not.EqualTo (testValue));
			Assume.That (vm.ValueName, Is.Not.EqualTo (testValueName));

			vm.Value = testValue;
			Assert.That (vm.ValueName, Is.EqualTo (testValueName));
		}

		[Test]
		public void NewValueChangesValueNameProperty ()
		{
			T testValue = GetNonDefaultRandomTestValue ();

			var vm = GetBasicTestModel ();
			Assume.That (vm.Value, Is.Not.EqualTo (testValue));

			bool changed = false;
			vm.PropertyChanged += (sender, args) => {
				if (args.PropertyName == nameof(vm.ValueName))
					changed = true;
			};

			vm.Value = testValue;
			Assert.That (changed, Is.True);
		}

		[Test]
		public void NewValueNameValueMatches ()
		{
			T testValue = GetNonDefaultRandomTestValue ();
			string testValueName = GetName (testValue);

			var vm = GetBasicTestModel ();
			Assume.That (vm.Value, Is.Not.EqualTo (testValue));
			Assume.That (vm.ValueName, Is.Not.EqualTo (testValueName));

			vm.ValueName = testValueName;
			Assert.That (vm.Value, Is.EqualTo (testValue));
		}

		[Test]
		public void NewValueNameChangesValue ()
		{
			T testValue = GetNonDefaultRandomTestValue ();
			string testValueName = GetName (testValue);

			var vm = GetBasicTestModel ();
			Assume.That (vm.Value, Is.Not.EqualTo (testValue));
			Assume.That (vm.ValueName, Is.Not.EqualTo (testValueName));

			bool changed = false;
			vm.PropertyChanged += (sender, args) => {
				if (args.PropertyName == nameof(vm.Value))
					changed = true;
			};

			vm.ValueName = testValueName;
			Assert.That (changed, Is.True);
		}

		[Test]
		public void DuplicateValues ()
		{
			T testValue = GetNonDefaultRandomTestValue ();

			var property = GetPropertyMock ();
			var predefined = property.As<IHavePredefinedValues<T>> ();
			predefined.SetupGet (p => p.PredefinedValues).Returns (new Dictionary<string, T> {
				{ "Value", testValue },
				{ "SameValue", testValue }
			});

			var vm = GetViewModel (property.Object, new[] { GetBasicEditor (property.Object) });

			Assume.That (vm.ValueName, Is.Not.EqualTo ("Value"));
			vm.ValueName = "Value";
			Assert.That (vm.Value, Is.EqualTo (testValue));
			
			bool changed = false;
			vm.PropertyChanged += (sender, args) => {
				if (args.PropertyName == nameof(vm.Value))
					changed = true;
			};

			vm.ValueName = "SameValue";
			Assert.That (vm.Value, Is.EqualTo (testValue));
			Assert.That (changed, Is.False);
		}

		protected abstract bool IsConstrained { get; }
		protected abstract IReadOnlyDictionary<string, T> Values { get; }

		protected string GetName (T value)
		{
			string name = GetNameOrDefault (value);
			Assume.That (name, Is.Not.Null);
			return name;
		}

		protected string GetNameOrDefault (T value)
		{
			return Values.Where (kvp => Equals (value, kvp.Value)).Select (kvp => kvp.Key).FirstOrDefault ();
		}

		protected override void AugmentPropertyMock (Mock<IPropertyInfo> propertyMock)
		{
			var predefined = propertyMock.As<IHavePredefinedValues<T>> ();
			predefined.SetupGet (h => h.IsConstrainedToPredefined).Returns (IsConstrained);
			predefined.SetupGet (h => h.PredefinedValues).Returns (Values);
		}
	}

	internal abstract class ConstrainedPredefinedValuesViewModelTests<T>
		: PredefinedValuesViewModelTests<T>
	{
		protected override bool IsConstrained => true;

		protected abstract T GetOutOfBoundsValue ();

		protected abstract string GetOutOfBoundsValueName ();

		protected override PredefinedValuesViewModel<T> GetViewModel (TargetPlatform platform, IPropertyInfo property, IEnumerable<IObjectEditor> editors)
		{
			return new PredefinedValuesViewModel<T> (platform, property, editors);
		}

		[Test]
		public void SetValueOutOfBounds ()
		{
			T value = GetOutOfBoundsValue ();

			var vm = GetBasicTestModel ();
			T originalValue = vm.Value;
			string originalValueName = vm.ValueName;
			Assume.That (vm.Value, Is.Not.EqualTo (value));

			vm.Value = value;
			Assert.That (vm.Value, Is.EqualTo (originalValue));
			Assert.That (vm.ValueName, Is.EqualTo (originalValueName));
		}

		[Test]
		public void SetValueNameOutOfBounds ()
		{
			string valueName = GetOutOfBoundsValueName ();

			var vm = GetBasicTestModel ();
			T originalValue = vm.Value;
			string originalValueName = vm.ValueName;
			Assume.That (vm.ValueName, Is.Not.EqualTo (valueName));

			bool changed = false;
			vm.PropertyChanged += (sender, args) => {
				if (args.PropertyName == nameof(vm.ValueName))
					changed = true;
			};

			vm.ValueName = valueName;
			Assert.That (vm.Value, Is.EqualTo (originalValue));
			Assert.That (vm.ValueName, Is.EqualTo (originalValueName));
			Assert.That (changed, Is.False);
		}
	}

	internal enum PredefinedEnumTest
		: int
	{
		None = 0,
		First = 1,
		Second = 2,
		Eigth = 8
	}

	[TestFixture]
	internal class EnumPredefinedViewModelTests
		: ConstrainedPredefinedValuesViewModelTests<int>
	{
		public EnumPredefinedViewModelTests ()
		{
			this.values = (int[]) Enum.GetValues (typeof(PredefinedEnumTest));
			this.names = Enum.GetNames (typeof(PredefinedEnumTest));

			var v = new Dictionary<string, int> (this.values.Length);
			for (int i = 0; i < this.values.Length; i++) {
				v.Add (this.names[i], this.values[i]);
			}

			this.predefinedValues = v;
		}

		protected override IReadOnlyDictionary<string, int> Values => this.predefinedValues;

		protected override string GetOutOfBoundsValueName ()
		{
			return "foo";
		}

		protected override int GetOutOfBoundsValue ()
		{
			return -1;
		}

		protected override int GetRandomTestValue (Random rand)
		{
			int index = rand.Next (0, this.values.Length);
			return this.values[index];
		}

		protected override PredefinedValuesViewModel<int> GetViewModel (TargetPlatform platform, IPropertyInfo property, IEnumerable<IObjectEditor> editors)
		{
			return new PredefinedValuesViewModel<int> (platform, property, editors);
		}

		private readonly IReadOnlyDictionary<string, int> predefinedValues;
		private readonly int[] values;
		private readonly string[] names;
	}

	internal abstract class AbdstractCombinableEnumPredefinedViewModelTests<T>
		: ConstrainedPredefinedValuesViewModelTests<T>
	{
		protected abstract List<string> GetRandomTestValueList ();

		protected override void AugmentPropertyMock (Mock<IPropertyInfo> propertyMock)
		{
			base.AugmentPropertyMock (propertyMock);
			var predefined = propertyMock.As<IHavePredefinedValues<T>> ();
			predefined.SetupGet (h => h.IsValueCombinable).Returns (true);
		}

		[Test]
		public void HasValueChangedWhenSetViaValueList ()
		{
			List<string> originalValueList;
			do {
				originalValueList = GetRandomTestValueList ();
			} while (originalValueList.Count < 2);

			var vm = GetBasicTestModel ();

			vm.ValueList = originalValueList;
			var originalValue = vm.Value;

			bool changed = false;
			vm.PropertyChanged += (sender, args) => {
				if (args.PropertyName == nameof (vm.Value))
					changed = true;
			};

			originalValueList.Remove (originalValueList[originalValueList.Count - 1]);
			var newValueList = originalValueList;
			vm.ValueList = newValueList;

			Assert.That (changed, Is.True);
			Assert.That (vm.Value, !Is.EqualTo (originalValue));
		}

		[Test]
		public async Task GetCheckedValues ()
		{
			var originalValueList = GetRandomTestValueList ();
			var vm = GetBasicTestModel ();

			vm.ValueList = originalValueList;

			var value = await vm.GetValues ();

			Assert.That (value, !Is.Null);
			Assert.That (value.Count, Is.GreaterThan (0));
		}
	}

	[TestFixture]
	internal class CombinableEnumPredefinedViewModelTests
		: AbdstractCombinableEnumPredefinedViewModelTests<int>
	{
		private readonly int[] values;
		private readonly string[] names;

		IReadOnlyDictionary<string, int> predefinedValues;
		protected override IReadOnlyDictionary<string, int> Values => this.predefinedValues;

		public CombinableEnumPredefinedViewModelTests ()
		{
			this.names = Enum.GetNames (typeof (FlagsTestEnum));
			this.values = (int[])Enum.GetValues (typeof (FlagsTestEnum));

			var vp = new Dictionary<string, int> (names.Length);
			for (int i = 0; i < names.Length; i++) {
				vp.Add (names[i], values[i]);
			}

			predefinedValues = vp;
		}

		protected override string GetOutOfBoundsValueName ()
		{
			return "foo";
		}

		protected override int GetOutOfBoundsValue ()
		{
			return 16;
		}

		protected override int GetRandomTestValue (Random rand)
		{
			int index = rand.Next (0, values.Length - 1);
			int value = values[index];
			if (index > 0) {
				int flags = rand.Next (0, values.Length - 1);
				for (int i = 0; i < flags; i++) {
					value |= values[rand.Next (1, values.Length - 1)];
				}
			}

			return value;
		}

		protected override List<string> GetRandomTestValueList ()
		{
			var rand = new Random (int.MaxValue);
			int index = rand.Next (0, values.Length - 1);
			var value = new List<string> ();
			if (index > 0) {
				int flags = rand.Next (0, values.Length - 1);
				for (int i = 0; i < flags; i++) {
					value.Add (names[rand.Next (1, values.Length - 1)]);
				}
			}

			return value;
		}

		protected override PredefinedValuesViewModel<int> GetViewModel (TargetPlatform platform, IPropertyInfo property, IEnumerable<IObjectEditor> editors)
		{
			return new PredefinedValuesViewModel<int> (platform, property, editors);
		}
	}
}
