using System;
using System.Collections;

namespace HSp.CsEpo
{
	/// <summary>
	/// Verwaltet die Tabellen zu einer EpoGeneric-Klasse und deren Beziheung zueinander
	/// </summary>
	public class EpoGenericTableInfo
	{
		public string		mainTable;
		public Hashtable    linkedTables, linkedFields, aliasNames;

		public EpoGenericTableInfo(string mainTableName)
		{
			mainTable = mainTableName;

			linkedTables = new Hashtable();
			aliasNames = new Hashtable();
		}


		/// <summary>
		/// Fügt eine verknüpfte Tabelle hinzu.
		/// </summary>
		/// <param name="name">Der Name der Tabelle</param>
		/// <param name="whereClause">Der verknüpftende Anteil einer Where Clause</param>
		public void AddLinkedTable(string name, string whereClause) 
		{
			linkedTables.Add(name, whereClause);
		}

		/// <summary>
		/// Fügt eine verknüpfte Tabelle hinzu für die ein Alias-name verwendet wird
		/// </summary>
		/// <param name="name">Der Name der Tabelle</param>
		/// <param name="whereClause">Die verküpfende Anteil einer WhereClause</param>
		/// <param name="alias">Der Alias Name</param>
		public void AddLinkedTable(string name, string whereClause, string alias) 
		{
			linkedTables.Add(name, whereClause);
			aliasNames.Add(name, alias);
		}

		/// <summary>
		/// Ein verküpftes Feld hinzufügen
		/// </summary>
		/// <param name="fieldName">Der Name des Feldes</param>
		/// <param name="tableName">Der Name einer zuvor mit AddLinkedTable hinzugefügten Tabelle bzw. der Alias-Name einer Tabelle</param>
		/// <param name="originalName">Der Name des Feldes (Spalte) in der Tabelle</param>
		public void AddLinkedField(string fieldName, string tableName, string originalName) 
		{
			if(!mainTable.Equals(tableName) && !linkedTables.Contains(tableName) && !aliasNames.ContainsValue(tableName) )
			{
				throw new EpoException("Die Tabelle [" + tableName + "] wurde noch nicht zu den verknüpften Tabellen hinzugefügt!");
			}


			if(linkedFields==null)
				linkedFields = new Hashtable();


			linkedFields.Add(fieldName,  tableName + "." + originalName);
		}
	}
}
