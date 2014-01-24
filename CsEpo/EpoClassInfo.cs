namespace HSp.CsEpo 
{

	using System;
	using System.Collections;
	using System.Data.Odbc;


	public class EpoClassInfo 
	{
		public OdbcConnection   	odbcConn;
		public string           	connStr;
		public Hashtable        	fieldMap;
		public string           	className,
									fullClassName,
									tableName;
		public Hashtable		   	joinMap,
									picksMap,
									linkMap;
		private string				keyFields;
        private Hashtable           parameters;


		public EpoClassInfo() 
		{
			fieldMap = new Hashtable();
			joinMap = new Hashtable();
			linkMap = new Hashtable();
			picksMap = new Hashtable();
            parameters = new Hashtable();
		}

		/// <summary>
		/// Ein Schlüsselfeld hinzufügen
		/// </summary>
		/// <param name="name"></param>
		public void AddKeyField(string name) 
		{
			if (keyFields!=null) 
			{
				keyFields += ",";
				keyFields += name;
			} else
				keyFields = name;
		}


		/// <summary>
		/// Das Schlüsselfeld setzen
		/// </summary>
		/// <param name="name"></param>
		public void SetKeyField(string name) 
		{
			keyFields = name;
		}


		/// <summary>
		/// Liefert die Schlüsselfelder für diese Epo Klasse
		/// </summary>
		/// <returns>Schlüselfelder als Array von string</returns>
		public string[] KeyFieldArray() 
		{
			string[]	answ;

			if (keyFields==null) return null;

			answ = keyFields.Split(',');

			if (answ!=null) {
				foreach(string s in answ) 
				{
					s.Trim();
				}
			}

			return answ;
		}


		/// <summary>
		/// Feststellen ob es sich um ein key-Feld handelt
		/// </summary>
		/// <param name="name">Der Name des zu untersuchenden Feldes</param>
		/// <returns>True wenn es ein key-Feld ist, ansonsten false</returns>
		public bool IsKeyField(string name) 
		{
			string[]	keys;
			bool		answ = false;
			int			i = 0;

			keys = KeyFieldArray();

			while(i<keys.Length && !answ) 
			{
				answ = name.Equals(keys[i]);
				i++;
			}
			
			return answ;
		}

        public void SetParameter(object key, object value) {
            parameters[key] = value;
        }

        public object GetParameter(object key)
        {
            return parameters[key];
        }

	}


}