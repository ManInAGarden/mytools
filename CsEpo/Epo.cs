namespace HSp.CsEpo 
{

	using System;
	using System.Data;
	using System.Xml;
	using System.Diagnostics;
	using System.Collections;
	using System.Reflection;
	using System.Data.Odbc;
	using System.Globalization;
	using System.IO;


	public enum EnumDbMode 
	{
		MsAccess = 0,
		Oracle = 1
	}

	/// <summary>
	/// Epo ist die Basis für alle persisitenten Klassen. Die Properties von Epo
	/// und allen abgeleiteten Klassen werden als persistente Attribute
	/// betrachtet.
	/// </summary>
	public class Epo : ICloneable 
	{   
		private Oid							myOid;
		public string						orderKey; //Ordungsbegriff für WebServeranwendung die alle User durcheindaner werfen
		protected static EpoClassInfoStore	classInfos;
		private static CultureInfo			usCulture = new CultureInfo("en-US", false);
		protected bool						connected = false;
		protected static Hashtable			transactionStore = new Hashtable();
		public static TraceSwitch			tsw = new TraceSwitch("Epo", "Epo Trace Level");
		protected static string				fldCloseBr = "]";
		protected static string				fldOpenBr = "[";
		protected static string				stdOrderKey = "*";
		public static EnumDbMode			dbMode = EnumDbMode.MsAccess;



		#region properties
		///Achtung alle Properties werden unter ihrem Namen persistent als Spalten
		///in der Datenbank gespeichert. Also füge hier nix mehr hinzu!!!

		public Oid oid 
		{
			set 
			{
				myOid = value;
			}
			get 
			{
				return myOid;
			}
		}


		#endregion

		#region constructors

		///<summary>
		///Der Standardkonstruktor. Hier wird ein leerer Datensatz angelegt
		///Dazu muss zuvor mindestend einmal eine Epo(connStr) aufgerufen worden
		///sein. D.h die Datenbankverbindung der Klasse muss zuerst gemacht
		///werden.
		//</summary>
		public Epo() 
		{ 	    	 	
			orderKey = stdOrderKey;
			myOid = new Oid(this);
		}

		///<summary>
		///Dieser Konstruktor merkt sich die Connection in einer
		///statischen Variable. Er sollte daher je Klasse nur einmal benutzt
		///werden.
		///</summary>
		public Epo(String connString) : this(connString, stdOrderKey)
		{
		}


		///<summary>
		///Dieser Konstruktor merkt sich die Connection in einer
		///statischen Variable. Zusätzlich kann ein Ordnungsbegriff genutzt werden. 
		///Er kann je Klasse mehrfach benutzt werden solange sich der Ordnungsbegriff jeweils
		///unterscheidet
		///werden.
		///</summary>
		public Epo(string connString, string orderK)
		{

			myOid = new Oid(this);

			orderKey = orderK;
			InitializeStorage();
			InitDbConnection(connString);
		}



		public static void SetStdOrderKey(string ok) 
		{
			stdOrderKey = ok;
		}

		

		public static string GetStdOrderKey() 
		{
			return stdOrderKey;
		}


		public static void SetDbMode(EnumDbMode setDbMode) 
		{
			dbMode = setDbMode;
			switch (setDbMode) 
			{
				case EnumDbMode.MsAccess:
					fldOpenBr = "[";
					fldCloseBr = "]";
					break;
				case EnumDbMode.Oracle:
					fldOpenBr = "";
					fldCloseBr = "";
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dsnstr"></param>
		/// <returns></returns>
		private string ExtractUserName(string dsnstr) 
		{
			string[]	comps;
			string		answ = "";
			char[]		seps = {';'};
			int			uidPos, eqPos;

			uidPos = dsnstr.ToUpper().IndexOf("UID");
			
			if(uidPos<0) return null;

			comps = dsnstr.Split(seps);
			foreach(string comp in comps) 
			{
				uidPos = dsnstr.ToUpper().IndexOf("UID");
				if (uidPos>=0) 
				{
					eqPos = comp.IndexOf("=");
					if(eqPos>=0) 
					{
						answ = comp.Substring(eqPos + 1);
						break;
					}
				}
			}

			return answ;
		}

		private object ParseValue(string key, string value, FieldInfo fi) 
		{
			object		answ = null;
		
		
			Debug.Assert(fi != null);
			Debug.Assert(key != null);
			Debug.Assert(value != null);
			Debug.Assert(fi.persist);
		
			if (value.Length == 0) return null;
			if (value.Equals("NULL")) return null;
		
			switch (fi.csTypeName) 
			{
				case "String":
					if (value.Length > fi.size) 
					{
						throw new ApplicationException("Feldinhalt von " + key + " ueberschreitet die Datenbanklaenge. Wert wurde wurde verkürzt");
					}
				
					answ = value;
					break;
				case "int":
					answ = Int32.Parse(value);
					break;
				case "Int32":
					answ = Int32.Parse(value);
					break;
				case "DateTime":
					answ = DateTime.Parse(value);
					break;
				case "TimeSpan":
					answ = TimeSpan.Parse(value);
					break;
				case "Boolean":
					int bolInt;
					bolInt = Int32.Parse(value);
					answ = (bolInt==1);
					break;
				case "Float":
					Single.Parse(value);
					break;
				case "Double":
					Double.Parse(value);
					break;
				case "EpoMemo":
					answ = new EpoMemo(value);
					break;
				case "Oid":
					answ = new Oid(value);
					break;
				case "Decimal":
					answ = Decimal.Parse(value);
					break;
				default:
					String errstr = String.Format("Unbekannter Typ: <{0}. Kann persitenten Type nicht bestimmen>", fi.csType.Name);
					Trace.WriteLine(errstr);
					throw new ApplicationException(errstr);
			}
		
			return answ;
		}
	
	


		///<summary>
		///Erzeugt das EPO Objekt aus einem XmlNode. Gut fuer Importverfahren
		///Dabei wird die oid beibehalten!!!! Außerdem wird alles in die DB geflusht.
		///</summary>	
		public Epo CreateFromXmlNode(XmlNode xNode) 
		{
			return CreateFromXmlNode(xNode, true);
		}
	

		/// <summary>
		/// Erzeugt Knoten aus einem XmlNode.
		/// </summary>
		/// <param name="xNode"></param>
		/// <param name="doFlush">true flusht alle erzeugten Knoten in die DB, false erzeugt die Epos nur im Speicher</param>
		/// <returns></returns>
		public Epo CreateFromXmlNode(XmlNode xNode, bool doFlush) 
		{
			Epo						answ = null,
									cepo;
			XmlAttributeCollection	attrs;   	  
			string					oidstr;
			IEnumerator				attEnu;
			object					typedVal;
			EpoClassInfo			eci;
			XmlNode					linksNode;
			XmlAttribute			currAtt;

   	
   		
  		
   		
			//Zuerst das Objekt im XmlNode erzeugen/updaten
			attrs = xNode.Attributes;
			oidstr = attrs["oid"].Value;
			if (oidstr != null) 
			{
				answ = NewFromOidStr(oidstr);
				eci = answ.ClassInfo();
				Debug.Assert(eci != null);
				if(answ!=null) 
				{
					attEnu = attrs.GetEnumerator();
					attEnu.Reset();
					while (attEnu.MoveNext()) 
					{
						//Leider habe ich hier stets Strings so dass diese nun zunächst typgrecht gewandelt 
						//werden müssen
						currAtt = attEnu.Current as XmlAttribute;
					
						typedVal = ParseValue(currAtt.Name, currAtt.Value, eci.fieldMap[currAtt.Name] as FieldInfo);
						answ.SetPropValue(currAtt.Name, typedVal);
					}
				
				
					if (doFlush) answ.Flush(); //macht automatisch insert oder update
				}
			}
   	  	
			//Nun die Knoten in der Links-Auflistung bearbeiten
			//das funktioniert natuerlich rekursiv
   	  
			linksNode = xNode.FirstChild;
			if (linksNode != null) 
			{
				if (!linksNode.Name.Equals("Links")) 
				{
					throw new ApplicationException("Unbekannte XML-Struktur, <Links> ... </Links> wurde erwartet");
				}
   	  		
				if (linksNode.HasChildNodes) 
				{
					for (int i=0; i<linksNode.ChildNodes.Count; i++) 
					{
						cepo = CreateFromXmlNode(linksNode.ChildNodes[i], doFlush);
						//Hier koennte man noch kontrollieren, ob der erzeugte Kindknoten tatsächlich laut
						//Link auf den aktuellen Knoten verweist
					}
				}
			}
   	  	
			return answ;
		}
   
   
		///<summary>
		///Liefert das EPO Objekt als XmlNode. Gut fuer Exportverfahren
		///</summary>
		public XmlNode AppendXmlRepresentation(XmlNode par) 
		{
			XmlNode					answ;
			EpoClassInfo   			eci, targetEci;
			IDictionaryEnumerator	fiEnum, linkEnum;
			FieldInfo				fi;
			string					valStr, currKey,
				targetClassName;
			XmlDocument				doc;
			XmlNode					attNode;
			EpoLink					link;
			Epo						verwo;
			ArrayList				erg;
			XmlNode					linkParNode;
			Epo						ergEpo;
			bool 					hadErgs;   	   
   	      	   

	   
			doc = par.OwnerDocument;
			answ = doc.CreateNode(XmlNodeType.Element,
				oid.ToString(),
				null);
   	   						 
			//Nun die Attribute drunterhängen
			eci = ClassInfo();
   	   
			Trace.WriteLineIf(tsw.TraceVerbose, String.Format("XML-Bilung fuer {0}", 
				eci.className));
			fiEnum = eci.fieldMap.GetEnumerator();
	   
	   
			fiEnum.Reset();
			while(fiEnum.MoveNext()) 
			{
				
				currKey = fiEnum.Key as String;
			
				fi = fiEnum.Value as FieldInfo;
			
				if (fi.persist) 
				{
					if (fi.dbTypeName=="DATE") 
					{
						valStr = GetPropValue(currKey).ToString();
					} else
						valStr = MyValue(currKey) ;

					valStr = valStr.Replace("'", "");
					attNode = doc.CreateNode(XmlNodeType.Attribute,
						currKey,
						null);
					attNode.Value = valStr;
					answ.Attributes.Append(attNode as XmlAttribute);
				}
			}
	   
	   
			if (eci.linkMap.Count > 0) 
			{
				hadErgs = false;
				//Nun die Links verfolgen und ggf. Kinder anhaengen
				//Fuer jeden Link einen Subnode bilden
				linkEnum = eci.linkMap.GetEnumerator();
				linkEnum.Reset();
				linkParNode = doc.CreateElement("Links");
				while (linkEnum.MoveNext()) 
				{
					link = linkEnum.Value as EpoLink;
					targetClassName = linkEnum.Key as String;		
					targetEci = classInfos.GetClassInfo(orderKey, targetClassName);
					Debug.Assert(targetEci != null, 
						"Target-Eopklasse <" + targetClassName + "> existiert nicht, oder wurde nicht initialisiert");

					verwo = NewByClassName(orderKey, targetClassName);
					erg = verwo.Select(fldOpenBr + link.foreignFieldName + fldCloseBr + "='" + oid + "'");
					if (erg != null) 
					{
						for (int k=0; k<erg.Count; k++) 
						{
							ergEpo = erg[k] as Epo;
							ergEpo.AppendXmlRepresentation(linkParNode);	
							hadErgs = true;
						}
					}
				}
	   	
				if (hadErgs) answ.AppendChild(linkParNode);
	   	
			}
	   
	   
			par.AppendChild(answ);
	   
			return answ;
		}
  
		#endregion


		#region public members
		///<summary>
		///Bevorzugtes Attribut für Sortierungen. Überlade dies je Klasse
		///mit deren bevorzugter Sortierung.
		///</summary>
		public virtual String PreferredOrdering() 
		{
			return "oid";
		}


		protected void InitializeStorage() 
		{
			if(classInfos==null) 
			{
				classInfos = new EpoClassInfoStore(orderKey);
			}
		}

		//Erzeugt ein nagelneues Epo anhand eines oidStrings - nutze dies niemals !!!!
		//Nur fuer internen Gebrauch!!!!!!
		private Epo NewFromOidStr(string oidStr) 
		{
			Oid					newOid;
			string   			clsName;
			Type				myType;
			Epo					newO;
			ConstructorInfo		constrInfo;
			EpoClassInfo		eci;
   	   
			newOid = new Oid(oidStr);
			clsName = newOid.ClassName;
			Debug.Assert(clsName!=null);
   	   
			eci = classInfos.GetClassInfo(orderKey,clsName);
   	   
			Debug.Assert(eci!=null);
   	   
			myType = Type.GetType(eci.fullClassName);
       
			constrInfo = myType.GetConstructor(Type.EmptyTypes);
			newO = (Epo)constrInfo.Invoke(null);
			newO.orderKey = orderKey;
   	   
			return newO;
		}


		//Gibt die Bezeichnung zurück unter der die Klasse an der Oberfläche bezeichnet wird
		//Defualt=ClassName
		//Überlade dies für bessseres Verhalten
		public virtual String DisplayName() 
		{
			return this.GetType().Name;
		}
   
   
		///<summary>
		///Liefert ein neues EPO zurück, das anhand der oid die hier im String-Format übergeben wird,
		/// gefüllt wurde
		///</summary>
		public virtual Epo ResolveOid(string oidstr) 
		{
			Oid		oid;


			oid = new Oid(oidstr);
			Debug.Assert(oid != null);

			return ResolveOid(oid);
		}


		///<summary>
		///Liefert ein neues EPO zurück, das anhand der oid gefüllt wurde
		///</summary>
		public virtual Epo ResolveOid(Oid oid) 
		{
			Epo               newO = null;
			String            classToCreateName;
			EpoClassInfo      eci, myeci;
			OdbcDataReader    dr = null;
			String            cmdStr;
			bool              gotOne;


			if (oid==null) return null;
			if(oid.OidStr==null) return null;
			if(oid.OidStr.Length==0) return null;

			classToCreateName = oid.ClassName;
			eci = classInfos.GetClassInfo(orderKey, classToCreateName);
            myeci = ClassInfo();

			Debug.Assert(eci!=null);
            Debug.Assert(myeci != null);

			if (tsw.TraceVerbose) 
			{
				Trace.WriteLine(String.Format("Erzeuge neues Epo der Klasse <{0}>",
					eci.fullClassName));
			}

			cmdStr = "SELECT * FROM "
				+ eci.tableName
				+ " WHERE oid='"
				+ oid.OidStr
				+ "';";

			lock(eci.odbcConn) 
			{
                myeci.odbcConn = OpenDbConnection(myeci.odbcConn);
                try
                {
                    dr = SelStmt(cmdStr);

                    if (dr != null)
                    {
                        gotOne = dr.Read();
                        if (gotOne)
                        {
                            newO = BuildEpoFromData(dr, eci);
                            newO.orderKey = orderKey;
                        }
                    }
                }
                finally
                {
                    if (dr != null)
                        dr.Close();

                    CloseDbConnection(myeci.odbcConn);
                    
                }

			}


			return newO;
		}


		public override String ToString() 
		{
			if(myOid != null)
				return myOid.ToString();
			else
				return "./.";
		}

		public override bool Equals(Object o) 
		{
			if(o is Epo) 
			{
				Epo e = o as Epo;
				return myOid.Equals(e.oid);
			} 
			else 
			{
				return false;
			}
		}

		public override int GetHashCode() 
		{
			return myOid.GetHashCode();
		}

		#endregion


		#region private members
		


		//Ein EPO aus einem Datareader bauen der gerade auf den
		//zu benutzenden Daten steht.
		//Es erfolgt kein Next oder so....
		protected Epo BuildEpoFromData(OdbcDataReader res, EpoClassInfo eci) 
		{
			Epo                     newO;
			IDictionaryEnumerator   fiEnum;
			PropertyInfo            propInfo;
			Object                  propO, dbO;
			ConstructorInfo         constrInfo;
			String                  currKey;
			FieldInfo               fi;
			Type                    myType;


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
				fi = fiEnum.Value as FieldInfo;

				if (fi.persist) 
				{
					propInfo = myType.GetProperty(currKey);
					
					dbO = res[currKey];

					propO = MyTypedValue(dbO, fi);
					//Debug.WriteLine(String.Format("Setze Wert {0} für property {1}",
					//                       propO,
					//                       currKey));

					propInfo.SetValue(newO,
						propO,
						null);
				}
			}

			Debug.Unindent();

			return newO;

		}

		//Beginnt eine Transaktion auf der Datenbankverbindung des Epo
		public void BeginTransaction() 
		{
			EpoClassInfo		eci;
			OdbcTransaction		transaction;

            Debug.Assert(orderKey != null);
			Debug.Assert(transactionStore[orderKey]==null);

			eci = ClassInfo();
			Trace.WriteLineIf(tsw.TraceVerbose, "******************BEGIN TRANSACTION*****************************");
            eci.odbcConn = OpenDbConnection(eci.odbcConn);
			transaction = eci.odbcConn.BeginTransaction(IsolationLevel.ReadCommitted);
			transactionStore[orderKey] = transaction;

		}
   
   
		/// <summary>
		/// Führt eine Commit für eine die zuvor begonnene Transaktion aus.
		/// </summary>
		public void CommitTransaction() 
		{
			OdbcTransaction	transaction;
            OdbcConnection conn;
   	   
			Debug.Assert(orderKey!=null);

			Trace.WriteLineIf(tsw.TraceVerbose, "*********************COMMIT********************************");
	   
			transaction = transactionStore[orderKey] as OdbcTransaction;
            conn = transaction.Connection; // must be saved becaus Commit sets transaction.connection to null
			transaction.Commit();
			  
			transactionStore.Remove(orderKey);
            
            if(conn.State == ConnectionState.Open)
                conn.Close();
		}
   
   
		public void RollbackTransaction() 
		{
			OdbcTransaction	transaction;
   		
			Trace.WriteLineIf(tsw.TraceVerbose, "*********************ROLLBACK********************************");
	   
			transaction = transactionStore[orderKey] as OdbcTransaction;
			transaction.Rollback();
            transaction.Connection.Close();

			transactionStore.Remove(orderKey);
		}


		//Findet eine bereits angelegte Connection oder legt eine neue an und öffnet diese
		protected OdbcConnection GetOdbcConnection(string connStr) 
		{
			EpoClassInfo      		eci = null;
			OdbcConnection			conn = null;
			IDictionaryEnumerator	cienu, ordenu;
			bool					found = false;
			Hashtable				cisByOrder;
			

			//Datenbankverbindungen über alle Ordnungsbegriffen wieder verwenden
			ordenu = classInfos.GetAllClassInfos().GetEnumerator();
			while(ordenu.MoveNext() && !found) 
			{
				cisByOrder = ordenu.Value as Hashtable;
				cienu = cisByOrder.GetEnumerator();
				if(cienu != null) 
				{
					while(cienu.MoveNext() && !found) 
					{
						eci = cienu.Value as EpoClassInfo;

						if(eci.odbcConn != null) 
						{
							if(eci.connStr.Equals(connStr))
							{
								if (eci.odbcConn!=null) 
								{
									if(eci.odbcConn.State != ConnectionState.Closed)
										found = true;
								}
							}
						}

						if(found)
							conn = eci.odbcConn;
					}
				}
			}

			if(!found) 
			{
				try 
				{
					conn = new OdbcConnection(connStr);
					conn.Open();

					ClassInfo().connStr = connStr;
				}
				catch (Exception exc) 
				{
					// Die gesamte ClassInfo für diese Klasse rauswerfen
					RemoveClassInfo(eci);

					throw;
				}
			}
			else
				ClassInfo().connStr = connStr;

			return conn;
		}


		/// <summary>
		/// Die ClassInfo wieder aus dem ClassInfo-Verzeichnis entfernen
		/// </summary>
		/// <param name="eci"></param>
		private void RemoveClassInfo(EpoClassInfo eci) 
		{
			if (eci!=null) 
			{
				classInfos.RemoveClassInfo(orderKey, eci.className);
			}
		}
	 

		/// <summary>
		/// Die ClassInfo aufsetzen
		/// </summary>
		/// <returns>Wenn die ClassInfo erstmalig aufgesetzt wurde wird true zurückgeliefert. War die ClassINf
		/// schon da wird false zurückgegeben.</returns>
		protected virtual bool SetupClassInfo() 
		{
			bool				answ;
			EpoClassInfo		eci;
			String				clName = this.GetType().Name;

			eci = ClassInfo();
			
			if(eci == null) 
			{
				answ = true;
				eci = new EpoClassInfo();
				eci.className = clName;
				eci.tableName = clName;
				eci.fullClassName = GetType().AssemblyQualifiedName;
				eci.AddKeyField("oid"); //Epo hat das KeyField oid
				SetNewClassInfo(eci);
			} 
			else 
			{
				answ = false;
			}


			return answ;
		}

		//Diese Methode sollte nur in Ausnahmefaellen von erbenden Klassen ueberladen werden. Und zwar dann,
		// wenn man den Datenzugriff selber regeln will/muss. S. sz.B. EpoGeneric
		protected virtual void InitDbConnection(String connStr) 
		{
			String            myStmt;
			bool              done;
			OdbcDataReader    dr = null;
			bool			  haveToWork = true;
			EpoClassInfo	  eci;
			Epo				  kv;


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

			eci = ClassInfo();
			if (tsw.TraceVerbose) 
			{
				Trace.WriteLine(String.Format("Initialisierung der Epoklasse <{0}> mit: {1}",
					eci.fullClassName,
					connStr));
			}   
	
			//Die Datenbankverbindung anlegen bzw. ermitteln wenn sie schon existiert.
			eci.odbcConn = GetOdbcConnection(connStr);
			connected = eci.odbcConn != null;

			try
			{
				classInfos.SetKnownVerwo(orderKey, this.GetType().Name, this);
				
			}
			catch (Exception exc)
			{
				Trace.WriteLine("Die EPO-Klasse " + this.GetType().Name + " wurde mehrfach unter dem Ordnungsbegriff " 
					+ orderKey 
					+ " initialisiert.");

			}

			if(!haveToWork)
				return;
	

			InitMyFieldMap();
			InitSpecialDbLen();

			// Versuch mal ein Select um festzustellen ob es die Tabelle
			// schon gibt
			myStmt = "SELECT oid FROM " + eci.tableName + ";";

			lock(eci.odbcConn) 
			{
                try
                {
                    dr = SelStmt(myStmt);

                    if (dr == null)
                    {
                        myStmt = "CREATE TABLE " + eci.tableName
                            + " "
                            + MyTableDomains()
                            + ";";

                        done = ExStmt(myStmt);
                        if (!done)
                        {
                            String errStr = "Gebe es auf. Tabelle ist nicht vorhanden und kann auch nicht neu angelegt werden";
                            Trace.WriteLine(errStr);
                            connected = false;
                            throw new Exception(errStr);
                        }
                        else
                        {
                            InitializeData();
                        }
                    }
                    else
                    {
                        //Auch hier damit Daten hinzugefügt werden bzw. Korrigiert werden
                        InitializeData();
                    }
                }
                finally
                {
                    if (dr != null)
                        dr.Close();

                    CloseDbConnection(eci.odbcConn);
                }
			}

		}

		// Baut das Dictionary der persistenten Properties und deren Typen auf
		protected void InitMyFieldMap() 
		{
			FieldInfo         fi;
			EpoClassInfo      eci;
			Type              myType;
			PropertyInfo[]   props;

			myType = GetType();
			if (tsw.TraceVerbose) 
			{
				Debug.WriteLine(String.Format("InitFieldMap fuer <{0}>", myType.Name));
				Debug.Indent();
			}

			eci = ClassInfo();
			props = myType.GetProperties(BindingFlags.Public
				| BindingFlags.Instance);

			for(int i=0; i<props.Length; i++) 
			{
				PropertyInfo myPropInfo = (PropertyInfo)props[i];

				fi = new FieldInfo(myPropInfo.PropertyType);
				fi.persist = myPropInfo.CanWrite;
				eci.fieldMap[myPropInfo.Name] = fi;
				fi.Complete();
				if (tsw.TraceVerbose) 
				{
					Debug.WriteLine(String.Format("FieldInfo Eintrag fuer Property <{0}>,Typ <{1}>",
						myPropInfo.Name,
						myPropInfo.PropertyType));
				}
			}

			if (tsw.TraceVerbose) 
			{
				Debug.Unindent();
			}
		}


		/// <summary>
		/// Setzt die ClassInfo. Soll normalerweise nicht verwendet werden. es sei denn man mach sich eine eigene Epo.Klasse
		/// in der alles selbst gebastelt wird. S.z.B. EpoGeneric.
		/// </summary>
		/// <param name="eci">Dei Epo-Class-Info</param>
		protected void SetNewClassInfo(EpoClassInfo eci) 
		{
			classInfos.SetClassInfo(orderKey, eci.className, eci);
		}



		///Momentan wird hier nix gemacht, der Member soll aber erhalten bleiben damit er von allen
		///erbenden Klassen aufgerufen werden kann.
		protected virtual void InitSpecialDbLen() 
		{
		}



		protected void SetDbLen(String propName, int l) 
		{
			SetDbLen(propName, l, 0);
		}


		///<summary>
		///Setzt die Datenbanklänge einer persistenten Property
		///<param name=propName>Der Name der Property </param>
		///<param name=l>Die Länge auf der Datenbank</param>
		///</summary>
		protected void SetDbLen(String propName, int l, int p) 
		{
			EpoClassInfo   eci;
			FieldInfo      fi;

			Debug.Assert(propName!=null);
			Debug.Assert(propName.Length>0,"SetDbLen ohne property name");

			eci = ClassInfo();
			
			if(eci==null)
				throw new EpoException(String.Format("ClassInfo für die Klasse {0} kann nicht gefunden werden.",
					GetType().Name));


			
			fi = (FieldInfo)eci.fieldMap[propName];
			if(fi==null)
				throw new EpoException(String.Format("FieldInfo für die Klasse {0}, Feld {1} kann nicht gefunden werden",
				GetType().Name,
				propName));

			Debug.Assert(fi.persist, "SetDbLen fuer nicht persistente (readonly) property ist sinnlos.");
			fi.size = l;
			fi.precision = p;
		}
   
		protected String GetConnStr() 
		{
			EpoClassInfo	eci;
   
			eci = ClassInfo();
			Debug.Assert(eci != null);
	  
			return eci.connStr;
		}


		protected OdbcDataReader SelStmt(String s) 
		{
			EpoClassInfo		eci;
			OdbcDataReader		answ = null;
			OdbcCommand			cmd;
			OdbcTransaction		transaction;

			eci = ClassInfo();
			
			if (tsw.TraceInfo) 
			{
				Trace.WriteLine(String.Format("ODBC SEL: {0}", s));
			}
			
			cmd = new OdbcCommand(s, eci.odbcConn);

			transaction = transactionStore[orderKey] as OdbcTransaction;
			if(transaction!=null)
				cmd.Transaction = transaction;
	 	
			try 
			{
				answ = cmd.ExecuteReader();
			}
			catch (OdbcException oexc)
			{
				Trace.WriteLine(oexc.Message);
			}
			catch (Exception exc) 
			{
				Trace.WriteLine(exc.Message);
			} 
			

			return answ;
		}


		//Alles was unter dem orderKey gespeichert wurde wegwerfen und die GC auslösen
		//Dabei Datenbankverbindung nicht schliessen weil dies zwischen Klassen und orderKeys gepoolt sind.
		public static void ClearOrder(string	orderKey) 
		{
			Hashtable	cis, kvs;

			if(classInfos==null) return;

			kvs = classInfos.GetAllKnownVerwos();
			kvs.Remove(orderKey);

			cis = classInfos.GetAllClassInfos();
			cis.Remove(orderKey);

			GC.Collect();

		}

		protected bool ExStmt(String s) 
		{
			int					lines;
			bool				answ = false;
			EpoClassInfo		eci;
			OdbcTransaction		transaction;

			eci = ClassInfo();

			if (tsw.TraceInfo) 
			{
				Trace.WriteLine(String.Format("ODBC EXEC: {0}", s));
			}   

			lock(eci.odbcConn) 
			{
                eci.odbcConn = OpenDbConnection(eci.odbcConn);
                try
                {
                    OdbcCommand cmd = new OdbcCommand(s, eci.odbcConn);
                    transaction = transactionStore[orderKey] as OdbcTransaction;
                    if (transaction != null)
                        cmd.Transaction = transaction;

                    try
                    {
                        lines = cmd.ExecuteNonQuery();

                        answ = true;
                    }
                    catch (InvalidOperationException exc)
                    {
                        Trace.WriteLine(exc.Message);
                    }
                    finally
                    {
                        cmd.Dispose();
                    }
                }
                finally
                {
                    CloseDbConnection(eci.odbcConn);
                }
			}

			return answ;
		}





		protected bool ExStmt(String s, int expectLines) 
		{
			bool			answ = false;
			int				lines;
			EpoClassInfo	eci;
			OdbcTransaction	transaction;

			eci = ClassInfo();
			if (tsw.TraceInfo) 
			{
				Trace.WriteLine(String.Format("ODBC EXEC: {0}", s));
			}

            eci.odbcConn = OpenDbConnection(eci.odbcConn);
			lock(eci.odbcConn) 
			{
                OdbcCommand cmd = new OdbcCommand(s, eci.odbcConn);
               
				transaction = transactionStore[orderKey] as OdbcTransaction;
				if(transaction!=null)
					cmd.Transaction = transaction;

                try
                {
                    lines = cmd.ExecuteNonQuery();
                    if (lines != expectLines)
                        answ = false;
                    else
                        answ = true;
                }
                catch (InvalidOperationException exc)
                {
                    Trace.WriteLine(exc.Message);
                }
                finally
                {
                    CloseDbConnection(eci.odbcConn);
                }
			}

			return answ;
		}


		private String MyTableDomains() 
		{
			String					answ = "(";
			EpoClassInfo			eci;
			String             		myClName = GetType().Name;
			FieldInfo            	fi;
			IDictionaryEnumerator	idictEnum;

			eci = ClassInfo();

			idictEnum = eci.fieldMap.GetEnumerator();
			idictEnum.Reset();
			while(idictEnum.MoveNext()) 
			{
				fi = idictEnum.Value as FieldInfo;

				if (fi.persist) 
				{
					answ = answ + " " + fldOpenBr + idictEnum.Key + fldCloseBr;
					answ = answ + " " + fi.dbTypeName;

					if(fi.size>0) 
					{
						answ = answ + "(" + fi.size.ToString() + ")";
					}
					if(fi.csTypeName.Equals("oid")) 
					{
						answ = answ + " NOT NULL ";
					}

					answ = answ + ",";
				}
			}

			answ = answ + "PRIMARY KEY (oid))";

			return answ;
		}


		//Die Feldliste für das UPDATE-Statement generieren
		protected virtual String MyUpdateString() 
		{
			String					answ = "";
			EpoClassInfo			eci;
			String					myClsName;
			FieldInfo				fi;
			IDictionaryEnumerator   idictEnum;
			int						lastKommaIdx;

			myClsName = GetType().Name;
			eci = ClassInfo();

			idictEnum = eci.fieldMap.GetEnumerator();
			idictEnum.Reset();
			while(idictEnum.MoveNext()) 
			{
				fi = idictEnum.Value as FieldInfo;
				if(!eci.IsKeyField(idictEnum.Key as string) && fi.persist) 
				{
					answ = answ + " " + fldOpenBr + (idictEnum.Key as String) + fldCloseBr;
					answ = answ + "=";
					answ = answ + MyValue(idictEnum.Key as String);

					answ = answ + ",";
				}
			}

			lastKommaIdx = answ.LastIndexOf(',');

			return answ.Remove(lastKommaIdx, 1) + " ";
		}


		//Den Wert der Property mit dem Namen propName 
		//als string zurückliefern, den das DB-System versteht
		protected String MyValue(String propName) 
		{
			EpoClassInfo      	eci;
			String         		myClsName;
			Object         		val;
			String         		answ = "NULL";
			FieldInfo         	fi;

			myClsName = GetType().Name;
			eci = ClassInfo();

			Debug.Assert(propName.Length>0);
			Debug.Assert(myClsName!=null);
			Debug.Assert(myClsName.Length>0);
			Debug.Assert(eci!=null);

			val = GetPropValue(propName);
			if(val!=null) 
			{
				fi = eci.fieldMap[propName] as FieldInfo;

				Debug.Assert(fi.persist);

				if(fi.dbTypeName.Equals("VARCHAR")) 
				{
					if(fi.csTypeName.Equals("TimeSpan")) 
					{
						TimeSpan ts = (TimeSpan)val;
						long	l;
					
						l = ts.Ticks;
					
						answ = "\'" + l.ToString() + "\'";
					} 
					else 
					{
						answ = "\'" + val.ToString() + "\'";
					}
				} 
				else if (fi.dbTypeName.Equals("TIMESTAMP")) 
				{
					DateTime dt = (DateTime)val;
      	
                    
					//String myStr = dt.ToString(usCulture.DateTimeFormat);
					String myStr = dt.ToString("yyyy-MM-dd HH:mm:ss");
					answ = "#" + myStr + "#";
				} 
				else if (fi.dbTypeName.Equals("DATE")) 
				{
					DateTime dt = (DateTime)val;
      	
					String myStr = dt.ToString("dd.MM.yyyy HH:mm:ss");
					answ = "TO_DATE('" + myStr + "', 'DD.MM.YYYY HH24:MI:SS')";
				} 
				else if (fi.dbTypeName.Equals("INTEGER") && fi.csTypeName.Equals("Boolean")) 
				{
					bool b = (bool)val;
					if(b)
						answ = "1";
					else
						answ = "0";
				} 
				else if(fi.dbTypeName.Equals("LONGTEXT")) 
				{
					answ = "\'" + val.ToString() + "\'";
				} 
				else if (fi.dbTypeName.Equals("DOUBLE")) 
				{
					double d = (double)val;
					answ = d.ToString(usCulture.NumberFormat);
				}
				else if (fi.dbTypeName.Equals("NUMBER")) 
				{
					decimal d = (decimal)val;
					answ = d.ToString(usCulture.NumberFormat);
				}
				else
					answ = val.ToString();
			}

			return answ;
		}


		protected virtual String MyProperties() 
		{
			EpoClassInfo			eci;
			String					myClsName;
			IDictionaryEnumerator   idictEnum;
			String					answ = "";
			int						komPos;
			FieldInfo				fi;

			myClsName = GetType().Name;
			eci = ClassInfo();

			idictEnum = eci.fieldMap.GetEnumerator();
			idictEnum.Reset();
			while(idictEnum.MoveNext()) 
			{
				fi = idictEnum.Value as FieldInfo;
				if (fi.persist) 
				{
					answ = answ + fldOpenBr + (idictEnum.Key as String) + fldCloseBr;
					answ = answ + ", ";
				}
			}

			if(answ.Length>2) 
			{
				komPos = answ.LastIndexOf(',');
				answ = answ.Remove(komPos, 2);
			}

			return answ;
		}

		protected virtual String MyValues() 
		{
			EpoClassInfo			eci;
			String            		myClsName;
			IDictionaryEnumerator	idictEnum;
			String            		answ = "";
			int               		komPos;
			FieldInfo				fi;

			myClsName = GetType().Name;
			eci = ClassInfo();
			idictEnum = eci.fieldMap.GetEnumerator();
			idictEnum.Reset();
			while(idictEnum.MoveNext()) 
			{
				fi = idictEnum.Value as FieldInfo;
				if (fi.persist) 
				{
					answ = answ + MyValue(idictEnum.Key as String);
					answ = answ + ", ";
				}
			}

			if(answ.Length>2) 
			{
				komPos = answ.LastIndexOf(',');
				answ = answ.Remove(komPos, 2);
			}

			return answ;
		}


		///Liefert ein csType-Objekt zurück das aus der Umandlung
		/// des inO gebildet wird.
		protected Object MyTypedValue(Object inO, FieldInfo fi) 
		{
			Object answ = null;

			Debug.Assert(fi.persist);

			if(inO==null) 
			{
				answ = null;
				return answ;
			}

			if(inO==System.DBNull.Value) 
			{
				answ=null;
				return answ;
			}


			switch(fi.csTypeName) 
			{
				case "Oid":
					answ = new Oid(inO as String);
					break;
				case "Boolean":
					if (Epo.dbMode==EnumDbMode.Oracle) 
					{
						decimal	decRep;

						decRep = (decimal)inO;
						answ = !(decRep == 0);

					} 
					else 
					{
						int intRep;
						intRep = (int)inO;
						answ = !(intRep == 0);
					}
					break;
				case "Decimal":
					decimal		decRep2;

					decRep2 = (decimal)inO;

					answ = decRep2;
					break;
				case "String":
					answ = inO.ToString();
					break;
				case "EpoMemo":
					answ = new EpoMemo(inO.ToString());
					break;
				case "TimeSpan":
					long		l;
     
					try
					{
						l = long.Parse(inO.ToString());
						answ = new TimeSpan(l);
					} 
					catch(Exception exc) 
					{
						Trace.WriteLine(exc.Message);
						answ = new TimeSpan();
					}
					break;
				case "DateTime":
					answ = inO;
					break;
				case "Int32":
					if (Epo.dbMode==EnumDbMode.Oracle) 
					{
						decimal	decRep;

						decRep = (decimal)inO;
						answ = (int) decRep;

					} 
					else  
					{
						answ = (int)inO;
					}
					break;
				default:
					answ = inO;
					break;
			}

			return answ;
		}
		#endregion

		///<summary>
		///Den Wert der Property mit dem Namen propName zurückliefern
		///<param name="propName">Der Name der public property</param>
		///</summary>
		public Object GetPropValue(String propName) 
		{
			PropertyInfo	propInfo;
			Object			val;
			string			className;

			propInfo = GetType().GetProperty(propName);
			if(propInfo==null) 
			{
				className = ClassInfo().className;
				throw new ApplicationException("EPO-Error: Die Property <" 
					+ propName 
					+ "> ist in der Klasse <" 
					+ className 
					+ "> nicht vorhanden.");
			}

			val = propInfo.GetValue(this, null);

			return val;
		}


		//Versucht eine Methode mit dem Namen methodName aufzurufen und gibt deren Wert zurück
		// Dies kann für Berechnete Felder verwendet werden.
		public object GetMethValue(string methodName) 
		{
			MethodInfo    methInfo;

			Debug.Assert(methodName != null, "Der Methodenname fehlt in GetMethValue");

			methInfo = GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
			if (methInfo == null) 
			{
				throw new ApplicationException("Die Methode <" + methodName + "> wurde in der EpoKlasse <"
					+ GetType().Name
					+ "> nicht gefunden.");

			}

			return methInfo.Invoke(this, null);
		}


		public FieldInfo GetFieldInfo(string fieldName) 
		{
			EpoClassInfo	eci;
			FieldInfo		fi;
   	 
			eci = this.ClassInfo();
			fi = eci.fieldMap[fieldName] as FieldInfo;
   	 
			return fi;
		}
   
   
		public string KeyEqalityExpression() 
		{
			EpoClassInfo	eci;
			string			answ = "";
			string[]		keys;
			bool			first = true;

			eci = ClassInfo();
			keys = eci.KeyFieldArray();

			foreach(string k in keys) 
			{
				if(k.Length>0) 
				{
					if(!first) answ += " AND ";
					answ += k + "=" + MyValue(k);
				}
			}

			return answ;
		}


		public void HandleJoinedPicks() 
		{
			EpoClassInfo			eci;
			EpoPick					epi;
			EpoPickInfo				pi;
			IDictionaryEnumerator	enu, fieldEnu;
			string					joinF, nativeF, foreignF;
			Epo						targetEp;
			Oid						targetOid;
			object					targetVal;
			
			eci = ClassInfo();
			if (eci.picksMap.Count==0) return;

			enu = eci.picksMap.GetEnumerator();
			while(enu.MoveNext()) 
			{
				joinF = enu.Key as string;
				epi = enu.Value as EpoPick;

				targetEp = null;
				fieldEnu = epi.pickfields.GetEnumerator();
				while(fieldEnu.MoveNext()) 
				{
					nativeF = fieldEnu.Key as string;
					pi = fieldEnu.Value as EpoPickInfo;
					if(!pi.constrain) 
					{
						if(targetEp==null) 
						{
							targetOid = GetPropValue(joinF) as Oid;
							targetEp = ResolveOid(targetOid);
						}

						if(targetEp!=null) 
						{
							foreignF = pi.foreignField;
							targetVal = targetEp.GetPropValue(foreignF);
							SetPropValue(nativeF, targetVal);
						}
					}
				}
			}
		}


		///<summary>
		///Save persistent properties to the database
		///</summary>
		public virtual void Flush() 
		{
			///Versuch zuerst ein UPDATE
			///Wenn das nicht klappt dann versuch ein INSERT
			///Wenn das auch nicht klappt: Au Weia!

			String			myStmt;
			EpoClassInfo	eci;
			bool			done;

			eci = ClassInfo();

			HandleJoinedPicks();

			
			myStmt = "UPDATE "
				+  eci.tableName
				+ " SET "
				+  MyUpdateString()
				+ " WHERE "
				+ KeyEqalityExpression()
				+ ";";

			done = ExStmt(myStmt, 1);
			if(!done) 
			{

				myStmt = "INSERT INTO "
					+  eci.tableName
					+ "( "
					+ MyProperties()
					+ ") "
					+ "VALUES ( "
					+ MyValues()
					+ ");";

				done = ExStmt(myStmt);
			}
		}


		public virtual int GetCount() 
		{
			return GetCount(null);
		}


		public virtual int GetCount(string where) 
		{
			string			s;
			int				val = -1;
			EpoClassInfo	eci;
   	
   	
			eci = ClassInfo();

            eci.odbcConn = OpenDbConnection(eci.odbcConn);
            try
            {
                s = "SELECT COUNT(*) FROM "
                    + eci.tableName;

                if (where != null)
                    s += " WHERE " + where;


                s += ";";


                OdbcCommand cmd = new OdbcCommand(s, eci.odbcConn);
                try
                {
                    val = (int)cmd.ExecuteScalar();
                }
                catch (InvalidOperationException exc)
                {
                    Trace.WriteLine(exc.Message);
                }
            }
            finally
            {
                CloseDbConnection(eci.odbcConn);
            }

			return val;
		}


        /// <summary>
        /// Öffnet die Datenankverbindng zur Epo-Klasse falls diese noch nicht geöffnet ist. 
        /// Wenn wir in einer Transaktion sind, wird die Connection der Transaktion zurückgegeben.
        /// </summary>
        protected OdbcConnection OpenDbConnection(OdbcConnection con)
        {
            OdbcTransaction transaction;
            EpoClassInfo eci;
            OdbcConnection answ = null;

            if (con.State == ConnectionState.Closed)
            {

                transaction = transactionStore[orderKey] as OdbcTransaction;
                if (transaction != null)
                {
                    answ = transaction.Connection;
                }
                else
                {
                    eci = ClassInfo();
                    con = GetOdbcConnection(eci.connStr);
                    answ = con;
                }

            }
            else
                answ = con;

            if (answ == null)
                Trace.WriteLine("Das sollte nie passieren!!!!");

            return answ;

        }

        protected void CloseDbConnection(OdbcConnection con) {
            OdbcTransaction transaction;


            transaction = transactionStore[orderKey] as OdbcTransaction;
            
            if(transaction==null)
                con.Close();
        }
        

   
		///<summary>
		///Selects all Objects with sorting as preferred and return them in a
		/// Hashtable. This member is usually called from an administration
		/// object.
		///</summary>
		public virtual ArrayList InternalSelect(String where, String orderBy) 
		{
			String					myStmt;
			ArrayList				epoV = null;
			OdbcDataReader			res;
			EpoClassInfo			eci;
			Epo						newO;
			String					myClsName;
			String					ord;

			
			eci = ClassInfo();
			myClsName = eci.className;

			myStmt = "SELECT "
				+ MyProperties()
				+ " FROM "
				+ eci.tableName;
      
			if(where!=null) 
			{
				myStmt += " WHERE ("
					+ where + ")";
			}

			if(orderBy==null)
				ord = PreferredOrdering();
			else
				ord = orderBy;
	 	
			if (ord != null) 
			{
				myStmt += " ORDER BY "
					+ ord;
			}

			myStmt += ";";

			lock(eci.odbcConn) 
			{
                eci.odbcConn = OpenDbConnection(eci.odbcConn);
				res = SelStmt(myStmt);
				if(res!=null) 
				{
					try 
					{
						eci = ClassInfo();
						epoV = new ArrayList();
						while(res.Read()) 
						{
							newO = BuildEpoFromData(res, eci);
							newO.orderKey = orderKey; //Den Ordnungsbegriff vererben
							epoV.Add(newO);
						}
					} 
					finally 
					{
						res.Close();
                        CloseDbConnection(eci.odbcConn);
					}
				}
			}

			return epoV;
		}
   
   
   
   
		///<summary>
		/// Selects all Objects with sorting as preferred and return them in an
		/// ArrayList. This member is usually called from an administration
		/// object. The delegate supplied by the caller determines which Epo
		/// is bigger and so to appear more in the beginning than the another Epo.
		///</summary>
		public virtual ArrayList Select(String where, IComparer comp) 
		{
			ArrayList	answ;
   	   
   	   
			answ = InternalSelect(where, "oid");
			if(answ != null) 
			{
			}
   	   
			answ.Sort(comp);
   	   
			return answ;
		}
   
   
		public virtual ArrayList Select(IComparer comp) 
		{
			ArrayList	answ;
      	   
      	   
			answ = InternalSelect(null, null);
			if(answ != null) 
			{
				answ.Sort(comp);
			}
      	   
			return answ;
		}
   
		public virtual ArrayList Select(string where) 
		{
			return InternalSelect(where, null);
		}
   
		public virtual ArrayList Select(string where, string order) 
		{
			return InternalSelect(where, order);
		}
   
   
		///<summary>
		///Selects all Objects with sorting as preferred and return them in a
		/// ArrayList. This member is usually called from an administration
		/// object.
		///</summary>
		public virtual ArrayList Select() 
		{
			return InternalSelect(null, null);
		}


		///<summary>
		///Liefert die Länge eines persistenten Fields in der Datenbank.
		///</summary>
		public int GetDbFieldLength(string name) 
		{
			EpoClassInfo	eci;
			FieldInfo		fi;
   	
			eci = ClassInfo();
			Debug.Assert(eci!=null);
			fi = eci.fieldMap[name] as FieldInfo;
			Debug.Assert(fi!=null, "Unbekannter Feldname <" + name + ">");
   	
			return fi.size;
		}

		///<summary>
		///Erzeugt ein neues Exemplar der Klasse. Dies ist nicht epo sondern
		///die erbende Klasse. Normalerweise nutzt man dies nur vom Verwaltungsobjekt
		///aus.
		///</summary>
		public Epo New() 
		{
			ConstructorInfo   	constrInf;
			Epo            	answ;

			constrInf = GetType().GetConstructor(Type.EmptyTypes);
			answ = constrInf.Invoke(null) as Epo;

			return answ;
		}
   
		// Für die Benutzung aus Visual Basic, da geht New nicht weil es
		// den Konstruktor von VB-Klassen aktivieren will
		public Epo NewInstance() 
		{
			return New();
		}




		private void AppendDeleteStmtsByRule(ArrayList	stmts) 
		{
			string					appStr,
				targetClassName;
			EpoClassInfo			eci = ClassInfo(),
				targetEci;
			Hashtable				linkMap = eci.linkMap;
			IDictionaryEnumerator	enu;
			EpoLink					link;
			Epo						verwo, ep;
			ArrayList				erg;
			
			if(linkMap==null) return;
			
			enu = linkMap.GetEnumerator();
			if(enu==null) return;
			
			
			while(enu.MoveNext()) 
			{
				link = enu.Value as EpoLink;
				if(link.delRule == EnumDelRule.cascade) 
				{
					targetClassName = enu.Key as String;		
					targetEci = classInfos.GetClassInfo(orderKey, targetClassName);
					Debug.Assert(targetEci != null, 
						"Target-Eopklasse <" + targetClassName + "> existiert nicht, oder wurde nicht initialisiert");
				
					verwo = NewByClassName(orderKey, targetClassName);
					erg = verwo.Select(fldOpenBr + link.foreignFieldName + fldCloseBr + "='" + oid + "'");
				
					if(erg!=null) 
					{
						appStr =  "DELETE FROM "
							+ targetEci.tableName
							+ " WHERE " 
							+ fldOpenBr
							+ link.foreignFieldName
							+ fldCloseBr
							+ "='"
							+ oid
							+ "';";
									
						stmts.Add(appStr);
					
						for(int i=0; i<erg.Count; i++) 
						{
							ep = erg[i] as Epo;
							ep.AppendDeleteStmtsByRule(stmts);
						}
					}
				}
			}	
		}




		protected bool HandleDeleteRules(Epo epo) 
		{
			EpoClassInfo			eci, targetEci;
			Hashtable				linkMap;
			IDictionaryEnumerator	enu;
			bool					failed;
			EpoLink					link;
			string					targetClassName;
			Epo						verwo, ep;
			ArrayList				erg;
		
			eci = ClassInfo();
			linkMap = eci.linkMap;
		
			if(linkMap==null) return true;
		
			enu = linkMap.GetEnumerator();
			if(enu==null) return true;
		
			failed = false;
		
		
			while(!failed && enu.MoveNext()) 
			{
				link = enu.Value as EpoLink;
				if(link.delRule==EnumDelRule.cascade) 
				{
					targetClassName = enu.Key as String;		
					targetEci = classInfos.GetClassInfo(orderKey, targetClassName);
					Debug.Assert(targetEci != null, 
						"Target-Eopklasse <" + targetClassName + "> existiert nicht, oder wurde nicht initialisiert");
							 
					verwo = NewByClassName(orderKey, targetClassName);
					erg = verwo.Select(fldOpenBr + link.foreignFieldName + fldCloseBr + "='" + oid + "'");
				
					if(erg!=null) 
					{
						for(int i=0; (!failed && i<erg.Count); i++) 
						{
							ep = erg[i] as Epo;
							failed = !ep.Delete();
						}
					}
				}
			}
		
			return !failed;
		}
	
	
	
		public bool DeleteBuffered() 
		{
			ArrayList	stmtBuffer = new ArrayList();
			string		myStmt;
			bool		done = true;
			int			i;
            EpoClassInfo eci;
		
			//Für jedes Delete (auch der Detailobjekte) ein Statement einfügen in stmts
			AppendDeleteStmtsByRule(stmtBuffer);

            eci = ClassInfo();

			myStmt= "DELETE FROM "
				+ eci.tableName
				+ " WHERE "
				+ KeyEqalityExpression()
				+ ";";
      		
			//Delete für den Master anhängen
			stmtBuffer.Add(myStmt);
      	
			i = 0;
			while(i<stmtBuffer.Count && done) 
			{
				myStmt = stmtBuffer[i] as string;
      		
				done = ExStmt(myStmt);
				if(!done)
					Trace.WriteLine(String.Format("Delete failed for <{0}>, <{1}>",
						eci.className,
						oid));
			
				i++;
			}
      	
			return done;
		}
	
		///<summary>
		///Löscht das Objekt aus der Datenbank
		///</summary>
		public virtual bool Delete() 
		{
			String    	myStmt;
			String    	myClsName;
			bool      	done;
            EpoClassInfo eci;

			done = HandleDeleteRules(this);
			if(!done) return false;

            eci = ClassInfo();

			myStmt= "DELETE FROM "
				+ eci.tableName
				+ " WHERE "
				+ KeyEqalityExpression()
				+ ";";


			done = ExStmt(myStmt);
			if(!done)
				Trace.WriteLine(String.Format("Delete failed for <{0}>, <{1}>",
					eci.className,
					oid));

			return done;
		}


		public EpoClassInfo ClassInfo() 
		{
			return classInfos.GetClassInfo(orderKey, GetType().Name);
		}


		public static EpoClassInfo ClassInfo(Epo ep) 
		{
			return classInfos.GetClassInfo(ep.orderKey, ep.GetType().Name);
		}

  
  
		public virtual object Clone() 
		{
			Epo 					answ = null;
			ConstructorInfo   		constrInf;
			FieldInfo				fi;
			EpoClassInfo         	eci;
			string             		myClName = GetType().Name;
			IDictionaryEnumerator	idictEnum;
			string					propName;
			object					val;
			PropertyInfo            propInfo;
	
			constrInf = GetType().GetConstructor(Type.EmptyTypes);
	
			answ = constrInf.Invoke(null) as Epo;
			if(answ!=null) 
			{
				//Nun die Werte kopieren und dabei die oid weglassen, denn die wurde schon im
				//Konstruktor neu gebildet und muss sich unterscheiden.
  				answ.orderKey = orderKey;

				eci = ClassInfo();
				idictEnum = eci.fieldMap.GetEnumerator();
				idictEnum.Reset();
				while(idictEnum.MoveNext()) 
				{
					fi = idictEnum.Value as FieldInfo;
					propName = idictEnum.Key as String;
			    
					if(!eci.IsKeyField(propName) && fi.persist) 
					{
						val = GetPropValue(propName);	
  	  		
						propInfo = answ.GetType().GetProperty(propName);
						propInfo.SetValue(answ,
							val,
							null);
					}
				}
  	    	
  	    	
				//Nun noch die Copy-Rules abarbeiten
				HandleCopyRules(answ);
			}
  		
			return answ as object;
		}
  	
  	
  	
		private void HandleCopyRules(Epo newEpo) 
		{
			EpoClassInfo			eci, targetEci;
			Hashtable				linkMap;
			IDictionaryEnumerator	enu;
			EpoLink					link;
			string					targetClassName;
			Epo						verwo, ep, clone;
			ArrayList				erg;
			string					myKey;
			
			eci = ClassInfo();
			linkMap = eci.linkMap;
			
			if(linkMap==null) return;
			
			enu = linkMap.GetEnumerator();
			if(enu==null) return;
			
			myKey = GetKey();
			
			
			while(enu.MoveNext()) 
			{
				link = enu.Value as EpoLink;
				if(link.copyRule==EnumCopyRule.copy) 
				{
					targetClassName = enu.Key as String;		
					targetEci = classInfos.GetClassInfo(orderKey, targetClassName);
					Debug.Assert(targetEci != null, 
						"Target-Epoklasse <" + targetClassName + "> existiert nicht, oder wurde nicht initialisiert");
								 
					verwo = NewByClassName(orderKey, targetClassName);
					erg = verwo.Select(fldOpenBr + link.foreignFieldName + fldCloseBr + "=" + myKey );
					
					if(erg!=null) 
					{
						for(int i=0; i<erg.Count; i++) 
						{
							ep = erg[i] as Epo;
							clone = ep.Clone() as Epo;
							if (clone != null) 
							{
								clone.SetPropValue(link.foreignFieldName, newEpo.GetKeyValue());
							} 
							else 
							{
								Debug.Assert(false, "Clonen hat nicht funktioniert obwohl Copy-Rule dies verlangt");
							}
						
							clone.Flush();
						}
					}
				} 
				else if(link.copyRule==EnumCopyRule.link) 
				{
					Debug.Assert(false, "Eine Copy-Rule ist auf link gesetzt, obwohl n:m Beziehungen noch nicht implementiert wurden");
				}
			}
			
	
		}
  	
  	
		public void SetPropValue(string propName, object value) 
		{
			PropertyInfo            propInfo;

			propInfo = GetType().GetProperty(propName);
			Debug.Assert(propInfo != null, "Unbekannter property name in SetPropValue())");

			if (propInfo.CanWrite)
				propInfo.SetValue(this,
					value,
					null);

		}

		/// <summary>
		/// Den Property Wert aus einem String setzen. Dazu ggf. den übergeben string parsen
		/// </summary>
		/// <param name="propName">Der Name der Property</param>
		/// <param name="value">Der Wert als string</param>
		public void SetPropValueFromString(string propName, string val) 
		{
			FieldInfo	fi;

			fi = GetFieldInfo(propName);
			if(fi==null) throw new ApplicationException("Die Property <" + propName + "> existiert nicht in der Klasse <"
					+ ClassInfo().className + ">.");

			if(val==null) 
			{
				SetPropValue(propName, null);
				return;
			}

			switch(fi.csTypeName) 
			{
				case "String":
					val = val.Replace("'", "\"");
					SetPropValue(propName, val);
					break;
				case "EpoMemo":
					val = val.Replace("'", "\"");
					SetPropValue(propName, new EpoMemo(val));
					break;
				case "Int32":
					int	intVal;

					try 
					{
						intVal = Int32.Parse(val);
						SetPropValue(propName, intVal);
					} 
					catch(Exception exc) 
					{
					}
					break;
				case "Oid":
					SetPropValue(propName, new Oid(val));
					break;
				case "Double":
					double	doubVal;

					try 
					{
						doubVal = Double.Parse(val);
						SetPropValue(propName, doubVal);
					} 
					catch(Exception exc) 
					{
					}
					break;
				case "Boolean":
					bool	bVal = false;
					try 
					{
						bVal = Boolean.Parse(val);
						SetPropValue(propName, bVal);
					} 
					catch(Exception exc) 
					{
					}
					break;
				case "DateTime":
					DateTime dt;

					try
					{
						dt = DateTime.Parse(val);
						SetPropValue(propName, dt);
					} 
					catch(Exception exc)
					{
					}
					break;
			}
		}


		/// <summary>
		/// Fügt einenen Joined Pick zu einem Join hinzu. Dies bewirkt dass ein feld in der Quellepo stets 
		/// mit dem Wert eines Feldes in der Ziel-Epo des Joins gefüllt wird,
		/// wenn der Join mit einer oid befüllt wird.
		/// </summary>
		/// <param name="joinName">Der Name des Joinfeldes das bereits existieren muss</param>
		/// <param name="foreignField">Der Name des Feldes in der Ziel-Epo</param>
		/// <param name="nativeField">Der Name des Feldes in der Quell-Epo des Joins.</param>
        /// <param name="co">Wahr um ein ConstrainFeld zu definieren. Dann werde nur soche Werte angeboten bei denen die Inhalte von Foreign und Native field identisch sind.</param>
		protected void AddJoinedPick(string joinName, string foreignField, string nativeField, bool co) 
		{
			EpoClassInfo	eci;
			EpoJoin			joinInf;
			EpoPick			ePick;

			eci = ClassInfo();
			joinInf = GetJoins(joinName);
			if (joinInf==null) {
				throw new ApplicationException("Der Join auf dem Feld <" 
					+ joinName
					+ "> existiert nicht beim Versuch einen Pick hinzuzufügen in der Klasse <"
					+ eci.className
					+ ">.");
			}

			if (eci.picksMap.Contains(joinName))
				ePick = eci.picksMap[joinName] as EpoPick;
			else 
			{
				ePick = new EpoPick();
				eci.picksMap[joinName] = ePick;
			}

			ePick.AddPickField(nativeField, foreignField, co);
						
		}


		//Fuegt einen Join auf eine andere EpoKlasse hinzu. Damit können 1:n Beziehungen
		//realisiert werden bei denen die aktuelle Epo-Klasse auf der 1-Seite und die
		//im Paramter className übergebene Klasse auf der n-Seite liegt.
		protected void AddJoin(string  fieldName,
							   string  className) 
		{

			EpoClassInfo	eci;
			Hashtable		joinMap;
			EpoJoin			join;


			Debug.Assert(fieldName != null, "Feldname muss angegeben werden in AddJoin");
			Debug.Assert(className != null, "Der Name der Zielklasse muss angegeben werden in AddJoin");

			eci = this.ClassInfo();
			joinMap = eci.joinMap;
		
			join = joinMap[fieldName] as EpoJoin;
		
			if(join==null) 
			{
				join = new EpoJoin(className);
							
				joinMap[fieldName] = join;
			} 
			else 
			{
				join.foreignClassNames.Add(className);
			}
		
		}
	
	
		//Fuegt einen LINK auf eine andere EpoKlasse hinzu. Damit können n:-Beziehungen
		//realisiert werden bei denen die aktuelle Epo-Klasse auf der n-Seite und die
		//im Paramter className übergebene Klasse auf der 1-Seite liegt. Zusätzlich können
		//Anweisungen übergeben werden die beim Löschen bzw. beim Kopieren der aktuellen Klasse
		//beachtet werden.
		protected void AddLink(string 			className,
			string			extFieldName,
			EnumDelRule		delRule,
			EnumCopyRule		copyRule) 
		{
						  
			EpoClassInfo	eci;
			Hashtable		linkMap;			
			EpoLink			link;
			
			
			Debug.Assert(extFieldName != null, "Feldname muss angegeben werden in AddJoin");
			Debug.Assert(className != null, "Der Name der Zielklasse muss angegeben werden in AddJoin");
			
			eci = this.ClassInfo();
			linkMap = eci.linkMap;
			
			link = new EpoLink(extFieldName,
				delRule,
				copyRule);
				
			linkMap[className] = link;
			
		}
	
	
		public EpoJoin GetJoins(string fieldName) 
		{
			EpoClassInfo	eci;
		
			eci = this.ClassInfo();
		
		
			return eci.joinMap[fieldName] as EpoJoin;
		}
	
	
		//Überladen um die Klasse mit Daten zu initialsieren nachdem die Tabelle
		//frisch angelegt wurde. Kann man fuer das Anlegen von SEED-Daten verwenden
		public virtual void InitializeData() 
		{
			if (tsw.TraceVerbose) 
			{
				Trace.WriteLine("Initialisierung von Dateninhalten für Klasse <"
					+ ClassInfo().className + ">");
			}
		}
	

		/// <summary>
		/// Eine neues Exemplar einer Epo-Klasse anhand des Klassennamens bilden
		/// </summary>
		/// <param name="orderKey">Der Ordnungsbgriff in dem die Verwaltungsinformationen gesucht werden</param>
		/// <param name="clName">Der Klassenname</param>
		/// <returns></returns>
		public static Epo NewByClassName(string orderKey, string clName) 
		{
			ConstructorInfo constrInf;
			Epo            	answ, epV;
			string			fullClassName;
			EpoClassInfo	eci;

		
			epV = Epo.VerwoByClassName(orderKey, clName);

			//Dies ergibt nicht die Classinfo von Epo sondern von der
			//Klasse deren Name in clName übergeben wurde!
			eci = classInfos.GetClassInfo(epV.orderKey, clName);
			fullClassName = eci.fullClassName;
		
			constrInf = Type.GetType(fullClassName).GetConstructor(Type.EmptyTypes);
			answ = constrInf.Invoke(null) as Epo;
			answ.orderKey = epV.orderKey;;

			return answ;
		}

		/// <summary>
		/// Anhand des Namens der Klasse das Verwaltungsobjekt finden und zurückgeben.
		/// </summary>
		/// <param name="clName">Der name der Klasse dessen Verwaltungsobjekt gefunden werden soll.</param>
		/// <returns></returns>
		public static Epo VerwoByClassName(string clName) 
		{
			return VerwoByClassName(stdOrderKey, clName);
		}

		/// <summary>
		/// Dient dazu festzustellen ob irgendwelche Verwaltungsobjekte für den betreffenden
		/// Ordnungsschlüssel initialisiert wurden.
		/// </summary>
		/// <param name="orderKey">Der Ordnungsschlüssel</param>
		/// <returns>True wenn Verwaltungsobjekte vorliegen, sonst false</returns>
		public static bool HasAnyVerwos(string orderKey) 
		{
			bool		answ = false;
			Hashtable	tst;

			if(classInfos!=null) 
			{
				tst = classInfos.GetKnownVerwos(orderKey);
				if(tst!=null) 
				{
					answ = tst.Count>0;
				}
			}

			return answ;
		}

		/// <summary>
		/// Anhand des Namens der Klasse das Verwaltungsobjekt finden und zurückgeben.
		/// </summary>
		/// <param name="clName">Der name der Klasse dessen Verwaltungsobjekt gefunden werden soll.</param>
		/// <returns></returns>
		public static Epo VerwoByClassName(string orderKey, string clName) 
		{
			Epo	answ = null;
		
			if(classInfos!=null)
				answ = classInfos.GetKnownVerwo(orderKey, clName);
			

			Debug.Assert(answ!=null, "Fuer die Klasse <" + clName + "> kann zu diesem Zeitpunkt kein Verwaltungsobjekt gefunden werden.");

			return answ;
		}



		/// <summary>
		/// Liefert eine Stringpräsentation des Schlüssel der Epo-Klasse.
		/// </summary>
		/// <returns>Ein eindeutiger Schlüssel</returns>
		public virtual string GetKeySearchString() 
		{
			return KeyEqalityExpression();
		}


		/// <summary>
		/// Wenn es nur einen Key gibt wird dessen Wert als String zurückgegeben, dabei werden die Gänsebeine mit produziert.
		/// </summary>
		/// <returns></returns>
		public virtual string GetKey() 
		{
			string[]	keys;

			keys = ClassInfo().KeyFieldArray();

			if (keys.Length != 1) 
			{
				throw new ApplicationException("Aufruf von GetKey in der Klasse <" 
					+ ClassInfo().className 
					+ ">wobei nicht genau ein Feld als Schlüssel definiert wurde.");
			}


			return MyValue(keys[0]);
		}


		/// <summary>
		/// Liefert den Inhalt des Schlüssels wenn es nur genau einen gibt, egal um welchen Typ es sich handelt
		/// </summary>
		/// <returns></returns>
		public virtual object GetKeyValue() 
		{
			string[]	keys;

			keys = ClassInfo().KeyFieldArray();

			if (keys.Length != 1) 
			{
				throw new ApplicationException("Aufruf von GetKey in der Klasse <" 
					+ ClassInfo().className 
					+ ">wobei nicht genau ein Feld als Schlüssel definiert wurde.");
			}


			return GetPropValue(keys[0]);
	}


		/// <summary>
		/// Closes any open DB-Connection. After this Epo-Klasses have to be reinitialized to allow
		/// further DB-operations. Normally used to tidy things when ending a program.
		/// </summary>
		public static void CloseAllConnections() 
		{
			IDictionaryEnumerator		enuc, enuorder;
			EpoClassInfo				eci;
			Hashtable					enucs;

			enuorder = classInfos.GetAllClassInfos().GetEnumerator();
			while (enuorder.MoveNext()) 
			{
				enucs = enuorder.Value as Hashtable;

				enuc = enucs.GetEnumerator();

				while(enuc.MoveNext()) 
				{
					eci = enuc.Value as EpoClassInfo;
					if (eci.odbcConn!=null) 
					{
						if (eci.odbcConn.State!=ConnectionState.Closed)
							eci.odbcConn.Close();
					}
				}
			}
		}


		



		/// <summary>
		/// Stellt fest ob ein Objekt ein Verwaltungsobjekt ist.
		/// </summary>
		/// <returns>True wenn es ein Verwaltungsobjekt ist, ansonsten false</returns>
		public bool IsVerwo() 
		{
			Hashtable kVerwos;

			kVerwos = classInfos.GetKnownVerwos(orderKey);

			return kVerwos.ContainsValue(this);
		}


		/// <summary>
		/// Setzt den Tabellennamen in der die Objekte dier Epo-Klasse abgelegt werden.
		/// Normalerweise ist der Tabellename identisch mit dem Klassennamen und die Benutzung
		/// von SetMainTable ist überflüssig.
		/// </summary>
		/// <param name="tName">Der name der bereits vorhandenen Tabelle</param>
		protected virtual void SetMainTable(string tName) 
		{
			EpoClassInfo	eci;

			eci = ClassInfo();
			eci.tableName = tName;
		}

		

	}
  
  
  

}
