using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace FChoice.DevTools.HgbstUtils.Text.UI
{
	public static class HgbstUtilConfig
	{
		private const int COL_SPACING_WIDTH = 2;

		private static readonly Hashtable paramMap;
		private static readonly Hashtable groupMap;
		private static readonly NameValueCollection nameMap;

		private static int elmObjid = -1;
		private static int showObjid = -1;
		private static int listObjid = -1;

		[ConfigParam("tree", "t", "name(s)",
			"Output the specified (by a comma-delimited list, or nothing for all)^^^LISTS as a tree", DisplayDefault = false,
			Group = "operation", GroupRequired = true, DefaultValue = "", Example = "/tree \"FAMILY,PART_STATUS\"",
			ValueRequired = false)]
		public static string ListTree { get; set; }

		[ConfigParam("elm", "e", "objid", "Output a tree for the LIST to which the specified (by objid)^^^element belongs",
			DefaultValue = "-1", DisplayDefault = false, Group = "operation", Example = "/elm 268435562")]
		public static int ElmObjid
		{
			get { return elmObjid; }
			set { elmObjid = value; }
		}

		[ConfigParam("show", "s", "objid", "Output a tree for the LIST to which the specified (by objid)^^^show belongs",
			DefaultValue = "-1", DisplayDefault = false, Group = "operation", Example = "/show 268435562")]
		public static int ShowObjid
		{
			get { return showObjid; }
			set { showObjid = value; }
		}

		[ConfigParam("list", "l", "objid", "Output a tree for the specified (by objid) LIST", DefaultValue = "-1",
			DisplayDefault = false, Group = "operation", Example = "/list 268435562")]
		public static int ListObjid
		{
			get { return listObjid; }
			set { listObjid = value; }
		}

		[ConfigParam("reparent", "r", "elmid:childshowid:newparentshowid",
			"Reparent an element hierarchy from one show to another show", DefaultValue = "", DisplayDefault = false,
			Group = "operation", Hidden = true, Example = "/reparent 268435562:268435562:268435562")]
		public static string ReparentVals { get; set; }

		[ConfigParam("db_type", "dbt", "type", "The provider type to use for the database connection", DefaultValue = "MSSQL",
			ValidValues = new[] {"MSSQL", "ORACLE"}, Example = "/db_type MSSQL")]
		public static string Db_type { get; set; }

		[ConfigParam("db_server", "dbs", "host", "The server/instance name or SID of the database server",
			Example = "/db_server CLFY_DB")]
		public static string Db_server { get; set; }

		[ConfigParam("db_name", "dbn", "catalog", "The database name within the instance (not required for ORACLE)",
			Example = "/db_name CL12")]
		public static string Db_name { get; set; }

		[ConfigParam("db_user", "dbu", "user id", "The user or login to use for database authentication",
			Example = "/db_user sa")]
		public static string Db_user { get; set; }

		[ConfigParam("db_pass", "dbp", "password", "The password to use for database authentication",
			Example = "/db_pass yourpassword")]
		public static string Db_pass { get; set; }

		[ConfigParam("quiet", "q", null, "Do not display non-log output to the console", DefaultValue = "true",
			ValueRequired = false, DisplayDefault = false)]
		public static bool QuietMode { get; set; }

		[ConfigParam("debug", "d", null, "Spawn the debugger at startedup", DefaultValue = "true", Hidden = true,
			ValueRequired = false, DisplayDefault = false)]
		public static bool DebugMode { get; set; }

		static HgbstUtilConfig()
		{
			Db_type = null;
			DebugMode = false;
			QuietMode = false;
			Db_pass = null;
			Db_user = null;
			Db_name = null;
			Db_server = null;
			ReparentVals = null;
			ListTree = null;
			paramMap = new Hashtable(StringComparer.InvariantCulture);

			groupMap = new Hashtable(StringComparer.InvariantCulture);

			nameMap = new NameValueCollection();

			var paramGroup = new ParamGroup {Required = false};

			groupMap.Add("Miscellaneous", paramGroup);

			var properties = typeof (HgbstUtilConfig).GetProperties();

			foreach (var propertyInfo in properties)
			{
				var configParamAttributes = propertyInfo.GetCustomAttributes(typeof (ConfigParamAttribute), false);

				if (configParamAttributes.Length <= 0) continue;

				var configParamAttribute = (ConfigParamAttribute) configParamAttributes[0];

				configParamAttribute.Property = propertyInfo;

				paramMap.Add(configParamAttribute.ParamNameLong, configParamAttribute);
				nameMap.Add(configParamAttribute.ParamNameShort, configParamAttribute.ParamNameLong);
				nameMap.Add(configParamAttribute.ParamNameLong, configParamAttribute.ParamNameShort);

				if (configParamAttribute.Group != null && configParamAttribute.Group != "Miscellaneous")
				{
					if (groupMap.ContainsKey(configParamAttribute.Group) == false)
					{
						paramGroup = new ParamGroup {Name = configParamAttribute.Group, Required = configParamAttribute.GroupRequired};
						paramGroup.ParamNames.Add(configParamAttribute.ParamNameLong);

						groupMap.Add(configParamAttribute.Group, paramGroup);
					}
					else
					{
						paramGroup = (ParamGroup) groupMap[configParamAttribute.Group];

						if (configParamAttribute.GroupRequired && paramGroup.Required == false)
							paramGroup.Required = true;

						paramGroup.ParamNames.Add(configParamAttribute.ParamNameLong);
					}
				}
				else
				{
					paramGroup = (ParamGroup) groupMap["Miscellaneous"];

					paramGroup.ParamNames.Add(configParamAttribute.ParamNameLong);
				}
			}
		}

		public static string GetUsageDisplay()
		{
			var proc = Process.GetCurrentProcess();

			var maxNameLength = 0;

			var usageTotal = new StringBuilder();
			var usageLine = new StringBuilder();
			var usageItems = new ArrayList();

			usageLine.AppendFormat("{0} ", proc.ProcessName);

			var groups = new ArrayList(groupMap.Values);
			groups.Sort();

			foreach (ParamGroup group in groups)
			{
				usageItems.Add(new GroupUsageItem(group));

				var paramNames = new ArrayList(group.ParamNames);
				paramNames.Sort();

				var firstParam = true;

				foreach (string paramName in paramNames)
				{
					var configParamAttribute = (ConfigParamAttribute) paramMap[paramName];

					if (configParamAttribute.Hidden == false)
					{
						var paramKey = paramName;

						if (configParamAttribute.ValueRequired)
						{
							paramKey = String.Format("{0} {1}", paramName, configParamAttribute.ValueDescription);
						}
						else if (configParamAttribute.ValueDescription != null)
						{
							paramKey = String.Format("{0} [{1}]", paramName, configParamAttribute.ValueDescription);
						}

						if (paramKey.Length > maxNameLength)
							maxNameLength = paramKey.Length;

						var paramUsageItem = new ParamUsageItem(paramKey, configParamAttribute);

						usageItems.Add(paramUsageItem);

						var lineStr = String.Format("{0}{1}", (firstParam) ? "[ " : " ", paramUsageItem.GetLineString());

						if (usageLine.Length + lineStr.Length > 78)
						{
							usageTotal.Append(usageLine.ToString());
							usageTotal.Append(Environment.NewLine);
							usageLine = new StringBuilder();
						}

						usageLine.Append(lineStr);

						firstParam = false;
					}
				}

				usageLine.Append(" ]");
			}

			usageTotal.Append(usageLine.ToString());

			foreach (IUsageItem usageItem in usageItems)
			{
				usageTotal.Append(usageItem.GetDetailString(maxNameLength));
			}

			return usageTotal.ToString();
		}

		private interface IUsageItem
		{
			string GetLineString();
			string GetDetailString(int maxNameLength);
		}

		private class GroupUsageItem : IUsageItem
		{
			private readonly ParamGroup group;
			private readonly string groupName;

			public GroupUsageItem(ParamGroup group)
			{
				this.group = group;

				if (this.group.Name == null)
				{
					groupName = "Miscellaneous";
				}
				else
				{
					groupName = this.group.Name.Substring(0, 1).ToUpper() + this.group.Name.Substring(1);
				}

				if (this.group.Required)
				{
					groupName += " (One Required)";
				}
			}

			public string GetLineString()
			{
				return "";
			}

			public string GetDetailString(int maxNameLength)
			{
				return String.Format("{0}{0}{1}{0}{2}",
				                     Environment.NewLine,
				                     groupName,
				                     new String('-', groupName.Length));
			}
		}

		private class ParamUsageItem : IUsageItem
		{
			private readonly string paramKey;
			private readonly ConfigParamAttribute paramAttr;

			public ParamUsageItem(string key, ConfigParamAttribute attr)
			{
				paramKey = key;
				paramAttr = attr;
			}

			public string GetLineString()
			{
				return String.Format("[/{0}]", paramKey);
			}

			public string GetDetailString(int maxNameLength)
			{
				var indent = (maxNameLength + COL_SPACING_WIDTH) + 1;
				var indentStr = String.Format("{0}{1}", Environment.NewLine, new String(' ', indent));

				var descriptionBuilder = new StringBuilder();
				descriptionBuilder.Append(paramAttr.Description.Replace("^^^", indentStr));

				descriptionBuilder.AppendFormat("{0}Shortened: /{1}", indentStr, paramAttr.ParamNameShort);

				if (paramAttr.DefaultValue != null && paramAttr.DisplayDefault)
				{
					descriptionBuilder.AppendFormat("{0}Default: {1}", indentStr, paramAttr.DefaultValue);
				}

				if (paramAttr.ValidValues != null)
				{
					descriptionBuilder.AppendFormat("{0}Valid values: '{1}'", indentStr, String.Join("','", paramAttr.ValidValues));
				}

				if (paramAttr.Example != null)
				{
					descriptionBuilder.AppendFormat("{0}Example: {1}", indentStr, paramAttr.Example);
				}

				return String.Format("{0}/{1}{2}{3}",
				                     Environment.NewLine,
				                     paramKey,
				                     new String(' ', (maxNameLength - paramKey.Length + COL_SPACING_WIDTH)),
				                     descriptionBuilder);
			}
		}

		public static ParseResult ParseCommandLine(string[] args)
		{
			var cfgValues = new Hashtable(StringComparer.InvariantCulture);

			if (args == null)
				throw new ArgumentNullException("args");

			var parseResult = new ParseResult();

			for (var i = 0; i < args.Length; i++)
			{
				var argument = args[i];

				if (argument[0] == '/' || argument[0] == '-')
				{
					var argInst = new Argument {Index = i, RawName = argument, Name = argument.Substring(1)};

					if (argument.Length == 1)
					{
						parseResult.UpdateStatus(false, i, argument);
					}

					// Look for a value after /foo in the next arg
					if ((i + 1) < args.Length)
					{
						string nextArg = args[i + 1];
						argInst.RawValue = nextArg;

						//make sure the next arg isn't another arg (/bar)
						// NOTE: you can escape values that start with "/" or "-" with "//" or "--"
						// respectively
						if (nextArg[0] == '/')
						{
							if (nextArg.Length > 1)
							{
								if (nextArg[1] == '/')
								{
									// Remove the escaped '/'
									argInst.Value = nextArg.Substring(1);
									i++;
								}
								// else, the next arg is an actual /foo-type command, so just
								// let the next loop iteration pick it up
							}
							else
							{
								argInst.Value = nextArg;
								i++;
							}
						}
						else if (nextArg[0] == '-')
						{
							if (nextArg.Length > 1)
							{
								if (nextArg[1] == '-')
								{
									// Remove the escaped '-'
									argInst.Value = nextArg.Substring(1);
									i++;
								}
								// else, the next arg is an actual -foo-type command, so just
								// let the next loop iteration pick it up		
							}
							else
							{
								argInst.Value = nextArg;
								i++;
							}
						}
						else
						{
							argInst.Value = nextArg;
							i++;
						}
					}

					cfgValues.Add(argInst.Name, argInst);
				}
				else
				{
					parseResult.UpdateStatus(false, i, argument);
					break;
				}
			}

			MapValuesToProperties(parseResult, null, cfgValues);

			// Check for unfulfilled required groups
			foreach (ParamGroup g in groupMap.Values)
			{
				if (g.Required && g.ValueParamName == null)
				{
					parseResult.UpdateStatus(false, g.Name);
					break;
				}
			}

			return parseResult;
		}

		private static void MapValuesToProperties(ParseResult result, object instance, IDictionary cfgValues)
		{
			foreach (Argument arg in cfgValues.Values)
			{
				// Try with the name given.
				var configParamAttribute = (ConfigParamAttribute) paramMap[arg.Name];

				if (configParamAttribute == null)
				{
					// If not, try mapping it from long->short or short->long to see if we find it
					var altName = nameMap[arg.Name];

					if (altName != null)
						configParamAttribute = (ConfigParamAttribute) paramMap[altName];
				}

				// If it's still null, give up, there's no param identified with that name
				if (configParamAttribute == null)
				{
					result.UpdateStatus(false, arg.Index, arg.RawValue,
					                    "Unrecognized parameter '{0}'", arg.RawName);
					return;
				}

				// Check for required value
				if (configParamAttribute.ValueRequired && (arg.Value == null || arg.Value.Trim().Length == 0))
				{
					result.UpdateStatus(false, arg.Index, arg.Value, "A value is required for parameter '{0}'", arg.Name);
				}

				// Check for group stuff
				if (configParamAttribute.Group != null)
				{
					var paramGroup = (ParamGroup) groupMap[configParamAttribute.Group];

					if (paramGroup.ValueParamName != null)
					{
						result.UpdateStatus(false, arg.Index, arg.Value,
						                    "Cannot specifiy more than one {0} argument (both '{1}' and '{2}' were specified)",
						                    paramGroup.Name, configParamAttribute.ParamNameLong, paramGroup.ValueParamName);

						return;
					}
					
					paramGroup.ValueParamName = configParamAttribute.ParamNameLong;
					paramGroup.Value = arg.Value;
				}

				if ((configParamAttribute.ValidValues != null && configParamAttribute.ValidValues.Length > 0)
				    && (arg.Value == null || arg.Value.Trim().Length == 0))
				{
					result.UpdateStatus(false, arg.Index, arg.Value,
					                    "Value for argument '{0}' is not valid. Valid values are: {1}",
					                    arg.Name, String.Join(", ", configParamAttribute.ValidValues));

					return;
				}

				// Verify that it's among the required values
				if (configParamAttribute.ValidValues != null)
				{
					var valueFound = false;

					foreach (var value in configParamAttribute.ValidValues)
					{
						if (String.Compare(value, arg.Value, true) != 0) continue;
						valueFound = true;
						break;
					}

					if (! valueFound)
					{
						result.UpdateStatus(false, arg.Index, arg.Value,
						                    "Value for argument '{0}' is not valid. Valid values are: {1}",
						                    arg.Name, String.Join(", ", configParamAttribute.ValidValues));

						return;
					}
				}

				// Assign the value
				object realVal;

				try
				{
					realVal = Convert.ChangeType(arg.Value ?? configParamAttribute.DefaultValue, configParamAttribute.Property.PropertyType);
				}
				catch
				{
					result.UpdateStatus(false, arg.Index, arg.Value, "Invalid value format (cannot parse)");
					return;
				}


				configParamAttribute.Property.SetValue(instance, realVal, BindingFlags.Static, null, null, null);
			}
		}

		private class Argument
		{
			public string Name;
			public string Value;
			public int Index;
			public string RawName;
			public string RawValue;
		}
	}
}