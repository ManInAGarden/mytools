using System;
using System.Collections;

namespace HSp.CsEpo
{
	/// <summary>
	/// Zusammenfassung f�r EpoGenericClassInfo.
	/// </summary>
	public class EpoGenericClassInfo : EpoClassInfo
	{
		public EpoGenericTableInfo	tableInfo; 
							
	
		
		public EpoGenericClassInfo()
		{
			// Nur die Fieldmap wird gebraucht das andere wird hier bewusst weggelassen
			fieldMap = new Hashtable();
		}
	}
}
