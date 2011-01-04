using System;
using System.Collections.Specialized;

namespace FChoice.DevTools.HgbstUtils.Text.UI
{
	internal class ParamGroup : IComparable
	{
		private readonly StringCollection paramNames = new StringCollection();

		public string Name { get; set; }
		public bool Required { get; set; }
		public string ValueParamName { get; set; }
		public object Value { get; set; }

		public StringCollection ParamNames{ get{ return this.paramNames; } }

		public int CompareTo(object obj)
		{
			var paramGroup = (ParamGroup) obj;

			if( paramGroup.Name == null )
				return -1;
			if( paramGroup.Required && !(this.Required) )
				return 1;
			return Name.CompareTo(paramGroup.Name);
		}
	}
}
