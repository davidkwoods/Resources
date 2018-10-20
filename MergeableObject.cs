using System;

namespace Temp
{
	public abstract class MergeableObject : ObservableObject, IEquatable<MergeableObject>
	{
		#region Events

		public event EventHandler Merged;

		#endregion

		#region Properties

		public abstract string MergeID
		{
			get;
		}

		#endregion

		#region Constructors

		public MergeableObject()
		{
		}

		#endregion

		#region Methods

		public abstract void Merge(MergeableObject obj);

        //Added a comment here.
		public virtual bool Equals(MergeableObject obj)
		{
			return this.MergeID == obj.MergeID;
		}

		protected virtual void OnMerged()
		{
			EventHandler mergedHandler = Merged;

			if (mergedHandler != null)
				mergedHandler(this, EventArgs.Empty);
		}

		protected bool DoTypesMatch<T>(T source, object target)
		{
			return target is T;
		}

		#endregion
	}

}
