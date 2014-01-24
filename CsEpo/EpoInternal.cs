using System;
using System.Collections;
using System.Diagnostics;

namespace HSp.CsEpo
{
	/// <summary>
	/// Speichert alle Daten in einer internen statischen HashTable statt in einer Datenbank
	/// </summary>
	public class EpoInternal : Epo, IComparer
	{
		private static Hashtable	_classStorage;
		public string[]				ocParts;

		public EpoInternal() : base()
		{
			orderKey = stdOrderKey;
		}

		public EpoInternal(int	initialMax)  : this(initialMax, stdOrderKey)
		{
		}

		public EpoInternal(int	initialMax, string orderKey) 
		{
			Hashtable	myStorage;
			bool		haveToWork = false;

			this.orderKey = orderKey;

			InitializeStorage();

			haveToWork = SetupClassInfo();

			if(!haveToWork)
				return;

			if(_classStorage == null)
				_classStorage = new Hashtable();

			try
			{
				classInfos.SetKnownVerwo(orderKey, this.GetType().Name, this);
			}
			catch (Exception exc)
			{
				Trace.WriteLine("Die EPO-Klasse " + this.GetType().Name + " wurde mehrfach initialisiert.");

			}

			myStorage = new Hashtable(initialMax);
			_classStorage[ClassInfo().className] = myStorage;

			InitMyFieldMap();
			InitSpecialDbLen();

		}


		protected void IntializeItAll() 
		{
			EpoClassInfo		eci;
			bool				haveToWork = false;


			haveToWork = SetupClassInfo();

			eci = ClassInfo();

			if (tsw.TraceVerbose) 
			{
				Trace.WriteLine(String.Format("Initialisierung der Epoklasse <{0}>",
					eci.fullClassName));
			}


			try
			{
				classInfos.SetKnownVerwo(orderKey, this.GetType().Name, this);
			}
			catch (Exception exc)
			{
				Trace.WriteLine("Die EPO-Klasse " + this.GetType().Name + " wurde mehrfach initialisiert.");

			}

			if(!haveToWork)
				return;

			InitMyFieldMap();
			
			//Obwohl keine DB-Anbindung da ist wird das trotzdem gemacht, damit man irgendwo Joins und
			//Links usw. defnieren kann.
			InitSpecialDbLen();
		}


		private bool WhereClauseOK(EpoInternal epi, string wc) 
		{
			if(wc==null) return true;

			return true;
		}


		private void SortArray(ArrayList inErg, string[] ocp) 
		{
			ocParts = ocp;
			inErg.Sort(this);
		}



		/// <summary>
		/// Löscht alle Datensätze dieser Epo-Klasse
		/// </summary>
		public void TruncateData() 
		{
			Hashtable	rawData;

			rawData = GetRawData();
			if(rawData!=null)
				rawData.Clear();
		}


		/// <summary>
		/// Internes Select für EpoInternal
		/// </summary>
		/// <param name="where">Die Where-Clause</param>
		/// <param name="orderBy">Die Order Clause für die Sortierung der Rückgabewerte</param>
		/// <returns></returns>
		public override ArrayList InternalSelect(string wc, string oc) 
		{
			Hashtable				rawData;
			ArrayList				erg;
			string[]				ocParts;	
			char[]					ocSeps = {','};
			IDictionaryEnumerator	enu;
			EpoInternal				ei;
			string					ord;


			rawData = GetRawData();

			if (rawData==null) return null;

			if(rawData.Count==0) return new ArrayList();

			erg = new ArrayList();

			enu = rawData.GetEnumerator();
			while(enu.MoveNext())
			{
				ei = enu.Value as EpoInternal;
				ei.orderKey = orderKey;
				if(WhereClauseOK(ei, wc)) 
				{

					erg.Add(ei);
				}
			}

			if(oc==null)
				ord = PreferredOrdering();
			else
				ord = oc;

			if(ord!=null) 
			{
				ocParts = ord.Split(ocSeps);
				SortArray(erg, ocParts);
			}

			return erg;
		}


		private Hashtable GetRawData() 
		{
			Hashtable		rawData;
			EpoClassInfo	eci;

			eci = ClassInfo();
			rawData = _classStorage[eci.className] as Hashtable;

			return rawData;
		}

		public override void Flush()
		{
			Hashtable rawData;

			rawData = GetRawData();

			rawData[this.oid] = this;

		}



		public override bool Delete()
		{
			Hashtable	rawData;
			object		obj;

			rawData = GetRawData();

			obj = rawData[this.oid];
			if(obj!=null) 
			{
				rawData.Remove(obj);
			}


			return true;
			
		}
		#region IComparer Member


		public int Compare(object x, object y)
		{
			EpoInternal	ex, ey;
			int			answ = 0, i=0;
			IComparable	fox, foy;
			string		op;


			ex = x as EpoInternal;
			ey = y as EpoInternal;

			Debug.Assert((ex!=null) && (ey!=null));

			while((answ==0) && (i<ocParts.Length)) 
			{
				op = ocParts[i].Trim();
			
				fox = ex.GetPropValue(op) as IComparable;
				foy = ey.GetPropValue(op) as IComparable;
				
				Debug.Assert((fox!=null) && (foy!=null));

				answ = fox.CompareTo(foy);

				

				i++;
			}

			return answ;
		}

		#endregion
	}
}
