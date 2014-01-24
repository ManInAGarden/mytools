using System;
using System.Diagnostics;
using System.Collections;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Reflection;

namespace HSp.CsEpo
{
	/// <summary>
	/// EpoGeneric dient dazu Zugriff auf Daten einer Datenbank zu erlangen. Dazu werden hier der Tabellenname,
	/// die Spalten und deren Typen definiert. Fuer jede Spalte muss eine Public Property angelegt werden auf die sich alle
	/// weiteren Definitionen beziehen.
	/// </summary>
	public class EpoGeneric : Epo
	{
		
		public EpoGeneric()
		{
		}


		public EpoGeneric(string connString) : this(connString, stdOrderKey)
		{
		}

		public EpoGeneric(string connString, string orderKey)
		{
			this.orderKey = orderKey;
			InitializeStorage();

			InitDbConnection(connString);

		}


		/// <summary>
		/// Definiert dei Haupttabelle einer EpoGeneric-Klasse
		/// </summary>
		/// <param name="name"></param>
		protected override void SetMainTable(string name) 
		{
			EpoGenericClassInfo egc;

			egc = ClassInfo() as EpoGenericClassInfo;

			egc.tableInfo = new EpoGenericTableInfo(name);
		}


		/// <summary>
		/// Fügt eine verknüpfte Tabelle hinzu
		/// </summary>
		/// <param name="name">Der Name der Tabelle</param>
		/// <param name="whereClause">Die Whereclause mit der die Anknüpfung an die Haupttabell erfolgt</param>
		protected void AddLinkedTable(string name, string whereClause) 
		{
			EpoGenericClassInfo egc;

			egc = ClassInfo() as EpoGenericClassInfo;

			egc.tableInfo.AddLinkedTable(name, whereClause);
		}



		/// <summary>
		/// Fügt eine verknüpfte Tabelle hinzu
		/// </summary>
		/// <param name="name">Der Name der Tabelle</param>
		/// <param name="whereClause">Die Whereclause mit der die Anknüpfung an die Haupttabell erfolgt</param>
		protected void AddLinkedTable(string name, string whereClause, string alias) 
		{
			EpoGenericClassInfo egc;

			egc = ClassInfo() as EpoGenericClassInfo;

			egc.tableInfo.AddLinkedTable(name, whereClause, alias);
		}



		/// <summary>
		/// Liefert die Position des Feldnamens im string zurück. Dabei wird der Name
		/// so gesucht dass er nicht bereits durch eine Tabellenbezeichnung qualifiziert wurde
		/// </summary>
		/// <param name="clause">Der zu untersuchende String</param>
		/// <param name="field">Der Feldname</param>
		/// <returns></returns>
		protected int IndexOfPureField(string clause, string field) 
		{
			int		ind = -1;
			char[]	seps = {' ', '(', ')', '='};
			string	searchClause = clause.ToUpper(),
					searchField = field.ToUpper(),
					tstStr;
			char	lsep, rsep;
			
			

			for(int i=0; (ind<0) && (i<seps.Length); i++) 
			{
				lsep = seps[i];
				for(int j=0; (ind<0) && (j<seps.Length); j++)
				{
					rsep = seps[j];
					tstStr = lsep + searchField + rsep;
					
					//Mittendrin prüfen
					ind = searchClause.IndexOf(tstStr);
					if(ind>=0) 
						ind++;
					else {  //Anfang checken
						ind = searchClause.IndexOf(searchField + rsep);
						if(ind!=0)
							ind = -1;
					}
					
				}

				if(ind<0)
				{
					//Ende prüfen
					ind = searchClause.IndexOf(lsep + searchField);
				
					if((ind >= 0) && (searchClause.Length - searchField.Length - 1) == ind) {
						ind++;
					} else
						ind = -1;
				}
			}
			

			return ind;
		}

		/// <summary>
		/// Ergänzt die Where clause so dass alle Spaltenangaben um den
		/// Tabellennamen erweitert sind. Bereits erweiterte Namen werden so
		/// belassen, wie sie angegeben wurden.
		/// </summary>
		/// <param name="inWhere">Die Eingangs-Where-Clause</param>
		/// <returns>Aufbreitete WhereClause</returns>
		protected string AmendedWhere(string inWhere) 
		{
			string					answ = inWhere;
			EpoGenericClassInfo		gci;
			IDictionaryEnumerator	enu;
			int						ind;

			gci = ClassInfo() as EpoGenericClassInfo;

			if(gci.tableInfo.linkedFields!=null) 
			{
				enu = (gci.tableInfo.linkedFields).GetEnumerator();
				while(enu.MoveNext()) 
				{
					ind = IndexOfPureField(answ, enu.Key as String);
					if(ind>=0) 
					{
						answ = answ.Remove(ind, (enu.Key as String).Length);
						answ = answ.Insert(ind, enu.Value as string);
					}
				}
			}

			enu = gci.fieldMap.GetEnumerator();
			while(enu.MoveNext()) 
			{
				ind = IndexOfPureField(answ, enu.Key as String);
				if(ind>=0) 
				{
					answ = answ.Remove(ind, (enu.Key as String).Length);
					answ = answ.Insert(ind, gci.tableInfo.mainTable + "." + enu.Key as string);
				}
			}

			return answ;
		}
	
		/// <summary>
		/// Fügt eine Feld auf einer Verlinkten Tabelle hinzu. Die Tabelle muss zuvor mit AddLinkedTable
		/// hizugefügt worden sein.
		/// </summary>
		/// <param name="fname">Der Feldname</param>
		/// <param name="tname">Der Tabellennam</param>
		/// <param name="originalName">Der Orignalfeldname in der Tabelle aus Tabellenname</param>
		protected void AddLinkedField(string fname, string tname, string originalName) 
		{
			EpoGenericClassInfo egc;

			egc = ClassInfo() as EpoGenericClassInfo;

			egc.tableInfo.AddLinkedField(fname, tname, originalName);
		}


		protected string GetMainTable() 
		{
			EpoGenericClassInfo egc;
			string				answ;
			EpoGenericTableInfo	tableInfo;
			


			egc = ClassInfo() as EpoGenericClassInfo;

			tableInfo = egc.tableInfo; 
			if (tableInfo != null)
				answ = tableInfo.mainTable;
			else
				answ = null;

			return answ;
		}


		protected override bool SetupClassInfo() 
		{
			bool					answ;
			EpoGenericClassInfo		eci;
			String					clName;

			clName = GetType().Name;
			eci = ClassInfo() as EpoGenericClassInfo;

			if(eci == null) 
			{
				answ = true;
				eci = new EpoGenericClassInfo();
				eci.className = clName;
				eci.fullClassName = GetType().AssemblyQualifiedName;
				SetNewClassInfo(eci);
			} 
			else 
			{
				answ = false;
			}


			return answ;
		}


		


		//Wir bauen hier unsere eigene Datenbankverbundung auf und Initialisieren alles selbst anhand eines
		//vorgefertigten Statements. Dabei kommen wir auch ohne oid aus. (hoffentlich)
		protected override void InitDbConnection(String connStr) 
		{
			EpoGenericClassInfo	eci;
			bool				haveToWork = false;
			Epo					kv;


			//Wenn es dazu schon ein Verwaltungsobjekt gibt, das connected ist - mach nix
			kv = classInfos.GetKnownVerwo(orderKey, this.GetType().Name);
			if (kv != null) 
			{
				eci = kv.ClassInfo() as EpoGenericClassInfo;
				if(eci!=null) 
				{
					if(eci.odbcConn.State==System.Data.ConnectionState.Open) return;
				}
			}

			haveToWork = SetupClassInfo();

			eci = ClassInfo() as EpoGenericClassInfo;
			if (tsw.TraceVerbose) 
			{
				Trace.WriteLine(String.Format("Initialisierung der Epoklasse <{0}> mit: {1}",
					eci.fullClassName,
					connStr));
			}

			eci.odbcConn = GetOdbcConnection(connStr);
			

			connected = eci.odbcConn != null;

			try
			{
				classInfos.SetKnownVerwo(orderKey, this.GetType().Name, this);
			}
			catch (Exception exc)
			{
				Trace.WriteLine("Die EPO-Klasse " + this.GetType().Name + " wurde mehrfach initialisiert.");

			}

			if(!haveToWork)
				return;

			InitMyFieldMap();
			InitSpecialDbLen();

		}


		public override bool Delete()
		{
			String    			myStmt;
			String    			myClsName;
			bool      			done;
			string				myTableName;
			EpoGenericClassInfo	eci;


			eci = ClassInfo() as EpoGenericClassInfo;
			myTableName = eci.tableInfo.mainTable;

            eci.odbcConn = OpenDbConnection(eci.odbcConn);
            try
            {
                done = HandleDeleteRules(this);
                if (!done) return false;

                myClsName = GetType().Name;

                myStmt = "DELETE FROM "
                    + myTableName
                    + " WHERE "
                    + KeyEqalityExpression()
                    + ";";


                done = ExStmt(myStmt);
                if (!done)
                    Trace.WriteLine(String.Format("Delete failed for Class <{0}>, table <{1}>, object <{2}>",
                        myClsName,
                        myTableName,
                        KeyEqalityExpression()));
            }
            finally
            {
                CloseDbConnection(eci.odbcConn);
            }

			return done;
			
		}


		/// <summary>
		/// Bildet den UpdateString für EpoGeneric
		/// </summary>
		/// <returns></returns>
		protected override String MyUpdateString() 
		{
			string	answ;
			int		oidEqualsIdx, nextTickIdx;
			string	offendingStr;

			answ = base.MyUpdateString();
			
			//Alles ist genauso wie in Epo - allerdings muss die oid Zuweisung wegfallen
			oidEqualsIdx = answ.IndexOf("oid='");
			nextTickIdx = answ.Substring(oidEqualsIdx + 6).IndexOf("'") + oidEqualsIdx + 8; //Das Komma mitnehmen

			offendingStr = answ.Substring(oidEqualsIdx, nextTickIdx-oidEqualsIdx);

			
			return answ.Replace(offendingStr, "");
		}


		public override void Flush() 
		{
			///Versuch zuerst ein UPDATE
			///Wenn das nicht klappt dann versuch ein INSERT
			///Wenn das auch nicht klappt: Au Weia!
			EpoGenericClassInfo	eci;
			String				myStmt;
			bool				done;
			string				mainTab;

			eci = ClassInfo() as EpoGenericClassInfo;

			mainTab = eci.tableInfo.mainTable;
			
			Debug.Assert(mainTab!=null,"Es wurde keine Haupttabelle in der Epo-Klasse <" 
				+ eci.className 
				+ "> definiert.");

			Debug.Assert(mainTab.Length > 0,"Es wurde keine Haupttabelle in der Epo-Klasse <" 
				+ eci.className 
				+ "> definiert.");

			

			//Versuche zuerst mal den Update
			myStmt = "UPDATE "
				+  mainTab
				+ " SET "
				+  MyUpdateString()
				+ " WHERE "
				+ KeyEqalityExpression()
				+ ";";

            eci.odbcConn = OpenDbConnection(eci.odbcConn);
            try
            {
                done = ExStmt(myStmt, 1);
                if (!done)
                {

                    myStmt = "INSERT INTO "
                        + mainTab
                        + "( "
                        + MyProperties()
                        + ") "
                        + "VALUES ( "
                        + MyValues()
                        + ");";

                    done = ExStmt(myStmt);
                }
            }
            finally
            {
                CloseDbConnection(eci.odbcConn);
            }
		
		}



		protected override String MyValues() 
		{
			EpoGenericClassInfo		eci;
			String            		myClsName;
			IDictionaryEnumerator	idictEnum;
			String            		answ = "";
			int               		komPos;
			FieldInfo				fi;


			// Im Gegensatz zum Orignal aus Epo muss hier die oid weggelassen werden

			myClsName = GetType().Name;
			eci = ClassInfo() as EpoGenericClassInfo;
			idictEnum = eci.fieldMap.GetEnumerator();
			idictEnum.Reset();
			while(idictEnum.MoveNext()) 
			{
				if((idictEnum.Key as string) != "oid") 
				{
					fi = idictEnum.Value as FieldInfo;
					if (fi.persist) 
					{
						answ = answ + MyValue(idictEnum.Key as String);
						answ = answ + ", ";
					}
				}
			}

			if(answ.Length>2) 
			{
				komPos = answ.LastIndexOf(',');
				answ = answ.Remove(komPos, 2);
			}

			return answ;
		}



		/// <summary>
		/// Liefert den Teil der WhereClause der die Tabellen miteinander verlinkt
		/// </summary>
		/// <returns></returns>
		protected string MyLinkingWheres() 
		{
			string					answ = "";
			EpoGenericClassInfo		eci;
			IDictionaryEnumerator	enu;

			eci = ClassInfo() as EpoGenericClassInfo;
			if(eci.tableInfo.linkedTables!=null) 
			{
				enu = eci.tableInfo.linkedTables.GetEnumerator();
				while(enu.MoveNext()) 
				{
					answ += enu.Value as string + " AND ";
				}
			}

			if(answ.EndsWith(" AND ") )
			{
				answ = answ.Substring(0, answ.Length - 5);
			}
			
			return answ;
		}



		private bool IsLinkedField(string fieldName) 
		{
			EpoGenericClassInfo eci;

			eci = ClassInfo() as EpoGenericClassInfo;
			if(eci.tableInfo==null) return false;
			if(eci.tableInfo.linkedFields==null) return false;

			return eci.tableInfo.linkedFields.ContainsKey(fieldName);
		}


		/// <summary>
		/// Liefert die direkten Properties der Klasse als komma-separierten string
		/// </summary>
		/// <returns></returns>
		protected override string MyProperties() 
		{
			EpoGenericClassInfo		eci;
			String					myClsName;
			IDictionaryEnumerator   idictEnum;
			String					answ = "";
			int						komPos;
			FieldInfo				fi;

			// Im Gegensatz zu Epo muss hier die oid weggelassen werden weil diese zwar
			// vererbt wird aber in den Tabellen nicht unbedingt vorhanden ist.

			myClsName = GetType().Name;
			eci = ClassInfo() as EpoGenericClassInfo;
			idictEnum = eci.fieldMap.GetEnumerator();
			idictEnum.Reset();
			while(idictEnum.MoveNext()) 
			{
				if((idictEnum.Key as String) != "oid") 
				{
					fi = idictEnum.Value as FieldInfo;
					if (fi.persist && !IsLinkedField(idictEnum.Key as string)) 
					{
						answ = answ + fldOpenBr + (idictEnum.Key as String) + fldCloseBr;
						answ = answ + ", ";
					}
				}
			}


			if((eci.tableInfo.linkedTables.Count>0) || (eci.tableInfo.linkedFields!=null))
				answ = answ + MyConnectedProperties();

			if(answ.Length>2) 
			{
				komPos = answ.LastIndexOf(',');
				answ = answ.Remove(komPos, 2);
			}

			return answ;
		}



		private string MyTableNames() 
		{
			EpoGenericClassInfo		eci;
			string					answ = "";
			
			eci = ClassInfo() as EpoGenericClassInfo;
			answ = eci.tableInfo.mainTable;


			if(eci.tableInfo.linkedTables.Count>0) 
			{
				IDictionaryEnumerator	enu;

				enu = eci.tableInfo.linkedTables.GetEnumerator();
				while(enu.MoveNext()) 
				{
					if(eci.tableInfo.aliasNames.ContainsKey(enu.Key)) 
						answ += ", " + enu.Key + " " + eci.tableInfo.aliasNames[enu.Key] as string;
					else
						answ += ", " + enu.Key;
				}
			}

			return answ;
		}


		private string MyConnectedProperties() 
		{
			EpoGenericClassInfo		eci;
			String					myClsName;
			IDictionaryEnumerator   idictEnum;
			String					answ = "";


			myClsName = GetType().Name;
			eci = ClassInfo() as EpoGenericClassInfo;
			if(eci.tableInfo.linkedFields==null) return answ;

			idictEnum = eci.tableInfo.linkedFields.GetEnumerator();
			while(idictEnum.MoveNext()) 
			{
				answ += idictEnum.Value as string + " AS " + idictEnum.Key as string + ", ";
			}


			return answ;
		}


		/// <summary>
		/// Die Generic-Art des Select
		/// </summary>
		/// <param name="where"></param>
		/// <param name="orderBy"></param>
		/// <returns></returns>
		public override ArrayList InternalSelect(String where, String orderBy) 
		{
			String					myStmt;
			ArrayList				epoV = null;
			OdbcDataReader			res;
			EpoGenericClassInfo		eci;
			Epo						newO;
			String					ord;
			string					linkWhere;


			eci = ClassInfo() as EpoGenericClassInfo;
			
			myStmt = "SELECT "
				+ MyProperties()
				+ " FROM "
				+ MyTableNames();
      
			linkWhere = MyLinkingWheres();

			if((where!=null) || (linkWhere.Length>0)) 
			{
				myStmt += " WHERE ";
			}

			if(where!=null) 
			{
				myStmt += "("
					+ AmendedWhere(where) + ")";

				if(linkWhere.Length>0) 
					myStmt += " AND ";
			}

			if(linkWhere.Length>0) 
			{
				myStmt += "(" + linkWhere + ")";
			}

			if(orderBy==null)
				ord = PreferredOrdering();
			else
				ord = orderBy;
	 	
			if (ord != null) 
			{
				if(ord.Length>0)
					myStmt += " ORDER BY " + ord;
			}

			myStmt += ";";

			lock(eci.odbcConn) 
			{
                eci.odbcConn = OpenDbConnection(eci.odbcConn);
                try
                {
                    res = SelStmt(myStmt);
                    if (res != null)
                    {

                        try
                        {
                            epoV = new ArrayList();
                            while (res.Read())
                            {
                                newO = BuildGenericEpoFromData(res, eci);
                                newO.orderKey = orderKey;
                                epoV.Add(newO);
                            }
                        }
                        finally
                        {
                            res.Close();
                        }
                    }
                }
                finally
                {
                    CloseDbConnection(eci.odbcConn);
                }
			}


			return epoV;
		}


		//Ein EPO aus einem Datareader bauen der gerade auf den
		//zu benutzenden Daten steht.
		//Es erfolgt kein Next oder so....
		//Sondebehandlung fuer die oid, die hier aus den KEyFeldern zusammengesetzt wird
		protected Epo BuildGenericEpoFromData(OdbcDataReader res, EpoGenericClassInfo eci) 
		{
			Epo                     newO;
			IDictionaryEnumerator   fiEnum;
			PropertyInfo            propInfo;
			Object                  propO, dbO;
			ConstructorInfo         constrInfo;
			String                  currKey;
			FieldInfo               fi;
			Type                    myType;
			string					oidStr;
			string[]				keys;


			//Debug.WriteLine(String.Format("Erzeugung eines neuen Objekts der Klasse <{0}>",
			//                              eci.fullClassName));
      
			Debug.Indent();
			fiEnum = eci.fieldMap.GetEnumerator();
			myType = Type.GetType(eci.fullClassName);

			Debug.Assert(myType!=null);

			constrInfo = myType.GetConstructor(Type.EmptyTypes);
			newO = (Epo)constrInfo.Invoke(null);

			fiEnum.Reset();
			while(fiEnum.MoveNext()) 
			{
				currKey = fiEnum.Key as String;
				
				if (!currKey.Equals("oid")) 
				{

					fi = fiEnum.Value as FieldInfo;
					if(fi.persist) 
					{
						propInfo = myType.GetProperty(currKey);
						dbO = res[currKey];

						propO = MyTypedValue(dbO, fi);				
					
						propInfo.SetValue(newO,
							propO,
							null);
					}
				}
			}

			// Nun die oid zusammenbauen
			oidStr = "";
			keys = eci.KeyFieldArray();

			if (keys!=null) 
			{
				for(int i=0; i<keys.Length; i++) 
				{
					if (i>0) oidStr += "_";
					currKey = keys[i];
					oidStr += res[currKey];
				}

				oidStr = eci.className + "_" + oidStr;

				oid = new Oid(oidStr);
			}

			Debug.Unindent();
			
			return newO;

		}

		//Mit dem Schlüssel initialisieren
		public override String PreferredOrdering() 
		{
			string					answ = "";
			EpoGenericClassInfo		eci;
			string[]				keys;
	

			eci = ClassInfo() as EpoGenericClassInfo;
			keys = eci.KeyFieldArray();
			for(int i=0; i<keys.Length; i++) 
			{
				if (i>0) 
				{
					answ += ",";
				}

				if(!IsLinkedField(keys[i]))
					answ += keys[i];
				else
					answ += eci.tableInfo.linkedTables[keys[i]] as string;
			}

			return answ;
		}


		/// <summary>
		/// Fügt ein Key-Feld zur Schlüsselfeldverwaltung der EpoGenric Klasse hinzu
		/// </summary>
		/// <param name="fieldName">Der Name des Schlüsselfeldes</param>
		public void AddKeyField(string fieldName) 
		{
			EpoGenericClassInfo		eci;
	
			eci = ClassInfo() as EpoGenericClassInfo;
			eci.AddKeyField(fieldName);
		}


		/// <summary>
		/// Setzt ein Feld als Key-Feld zur Schlüsselfeldverwaltung der EpoGenric Klasse
		/// Alle bislang vorhandenen geerbten Schlüssel werden dabei gelöscht!!!
		/// </summary>
		/// <param name="fieldName">Der Name des Schlüsselfeldes</param>
		public void SetKeyField(string fieldName) 
		{
			EpoGenericClassInfo		eci;
	
			eci = ClassInfo() as EpoGenericClassInfo;
			eci.SetKeyField(fieldName);
		}


	}
}
