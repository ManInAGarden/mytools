using System;

namespace HSp.CsEpo
{
	/// <summary>
	/// Exception, die bei Ungereimtheiten in der Benutzung von Epo-Klassen geworfen wird.
	/// </summary>
	public class EpoException : ApplicationException
	{
		public EpoException() : base()
		{
		}


		public EpoException(string msg) : base(msg) 
		{
		}


		public EpoException(string msg, Exception exc) : base(msg, exc) 
		{

		}
	}
}
