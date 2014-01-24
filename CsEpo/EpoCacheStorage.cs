using System;
using System.Collections;

namespace HSp.CsEpo
{
	/// <summary>
	/// Realisiert den CacheSpeicher für EpoKlassen
	/// </summary>
	public class EpoCacheStorage : Hashtable
	{
		private static TimeSpan		_objCacheDecayTime = new TimeSpan(0,2,0);
	

		public override object this [object key]   // Indexer declaration
		{
			get 
			{
				object			reto = base[key];
				EpoCacheEntry	ece;
				
				
				if(reto==null) return null;

				ece = reto as EpoCacheEntry;
				ece.RefreshHitTime();

				return ece.Value;
			}
			set 
			{
				EpoCacheEntry ece = new EpoCacheEntry(value);
				base[key] = ece;

				CleanSweep();
			}
		}



		public EpoCacheStorage() : base()
		{
		}

		public EpoCacheStorage(int size) : base(size) 
		{
		}


		

		/// <summary>
		/// Den Object-Cache so abräumen, dass alle Objekte die älter sind als die Decay-Zeitspanne
		/// entfernt werden.
		/// </summary>
		public void CleanSweep() 
		{
			IDictionaryEnumerator	enu;
			ArrayList				delCandidates = new ArrayList();
			EpoCacheEntry			ece;
			DateTime				deathTime = DateTime.Now;

			enu = GetEnumerator();
			while(enu.MoveNext()) 
			{
				ece = enu.Value as EpoCacheEntry;
				if(ece.IsDecayed(deathTime, _objCacheDecayTime))
					delCandidates.Add(enu.Key);
			}

			foreach (object delCandKey in delCandidates) 
			{
				Remove(delCandKey);
			}
			
		}




	}
}
