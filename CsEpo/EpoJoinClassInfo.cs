using System;

namespace HSp.CsEpo
{
	/// <summary>
	/// Speichert temporär Informationen zu Ejo-Klassen
	/// </summary>
	public class EpoJoinClassInfo
	{
		Epo		m_ep;
		string	m_aliasName;


		public EpoJoinClassInfo(Epo ep)
		{
			m_ep = ep;
		}

		public string AliasName 
		{
			set{m_aliasName = value;}
			get{return m_aliasName;}
		}

		public Epo Ep 
		{
			get {return m_ep;}
		}
	}
}
