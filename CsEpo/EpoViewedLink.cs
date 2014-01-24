namespace HSp.CsEpo {

	delegate string DispDelegate();

	class EpoViewedLink {
		public string		className;
		public string 		foreignKeyName;
		public string		additionalWhere;
		public string		displayMethName;
		
		
		public EpoViewedLink(string cnam, string fk) {
			this.className = cnam;
			this.foreignKeyName = fk;
		}
		
		
		public EpoViewedLink(string cnam, string fk, string addWhere) {
			this.className = cnam;
			this.foreignKeyName = fk;
			this.additionalWhere = addWhere;
		}


		public EpoViewedLink(string cnam, string fk, string addWhere, string what) 
		{
			this.className = cnam;
			this.foreignKeyName = fk;
			this.additionalWhere = addWhere;
			this.displayMethName = what;
		}
	}
}