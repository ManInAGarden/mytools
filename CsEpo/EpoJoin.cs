namespace HSp.CsEpo {

	using System;
	using System.Diagnostics;
	using System.Collections;
  	
  	//Ermöglicht die Deklaration von 1:-Verknüpfungen (auf der 1-Seite)
  	//Zu jedem Feld sind Verknüpfugen zu beliebig vielen verschiedenen Klassen möglich.
  	//EpoViews bauen daraus automatisch Komboboxen und füllen diese mit den referenzierbaren Objekten (aus
  	//den genannten Klassen).
	public class EpoJoin {
		public ArrayList		foreignClassNames;
		
		
		public EpoJoin(string foreignClassName) {
					   
			this.foreignClassNames = new ArrayList();
			this.foreignClassNames.Add(foreignClassName);
		}
	}

	
}