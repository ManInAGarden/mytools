using System;
using System.Collections;

namespace HSp.CsEpo
{


	public class EpoPickInfo
	{
		public bool		constrain = false;
		public string	foreignField;

		public EpoPickInfo(string ff, bool constr) 
		{
			foreignField = ff;
			constrain = constr;
		}
	}

	
	
	/// <summary>
	/// Verwaltung von Pickinformationen zu Joins
	/// </summary>
	public class EpoPick
	{
		


		public Hashtable	pickfields;

		public EpoPick()
		{
			pickfields = new Hashtable();
		}



		public void AddPickField(string nativeName, string foreignName, bool constrain) 
		{
			pickfields[nativeName] = new EpoPickInfo(foreignName, constrain);
		}
	}

}
