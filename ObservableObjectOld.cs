using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
namespace dwoodsRSS
{
	public abstract class ObservableObject : INotifyPropertyChanged
	{
#region Events
		public event PropertyChangedEventHandler PropertyChanged;
#endregion
#region Methods
		protected virtual void NotifyPropertyChanged(string propertyName)
		{
			if (propertyName == null)
			throw new ArgumentNullException("propertyName");
			PropertyChangedEventHandler propertyChangedHandler = PropertyChanged;
			if (propertyChangedHandler != null)
			{
				propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		protected virtual void NotifyPropertyChanged<TProperty>(Expression<Func<TProperty>> property)
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
			NotifyPropertyChanged(memberExpression.Member.Name);
		}
#endregion
	}
}
