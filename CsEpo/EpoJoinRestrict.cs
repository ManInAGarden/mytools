using System;

namespace HSp.CsEpo
{
	/// <summary>
	/// Zusammenfassung für EpoJoinRestrict.
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
