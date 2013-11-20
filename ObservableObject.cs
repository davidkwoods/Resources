using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Temp
{

	/* Proper use of properties when inheriting from ObservableObject:
	 * 
	 * SetProperty checks to see if the value being assigned is different or the same
	 * as the one already there.  If it's the same, it does nothing and returns false.
	 * If it's different, the value is assigned and OnPropertyChanged is raised.
	 * 
	 * Use the returned bool to trigger subsequent OnPropertyChanged events for other,
	 * derived properties.  Use a Linq expression rather than a string to keep things
	 * refactor-friendly.
	 * 
	 * Example of a simple property:

		private string backerName;
		public string PropertyName
		{
			get { return backerName; }
			set { SetProperty(ref backerName, value); }
		}

	 * 
	 * Example of a derived property
	
		private int baseBacker;
		public int BaseProperty
		{
			get { return baseBacker; }
			set
			{
				if (SetProperty(ref baseBacker, value))
				{
					OnPropertyChanged(() => DerivedProperty);
				}
			}
		}
		public int DerivedProperty
		{
			get { return BaseProperty * BaseProperty; }
		}
	*/

	public abstract class ObservableObject : INotifyPropertyChanged
	{
		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected bool SetProperty<T>(ref T propertyBacker, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(propertyBacker, value))
				return false;

			propertyBacker = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		protected virtual void OnPropertyChanged<T>(Expression<Func<T>> property)
		{
			var lambda = (LambdaExpression)property;
			MemberExpression memberExpression;
			if (lambda.Body is UnaryExpression)
			{
				var unaryExpression = (UnaryExpression)lambda.Body;
				memberExpression = (MemberExpression)unaryExpression.Operand;
			}
			else
			{
				memberExpression = (MemberExpression)lambda.Body;
			}
			OnPropertyChanged(memberExpression.Member.Name);
		}
	}
}
