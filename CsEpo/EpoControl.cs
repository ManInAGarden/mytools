namespace HSp.CsEpo {
 	
 	using System;
 	using System.Windows.Forms;

	class EpoControl : IComparable {
		public string	label;
		public int		labLine, labColumn,
						line, column,
						lineSpan, colSpan,
						buLine, buCol;
		public bool		visible = true,
						enabled = true,
						edilink = false;
		public Control	ctrl;
		public int		number;
		
		
		public EpoControl(string lab, int c, int l, int colspan, int linespan) {
			this.label = lab;
			this.line = l;
			this.column = c;
			this.lineSpan = linespan;
			this.colSpan = colspan;
			this.labLine = line;
			this.labColumn = 1;
			this.number = Int32.MaxValue; // Markerung für nicht gesetzte Reihenfolge
		}
		
		/// <summary>
		/// Vergleichsmethode fuer EpoControls
		/// </summary>
		/// <param name="obj">Das Objekt mit dem das aktuelle Objekt verglichen wird</param>
		/// <returns>Gibt 0 zurück wenn beide Objekte gleich sind. Je nachdem welches Objekt kliener ist wird -1 oder +1 zurückgegeben.</returns>
		public int CompareTo(object obj) {
			if(obj is EpoControl) {
			    EpoControl	otherEco = obj as EpoControl;
			    long		myPosCode, otherPosCode;
		
				myPosCode = 40000 * this.line + this.column;
				otherPosCode = 40000 * otherEco.line + otherEco.column;
				
		        return myPosCode.CompareTo(otherPosCode);
		    }
		        
		    throw new ArgumentException("object is not a Temperature");    
		}

		public override string ToString()
		{
				  return number.ToString() + ":" + label;
		}

		
	}
	
}