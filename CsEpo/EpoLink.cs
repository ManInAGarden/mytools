namespace HSp.CsEpo {

	using System;
	using System.Diagnostics;
	using System.Collections;
  	
  	
  	public enum EnumDelRule {cutOff = 0, cascade = 1};
  	public enum EnumCopyRule {setnull = 0, copy = 1, link = 2};
  	
	public class EpoLink {
		public EnumDelRule		delRule;
		public EnumCopyRule		copyRule;
		public string			foreignFieldName;
		
		
		public EpoLink(string			foreignFieldName, 
					   EnumDelRule		delRule, 
					   EnumCopyRule		copyRule) {
					   
			this.foreignFieldName = foreignFieldName;
			this.delRule = delRule;
			this.copyRule = copyRule;
		}
	}

	
}