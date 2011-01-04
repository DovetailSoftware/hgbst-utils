using System;
using System.Reflection;

namespace FChoice.DevTools.HgbstUtils.Text.UI
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple=false)] 
	public class ConfigParamAttribute : Attribute
	{
		private string uniqueParamGroupName;

		public ConfigParamAttribute( string longName, string shortName, string valueDesc, string description )
		{
			Hidden = false;
			ValidValues = null;
			Example = null;
			GroupRequired = false;
			ValueRequired = true;
			DisplayDefault = true;
			DefaultValue = null;
			ParamNameLong = longName;
			ParamNameShort = shortName;
			Description = description;
			ValueDescription = valueDesc;
		}

		public string ParamNameShort { get; set; }
		public string ParamNameLong { get; set; }
		public string DefaultValue { get; set; }
		public bool DisplayDefault { get; set; }
		public string ValueDescription { get; set; }
		public bool ValueRequired { get; set; }

		public string Group
		{
			get{ return uniqueParamGroupName; }
			set
			{ 
				if( value == "Miscellaneous" )
				{
					throw new InvalidOperationException("The group name 'Miscellaneous' is reserved.");
				}
				uniqueParamGroupName = value; 
			}
		}

		public bool GroupRequired { get; set; }
		public string Example { get; set; }
		public string Description { get; set; }
		public string[] ValidValues { get; set; }
		public PropertyInfo Property { get; set; }
		public bool Hidden { get; set; }
	}
}
