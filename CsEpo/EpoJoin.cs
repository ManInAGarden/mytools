namespace HSp.CsEpo {

	using System;
	using System.Diagnostics;
	using System.Collections;
  	
  	//Erm�glicht die Deklaration von 1:-Verkn�pfungen (auf der 1-Seite)
  	//Zu jedem Feld sind Verkn�pfugen zu beliebig vielen verschiedenen Klassen m�glich.
  	//EpoViews bauen daraus automatisch Komboboxen und f�llen diese mit den referenzierbaren Objekten (aus
  	//den genannten Klassen).
	public class EpoJoin {
		public ArrayList		foreignClassNames;
		
		
		public EpoJoin(string foreignClassName) {
					   
			this.foreignClassNames = new ArrayList();
			this.foreignClassNames.Add(foreignClassName);
		}
	}

	
}