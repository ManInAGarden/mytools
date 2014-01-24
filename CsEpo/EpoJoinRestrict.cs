using System;

namespace HSp.CsEpo
{
	/// <summary>
	/// Zusammenfassung f�r EpoJoinRestrict.
	/// </summary>
	public class EpoJoinRestrict
	{
		public string foreignFieldName, nativeFieldName;

		public EpoJoinRestrict(string foreign, string native)
		{
			foreignFieldName = foreign;
			nativeFieldName = native;
		}
	}
}
