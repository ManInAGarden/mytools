namespace HSp.CsEpo {
	public class EpoMemo {
		private string 	text;
		
		
		public string Text {
			set {text = value;}
			get {return text;}
		}
		
		public EpoMemo(string intxt) {
			text = intxt;
		}
		
		
		public override string ToString() {
			return text;
		}
	}
}