using System;
using System.Collections;
using System.Data;
using FChoice.Common;
using FChoice.Common.Data;

namespace FChoice.DevTools.HgbstUtils.Core
{
	public static class HgbstUtil
	{
		private static readonly Logger log = LogManager.GetLogger(typeof (HgbstUtil));

		#region DumpHgbstLists

		public static void DumpHgbstLists(params string[] listNames)
		{
			var sqlHelper = new SqlHelper();
			DataSet listDs;

			if (listNames == null || listNames.Length == 0 || (listNames.Length == 1 && listNames[0].Trim().Length == 0))
			{
				sqlHelper.CommandText = "SELECT * FROM table_hgbst_lst";
				listDs = sqlHelper.ExecuteDataSet();
			}
			else
			{
				var names = new object[listNames.Length];
				string colName = null;
				const string hgbstListSql = "SELECT * FROM table_hgbst_lst WHERE (title IN({0}))";

				Array.Copy(listNames, 0, names, 0, listNames.Length);

				sqlHelper.CommandText = String.Format(hgbstListSql, SqlHelper.IN_LIST_TOKEN);
				listDs = sqlHelper.ExecuteDataSetInList(names, colName);
			}

			if (listDs == null || listDs.Tables.Count == 0 || listDs.Tables[0].Rows.Count == 0)
			{
				var namesCombined = (listNames == null) ? "" : String.Join("', '", listNames);
				Console.WriteLine("No HGBST lists found matching the specified criteria ('{0}').", namesCombined);

				return;
			}

			DumpHgbstLists(listDs);
		}

		private static void DumpHgbstLists(DataSet listDs)
		{
			var listCtr = 0;

			foreach (DataRow row in listDs.Tables[0].Rows)
			{
				if (row["hgbst_lst2hgbst_show"] == DBNull.Value || row["hgbst_lst2hgbst_show"] == null)
				{
					Console.WriteLine("LIST[{0}]: *** ERROR *** Invalid list: No root show", ++listCtr);
					return;
				}
				
				Console.WriteLine("LIST[{0}]: {1} ({2})", ++listCtr, row["title"], row["objid"]);

				var trackTable = new Hashtable();
				var wasSuccess = RenderShow(trackTable, 1, Convert.ToInt32(row["hgbst_lst2hgbst_show"]), 0);

				if (! wasSuccess)
				{
					Console.WriteLine("LIST[{0}]: One or more errors were detected during the processing of this list", listCtr);
					return;
				}
			}
		}

		private static bool RenderShow(Hashtable trackTable, int indentLevel, int showId, int parentElmId)
		{
			var showSet = SqlHelper.ExecuteDataSet(String.Format("SELECT * FROM table_hgbst_show WHERE objid = {0}", showId));

			var showRow = showSet.Tables[0].Rows[0];

			var indent = new char[indentLevel*2];
			var idx = 0;

			for (var i = 0; i < indentLevel; i++)
			{
				indent[idx++] = '|';
				indent[idx++] = ' ';
			}


			// Check for loop
			if (parentElmId > 0 && trackTable.ContainsKey(showRow["objid"]))
			{
				Console.WriteLine("{0}|_Show: {1} ({2}) *** ERROR *** Loop detected", new String(indent), showRow["title"], showRow["objid"]);
				return false;
			}
			
			Console.WriteLine("{0}|_Show: {1} ({2})", new String(indent), showRow["title"], showRow["objid"]);

			trackTable.Add(showRow["objid"], "");

			return GetElementsForShow(trackTable, indentLevel + 1, Convert.ToInt32(showRow["objid"]), parentElmId);
		}

		private static bool GetElementsForShow(Hashtable trackTable, int indentLevel, int showId, int parentElmId)
		{
			var elementDataSet = SqlHelper.ExecuteDataSet(
				String.Format(@"
SELECT e.objid, e.title 
FROM table_hgbst_elm e, mtm_hgbst_elm0_hgbst_show1 mtm
WHERE e.objid = mtm.hgbst_elm2hgbst_show
AND mtm.hgbst_show2hgbst_elm = {0}
AND mtm.hgbst_elm2hgbst_show != {1}",
					showId, parentElmId));

			foreach (DataRow elmRow in elementDataSet.Tables[0].Rows)
			{
				var indent = new char[indentLevel*2];
				var idx = 0;

				for (var i = 0; i < indentLevel; i++)
				{
					indent[idx++] = '|';
					indent[idx++] = ' ';
				}

				// Look for sub-shows/levels

				var showSet = SqlHelper.ExecuteDataSet(
					String.Format(
						@"
SELECT * 
FROM mtm_hgbst_elm0_hgbst_show1 
WHERE hgbst_elm2hgbst_show = {0}
AND hgbst_show2hgbst_elm != {1}",
						elmRow["objid"], showId));

				if (showSet != null && showSet.Tables.Count > 0 && showSet.Tables[0].Rows.Count > 0)
				{
					if (showSet.Tables[0].Rows.Count > 1)
					{
						Console.WriteLine(
							"{0}|_Element: {1} ({2}) *** ERROR *** Element is linked to more than one parent (or has more than one child) Show 1 Objid: {3}, Show 2 Objid: {4}",
							new String(indent),
							elmRow["title"],
							elmRow["objid"],
							showSet.Tables[0].Rows[0]["hgbst_show2hgbst_elm"],
							showSet.Tables[0].Rows[1]["hgbst_show2hgbst_elm"]);

						return false;
					}
					
					Console.WriteLine("{0}|_Element: {1} ({2})", new String(indent), elmRow["title"], elmRow["objid"]);

					// Now we've got the sub-show ID
					var wasSuccessful = RenderShow(trackTable, indentLevel + 1,
					                             Convert.ToInt32(showSet.Tables[0].Rows[0]["hgbst_show2hgbst_elm"]),
					                             Convert.ToInt32(elmRow["objid"]));

					if (! wasSuccessful)
						return false;
				}
				else
				{
					Console.WriteLine("{0}|_Element: {1} ({2})", new String(indent), elmRow["title"], elmRow["objid"]);
				}
			}

			return true;
		}

		#endregion

		#region DumpListForListObjid

		public static void DumpListForListObjid(int listObjid)
		{
			var sqlHelper = new SqlHelper("SELECT * FROM table_hgbst_lst WHERE objid = {0}");
			sqlHelper.Parameters.Add("listobjid", listObjid);

			var listSet = sqlHelper.ExecuteDataSet();

			if (listSet == null || listSet.Tables.Count == 0 || listSet.Tables[0].Rows.Count == 0)
			{
				Console.WriteLine("No HGBST List found with objid '{0}'.", listObjid);
				return;
			}

			DumpHgbstLists(sqlHelper.ExecuteDataSet());
		}

		#endregion

		#region DumpListForElementObjid

		public static void DumpListForElementObjid(int elmObjid)
		{
			#region Sql statement to get list for element (NOTE: Only supports 5-levels)

			// NOTE WARN Hard-coded to 5-levels-or-less support
			const string listByElementObjidSql = @"
SELECT objid FROM table_hgbst_lst WHERE hgbst_lst2hgbst_show = 
(
	SELECT show5.objid FROM table_hgbst_show show5, table_hgbst_show show5A
	WHERE show5A.objid IN 
	(
		SELECT show4.objid FROM table_hgbst_show show4, table_hgbst_show show4A
		WHERE show4A.objid IN 
		(
			SELECT show3.objid FROM table_hgbst_show show3, table_hgbst_show show3A
			WHERE show3A.objid IN 
			(
				SELECT show2.objid FROM table_hgbst_show show2, table_hgbst_show show2A
				WHERE show2A.objid IN 
				(
					SELECT show1.objid FROM table_hgbst_show show1, table_hgbst_show show1A
					WHERE show1A.objid IN 
					(
						SELECT parentShow.objid
						FROM table_hgbst_show parentShow, mtm_hgbst_elm0_hgbst_show1 mtm1
						WHERE 
							mtm1.hgbst_elm2hgbst_show = {0}
							AND parentShow.objid = mtm1.hgbst_show2hgbst_elm
							AND 
							(
								parentShow.objid = 
								(
									SELECT childShow.chld_prnt2hgbst_show
									FROM table_hgbst_show childShow, mtm_hgbst_elm0_hgbst_show1 mtm2
									WHERE childShow.objid = mtm2.hgbst_show2hgbst_elm
									AND mtm2.hgbst_elm2hgbst_show = mtm1.hgbst_elm2hgbst_show
									AND childShow.objid != parentShow.objid
								)
								OR
									1 = 
									(
										SELECT COUNT(*) 
										FROM mtm_hgbst_elm0_hgbst_show1 mtm3 
										WHERE mtm3.hgbst_elm2hgbst_show = mtm1.hgbst_elm2hgbst_show
									)
								OR 
									parentShow.chld_prnt2hgbst_show IS NULL 						
							)
					)
	
					AND 
					(
						(show1A.chld_prnt2hgbst_show IS NULL AND show1.objid = show1A.objid)
						OR
						(show1.objid = show1A.chld_prnt2hgbst_show)
					)
				)
				AND 
				(
					(show2A.chld_prnt2hgbst_show IS NULL AND show2.objid = show2A.objid)
					OR
					(show2.objid = show2A.chld_prnt2hgbst_show)
				)
			)
			AND 
			(
				(show3A.chld_prnt2hgbst_show IS NULL AND show3.objid = show3A.objid)
				OR
				(show3.objid = show3A.chld_prnt2hgbst_show)
			)
		)
		AND 
		(
			(show4A.chld_prnt2hgbst_show IS NULL AND show4.objid = show4A.objid)
			OR
			(show4.objid = show4A.chld_prnt2hgbst_show)
		)
	)
	AND 
	(
		(show5A.chld_prnt2hgbst_show IS NULL AND show5.objid = show5A.objid)
		OR
		(show5.objid = show5A.chld_prnt2hgbst_show)
	)
)";

			#endregion

			var sqlHelper = new SqlHelper(listByElementObjidSql);
			sqlHelper.Parameters.Add("elmobjid", elmObjid);

			var result = sqlHelper.ExecuteScalar();

			if (result == DBNull.Value || result == null)
			{
				// No results returned. This is usually caused by:
				// 1.) The element does not exist
				// 2.) The element is in a show hierarchy that is disconnected
				// 3.) There is some corrupt data up the hierarchy

				// Check to see if the element doesn't exists first (the most common case)
				sqlHelper = new SqlHelper("SELECT objid FROM table_hgbst_elm WHERE objid = {0}");
				sqlHelper.Parameters.Add("elmObjid", elmObjid);

				result = sqlHelper.ExecuteScalar();

				if (result == DBNull.Value || result == null)
				{
					Console.WriteLine("No HGBST element found with objid '{0}'.", elmObjid);
					return;
				}
				
				Console.WriteLine("Unable to retrieve LIST tree for element with objid '{0}'.", elmObjid);
				return;
			}

			var listObjid = Convert.ToInt32(sqlHelper.ExecuteScalar());

			DumpListForListObjid(listObjid);
		}

		#endregion

		#region DumpListForShowObjid

		public static void DumpListForShowObjid(int showObjid)
		{
			#region Sql statement to get the list of a show by the show's objid

			const string listByShowObjidSql = @"
SELECT objid FROM table_hgbst_lst WHERE hgbst_lst2hgbst_show = 
(
	SELECT show5.objid FROM table_hgbst_show show5, table_hgbst_show show5A
	WHERE show5A.objid IN 
	(
		SELECT show4.objid FROM table_hgbst_show show4, table_hgbst_show show4A
		WHERE show4A.objid IN 
		(
			SELECT show3.objid FROM table_hgbst_show show3, table_hgbst_show show3A
			WHERE show3A.objid IN 
			(
				SELECT show2.objid FROM table_hgbst_show show2, table_hgbst_show show2A
				WHERE show2A.objid IN 
				(
					SELECT show1.objid FROM table_hgbst_show show1
					WHERE show1.objid = {0}
				)
				AND 
				(
					(show2A.chld_prnt2hgbst_show IS NULL AND show2.objid = show2A.objid)
					OR
					(show2.objid = show2A.chld_prnt2hgbst_show)
				)
			)
			AND 
			(
				(show3A.chld_prnt2hgbst_show IS NULL AND show3.objid = show3A.objid)
				OR
				(show3.objid = show3A.chld_prnt2hgbst_show)
			)
		)
		AND 
		(
			(show4A.chld_prnt2hgbst_show IS NULL AND show4.objid = show4A.objid)
			OR
			(show4.objid = show4A.chld_prnt2hgbst_show)
		)
	)
	AND 
	(
		(show5A.chld_prnt2hgbst_show IS NULL AND show5.objid = show5A.objid)
		OR
		(show5.objid = show5A.chld_prnt2hgbst_show)
	)
)";
			var sqlHelper = new SqlHelper(listByShowObjidSql);
			sqlHelper.Parameters.Add("showObjid", showObjid);

			var result = sqlHelper.ExecuteScalar();

			if (result == DBNull.Value || result == null)
			{
				// No results returned. This is usually caused by:
				// 1.) The show does not exist
				// 2.) The show is in a show hierarchy that is disconnected
				// 3.) There is some corrupt data up the hierarchy

				// Check to see if the show doesn't exists first (the most common case)
				sqlHelper = new SqlHelper("SELECT objid FROM table_hgbst_show WHERE objid = {0}");
				sqlHelper.Parameters.Add("showObjid", showObjid);

				result = sqlHelper.ExecuteScalar();

				if (result == DBNull.Value || result == null)
				{
					Console.WriteLine("No HGBST show found with objid '{0}'.", showObjid);
					return;
				}
				
				Console.WriteLine("Unable to retrieve LIST tree for show with objid '{0}'.", showObjid);
				return;
			}

			var listObjid = Convert.ToInt32(sqlHelper.ExecuteScalar());

			DumpListForListObjid(listObjid);

			#endregion
		}

		#endregion

		#region CopyAndReparentElement

		public static void CopyAndReparentElement(int elmObjid, int childShowObjid, int newParentShowObjid)
		{
			var prov = DbProviderFactory.Provider;

			using (var connection = prov.GetConnection())
			{
				connection.Open();

				using (var transaction = connection.BeginTransaction())
				{
					var batch = new UpdateQueryBatch(transaction, prov);
					CopyAndReparentElement(batch, elmObjid, childShowObjid, newParentShowObjid);
					var ret = batch.Execute();
					log.LogDebug("Execute query batch. Result: {0}", ret);
					transaction.Commit();
				}
			}

			Console.WriteLine("Copy and Reparent element successful");
		}

		private static void CopyAndReparentElement(UpdateQueryBatch batch, int elmObjid, int childShowObjid,
		                                           int newParentShowObjid)
		{
			log.LogDebug("CopyAndReparentElement called. Elmenet: {0}, Child Show: {1}, New Parent Show: {2}",
			             elmObjid, childShowObjid, newParentShowObjid);

			// Get table ID's for hgbst_elm and hgbst_show
			var sqlHelper = new SqlHelper(batch.Provider)
			                {
			                	Transaction = batch.Transaction,
			                	CommandText = @"SELECT type_id, type_name FROM adp_tbl_name_map WHERE type_name = 'hgbst_elm' OR type_name = 'hgbst_show'"
			                };

			var hgbstElmTableID = 0;
			var hgbstShowTableID = 0;

			using (var dataReader = sqlHelper.ExecuteReader())
			{
				while (dataReader.Read())
				{
					if (String.Compare(Convert.ToString(dataReader["type_name"]), "hgbst_elm", true) == 0)
					{
						hgbstElmTableID = Convert.ToInt32(dataReader["type_id"]);
					}
					else if (String.Compare(Convert.ToString(dataReader["type_name"]), "hgbst_show", true) == 0)
					{
						hgbstShowTableID = Convert.ToInt32(dataReader["type_id"]);
					}
				}
			}

			sqlHelper = new SqlHelper(batch.Provider)
			            {
			            	Transaction = batch.Transaction,
			            	CommandText = "SELECT site_id FROM adp_db_header"
			            };
			var siteId = Convert.ToInt32(sqlHelper.ExecuteScalar());

			var objidBase = siteId*Convert.ToInt32(Math.Pow(2, 28));
			//this.objidBase = 0;

			// Duplicate the element
			var dupeElmObjid = DuplicateElement(batch, elmObjid, newParentShowObjid, objidBase, hgbstElmTableID);
			log.LogDebug("Duplicated element. New Objid: {0}", dupeElmObjid);

			// Duplicate the child show and link it to the new element
			var dupeChildShowObjid = DuplicateShow(batch, childShowObjid, dupeElmObjid, newParentShowObjid, objidBase, hgbstShowTableID);
			log.LogDebug("Duplicated child show. New Objid: {0}", dupeChildShowObjid);

			// Get the whole child hierarchy for this element starting from its childShowObjid and duplicate it
			log.LogDebug("Duplicating child hierarchy...");
			DuplicateChildHierarchy(batch, elmObjid, childShowObjid, dupeChildShowObjid, objidBase, hgbstElmTableID,
			                        hgbstShowTableID);

			// Unlink the old elm from the new parent show (if it was linked - a la the Best Buy double-linked problem)
			log.LogDebug("Unlinking original element (if necessary) from new parent to complete the separation");
			var commandParameters = new DataParameterCollection {{"elmObjid", elmObjid}, {"newParent", newParentShowObjid}};
			batch.AddStatement(@"DELETE FROM mtm_hgbst_elm0_hgbst_show1 WHERE hgbst_elm2hgbst_show = {0} and hgbst_show2hgbst_elm = {1}", commandParameters);
		}

		private static int DuplicateElement(UpdateQueryBatch batch, int curElmObjid, int newParentShowObjid, int objidBase,
		                                    int hgbstElmTableID)
		{
			var sqlHelper = new SqlHelper(batch.Provider)
			             {
			             	Transaction = batch.Transaction,
			             	CommandText = @"SELECT * FROM table_hgbst_elm WHERE objid = {0}"
			             };
			sqlHelper.Parameters.Add("curElmObjid", curElmObjid);

			var newObjid = GetNextObjid(batch, hgbstElmTableID, objidBase);

			var elmSet = sqlHelper.ExecuteDataSet();

			if (elmSet == null || elmSet.Tables.Count != 1 || elmSet.Tables[0].Rows.Count != 1)
			{
				throw new ApplicationException("Element with objid " + curElmObjid + " not found, or more than one row with that objid exists");
			}

			var elementRow = elmSet.Tables[0].Rows[0];

			var commandParameters = new DataParameterCollection
									{
			                        	{batch.GetUniqueParamName("objid"), newObjid},
			                        	{batch.GetUniqueParamName("title"), elementRow["title"]},
			                        	{batch.GetUniqueParamName("rank"), elementRow["rank"]},
			                        	{batch.GetUniqueParamName("state"), elementRow["state"]},
			                        	{batch.GetUniqueParamName("intval1"), elementRow["intval1"]}
			                        };
			batch.AddStatement(@"INSERT INTO table_hgbst_elm (objid, title, rank, state, dev, intval1) VALUES ({0}, {1}, {2}, {3}, NULL, {4})", commandParameters);

			// Now relate it to the new parent
			commandParameters = new DataParameterCollection
			                    {
			                    	{batch.GetUniqueParamName("newObjid"), newObjid},
			                    	{batch.GetUniqueParamName("newParentShowObjid"), newParentShowObjid}
			                    };
			batch.AddStatement( @"INSERT INTO mtm_hgbst_elm0_hgbst_show1 (hgbst_elm2hgbst_show, hgbst_show2hgbst_elm) VALUES ({0}, {1})", commandParameters);

			return newObjid;
		}


		private static int DuplicateShow(UpdateQueryBatch batch, int curShowObjid, int parentElmObjid, int parentShowObjid, int objidBase, int hgbstShowTableID)
		{
			var sqlHelper = new SqlHelper(batch.Provider)
			             {
			             	Transaction = batch.Transaction,
			             	CommandText = @"SELECT * FROM table_hgbst_show WHERE objid = {0}"
			             };
			sqlHelper.Parameters.Add("curShowObjid", curShowObjid);

			var newObjid = GetNextObjid(batch, hgbstShowTableID, objidBase);

			var showSet = sqlHelper.ExecuteDataSet();

			if (showSet == null || showSet.Tables.Count != 1 || showSet.Tables[0].Rows.Count != 1)
			{
				throw new ApplicationException("Show with objid " + curShowObjid + " not found, or more than one row with that objid exists");
			}

			var showRow = showSet.Tables[0].Rows[0];

			var cmdParams = new DataParameterCollection
			                {
			                	{batch.GetUniqueParamName("objid"), newObjid},
			                	{batch.GetUniqueParamName("title"), showRow["title"]},
			                	{batch.GetUniqueParamName("def_val"), showRow["def_val"]},
			                	{batch.GetUniqueParamName("chld_prnt2hgbst_show"), parentShowObjid}
			                };
			batch.AddStatement(String.Format(
@"
INSERT INTO table_hgbst_show (objid, last_mod_time, title, def_val, dev, chld_prnt2hgbst_show)
VALUES ({{0}}, {0}, {{1}}, {{2}}, NULL, {{3}})",
					batch.Provider.GetDateStatement()), cmdParams);

			// Now relate it to the new parent
			cmdParams = new DataParameterCollection
			            {
			            	{batch.GetUniqueParamName("parentElmObjid"), parentElmObjid},
			            	{batch.GetUniqueParamName("newShowObjid"), newObjid}
			            };
			batch.AddStatement(@"INSERT INTO mtm_hgbst_elm0_hgbst_show1 (hgbst_elm2hgbst_show, hgbst_show2hgbst_elm) VALUES ({0}, {1})", cmdParams);


			return newObjid;
		}

		private static int GetNextObjid(UpdateQueryBatch batch, int tableID, int objidBase)
		{
			var sqlHelper = new SqlHelper(batch.Provider, CommandType.StoredProcedure, "fc_new_oid") {Transaction = batch.Transaction};

			sqlHelper.Parameters.Add("type_num", ConvertType(batch.Provider, tableID));
			sqlHelper.Parameters.Add("num_ids", ConvertType(batch.Provider, 1));
			sqlHelper.Parameters.Add("out_num", ConvertType(batch.Provider, 0));
			sqlHelper.Parameters["out_num"].Direction = ParameterDirection.Output;

			sqlHelper.ExecuteNonQuery();

			return objidBase + Convert.ToInt32(sqlHelper.Parameters["out_num"].Value);
		}

		internal static object ConvertType(DbProvider provider, object val)
		{
			var objType = provider.TypeMapper.MapTypeToDbType(val.GetType());

			return Convert.ChangeType(val, objType);
		}

		private static void DuplicateChildHierarchy(UpdateQueryBatch batch, int curElmObjid, int oldChildShowObjid,
		                                            int newChildShowObjid, int objidBase, int hgbstElmTableID,
		                                            int hgbstShowTableID)
		{
			DuplicateElementsForShow(batch, curElmObjid, oldChildShowObjid, newChildShowObjid, objidBase, hgbstElmTableID,
			                         hgbstShowTableID);
		}

		private static void DuplicateElementsForShow(UpdateQueryBatch batch, int parentElmObjid, int showObjid,
		                                             int newShowObjid, int objidBase, int hgbstElmTableID,
		                                             int hgbstShowTableID)
		{
			log.LogDebug("Duplicating elements for show. Parent Element: {0}, Show: {1}, New Parent Show: {2}",
			             parentElmObjid, showObjid, newShowObjid);

			var sqlHelper = new SqlHelper(batch.Provider)
			                {
			                	Transaction = batch.Transaction,
			                	CommandText =
			                		@"
SELECT e.*
FROM table_hgbst_elm e, mtm_hgbst_elm0_hgbst_show1 mtm
WHERE e.objid = mtm.hgbst_elm2hgbst_show
AND mtm.hgbst_show2hgbst_elm = {0}
AND mtm.hgbst_elm2hgbst_show != {1}"
			                };

			sqlHelper.Parameters.Add("showObjid", showObjid);
			sqlHelper.Parameters.Add("parentElmObjid", parentElmObjid);

			var elmSet = sqlHelper.ExecuteDataSet();

			foreach (DataRow elmRow in elmSet.Tables[0].Rows)
			{
				var oldElmObjid = Convert.ToInt32(elmRow["objid"]);

				log.LogDebug("Duplicating child element {0}", oldElmObjid);

				var newElmObjid = DuplicateElement(batch, Convert.ToInt32(oldElmObjid), newShowObjid, objidBase, hgbstElmTableID);

				// Look for sub-shows/levels and dupe those too
				sqlHelper = new SqlHelper(batch.Provider)
				            {
				            	Transaction = batch.Transaction,
				            	CommandText = @"
SELECT * 
FROM mtm_hgbst_elm0_hgbst_show1 
WHERE hgbst_elm2hgbst_show = {0}
AND hgbst_show2hgbst_elm != {1}"
				            };

				sqlHelper.Parameters.Add("elmObjid", oldElmObjid);
				sqlHelper.Parameters.Add("showObjid", showObjid);

				var showSet = sqlHelper.ExecuteDataSet();

				if (showSet == null || showSet.Tables.Count <= 0 || showSet.Tables[0].Rows.Count <= 0) continue;

				var showObjidToDupe = Convert.ToInt32(showSet.Tables[0].Rows[0]["hgbst_show2hgbst_elm"]);

				log.LogDebug("Duplicating child show {0}", showObjidToDupe);

				var newSubChildShowObjid = DuplicateShow(batch, showObjidToDupe, newElmObjid, newShowObjid, objidBase, hgbstShowTableID);

				log.LogDebug("New dupe child show create ({0}). Duplicating elements for original show ({1})", newSubChildShowObjid, showObjidToDupe);

				DuplicateElementsForShow(batch, oldElmObjid, showObjidToDupe, newSubChildShowObjid, objidBase, hgbstElmTableID, hgbstShowTableID);
			}
		}

		#endregion
	}
}