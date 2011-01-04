using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using FChoice.Common;
using FChoice.Common.Data;
using FChoice.DevTools.HgbstUtils.Core;

namespace FChoice.DevTools.HgbstUtils.Text.UI
{
	internal class HgbstUtilsMain
	{
		[STAThread]
		private static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				DisplayBanner();
				DisplayUsage();

				return;
			}

			foreach (var arg in args)
			{
				if (String.Compare(arg, "/?", true) == 0
				    || String.Compare(arg, "-?", true) == 0)
				{
					DisplayBanner();
					DisplayUsage();
					return;
				}

#if DEBUG
				if (String.Compare(arg, "/d", true) == 0
				    || String.Compare(arg, "-d", true) == 0)
				{
					Debugger.Break();
				}
#endif
			}


			try
			{
				var parseResult = HgbstUtilConfig.ParseCommandLine(args);

				if (! parseResult.WasSuccess)
				{
					DisplayBanner();

					const ErrorNumbers errType = ErrorNumbers.InvalidCmdLineSyntax;

					if (parseResult.GroupName != null)
					{
						DisplayError(errType, null,
						             "Invalid command line syntax. No {0} specified.",
						             parseResult.GroupName);
					}
					else
					{
						DisplayError(errType, null,
						             "Invalid command line syntax near argument {0} (value: '{1}'): {2}",
						             parseResult.ErrorArgIndex + 1,
						             parseResult.ErrorArgValue ?? "(null)",
						             parseResult.Message);
					}

					DisplayUsage();

					Environment.Exit((int) errType);
				}
				else
				{
					if (! HgbstUtilConfig.QuietMode)
						DisplayBanner();
				}

//				Console.WriteLine("Tree? [{0}]", GetVal(HgbstUtilConfig.ListTree));
//				Console.WriteLine("Elm? [{0}]", GetVal(HgbstUtilConfig.ElmObjid));	
//				Console.WriteLine("Show? [{0}]", GetVal(HgbstUtilConfig.ShowObjid));
//				Console.WriteLine("List? [{0}]", GetVal(HgbstUtilConfig.ListObjid));
//				Console.WriteLine("DbType? [{0}]", GetVal(HgbstUtilConfig.Db_type));
//				Console.WriteLine("Server? [{0}]", GetVal(HgbstUtilConfig.Db_server));
//				Console.WriteLine("Name? [{0}]", GetVal(HgbstUtilConfig.Db_name));
//				Console.WriteLine("User? [{0}]", GetVal(HgbstUtilConfig.Db_user));
//				Console.WriteLine("Pass? [{0}]", GetVal(HgbstUtilConfig.Db_pass));

				if (HgbstUtilConfig.Db_server != null
				    || HgbstUtilConfig.Db_name != null
				    || HgbstUtilConfig.Db_user != null
				    || HgbstUtilConfig.Db_pass != null)
				{
					DbProvider prov = DbProviderFactory.CreateProvider(HgbstUtilConfig.Db_type);

					if (! prov.ValidateConnectionParams(
						HgbstUtilConfig.Db_server,
						HgbstUtilConfig.Db_name,
						HgbstUtilConfig.Db_user,
						HgbstUtilConfig.Db_pass))
					{
						const ErrorNumbers errType = ErrorNumbers.InvalidDbParams;

						DisplayError(errType,
						             null,
						             "One or more required database connection parameters are missing. Either none, or all the required parameters must be specified.");

						DisplayUsage();
						Environment.Exit((int) errType);
					}

					string conStr = prov.CreateConnectionString(
						HgbstUtilConfig.Db_server,
						HgbstUtilConfig.Db_name,
						HgbstUtilConfig.Db_user,
						HgbstUtilConfig.Db_pass, "");

					FCConfiguration.Current[ConfigValues.DB_TYPE] = HgbstUtilConfig.Db_type;
					FCConfiguration.Current[ConfigValues.CONNECT_STRING] = conStr;

					// Reset the provider factory just in case
					DbProviderFactory.Provider = DbProviderFactory.CreateProvider(null);
				}
				else
				{
					if (FCConfiguration.Current == null)
						FCConfiguration.LoadEnvironmentSettings(new NameValueCollection(ConfigurationManager.AppSettings), false, true);

					var connectionString = FCConfiguration.Current[ConfigValues.CONNECT_STRING];

					if (connectionString == null || connectionString.Trim().Length == 0)
					{
						const ErrorNumbers errType = ErrorNumbers.NoDbSpecified;

						DisplayError(errType, null,
						             "No database parameters specified. Either edit the exe.config file or specify the parameters from the command line.");
						DisplayUsage();

						Environment.Exit((int) errType);
					}
				}


				if (HgbstUtilConfig.ListTree != null)
				{
					HgbstUtil.DumpHgbstLists(HgbstUtilConfig.ListTree.Split(','));
				}
				else if (HgbstUtilConfig.ElmObjid > 0)
				{
					HgbstUtil.DumpListForElementObjid(HgbstUtilConfig.ElmObjid);
				}
				else if (HgbstUtilConfig.ShowObjid > 0)
				{
					HgbstUtil.DumpListForShowObjid(HgbstUtilConfig.ShowObjid);
				}
				else if (HgbstUtilConfig.ListObjid > 0)
				{
					HgbstUtil.DumpListForListObjid(HgbstUtilConfig.ListObjid);
				}
				else if (HgbstUtilConfig.ReparentVals != null && HgbstUtilConfig.ReparentVals.Trim().Length > 0)
				{
					var reparentIdChain = HgbstUtilConfig.ReparentVals.Split(':');

					if (reparentIdChain.Length != 3)
					{
						const ErrorNumbers errType = ErrorNumbers.InvalidReparentValues;

						DisplayError(errType, null, "Invalid value for reparent objid list. Must be in the format: 1:2:3");
						Environment.Exit((int) errType);
					}

					var elmObjid = Int32.Parse(reparentIdChain[0].Trim());
					var childShowObjid = Int32.Parse(reparentIdChain[1].Trim());
					var newParentShowObjid = Int32.Parse(reparentIdChain[2].Trim());

					HgbstUtil.CopyAndReparentElement(elmObjid, childShowObjid, newParentShowObjid);
				}
			}
			catch (Exception ex)
			{
				const ErrorNumbers errType = ErrorNumbers.GeneralError;

				DisplayError(errType, ex, "Unexpected error");

				Environment.Exit((int) errType);
			}
		}

		public static void DisplayUsage()
		{
			if (! HgbstUtilConfig.QuietMode)
			{
				Console.WriteLine();
				Console.WriteLine(HgbstUtilConfig.GetUsageDisplay());
			}
		}

		public static void DisplayError(ErrorNumbers errNum, Exception ex, string errorFmt, params object[] args)
		{
			if (ex != null)
			{
				Console.WriteLine("ERROR: [{0}] {1}{2}\t{3}", (int) errNum, String.Format(errorFmt, args), Environment.NewLine, ex);
			}
			else
			{
				Console.WriteLine("ERROR: [{0}] {1}", (int) errNum, String.Format(errorFmt, args));
			}
		}

		public static void DisplayBanner()
		{
			if (! HgbstUtilConfig.QuietMode)
			{
				var processName = Process.GetCurrentProcess().ProcessName;

				var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

				Console.WriteLine("{0} - HGBST Utilities version {1}", processName, version);
				Console.WriteLine("http://www.dovetailsoftware.com :: support@dovetailsoftware.com");
				Console.WriteLine();
			}
		}
	}
}