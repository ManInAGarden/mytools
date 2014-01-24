namespace HSp.CsEpo 
{
	using System;
	using System.Diagnostics;
	using System.Collections;
  	
	///<summary>
	///Implementiert eine GenricEpo-Klasse, die die Selects chached und bei erneutem Select auf den
	///cache zugreift.
	///</summary>
	public class EpoGenericCached : EpoGeneric
	{
		private static EpoCacheStorage 	selCache = new EpoCacheStorage(100);
		private static EpoCacheStorage 	objCache = new EpoCacheStorage(1000);

		private static Hashtable		sC = Hashtable.Synchronized(selCache);
		private static Hashtable		oC = Hashtable.Synchronized(objCache);

		private static int 				ochit=0,
										schit=0;
		private static Hashtable		cacheDelDelgates;


  		
		public EpoGenericCached(String connString) : base(connString) 
		{
		}


		public EpoGenericCached(String connString, string orderKey) : base(connString, orderKey) 
		{
		}
  		 
		public EpoGenericCached() : base() 
		{
		}


		public static int ObjCacheCount 
		{
			get {return oC.Count; }
		}

		public static int SelCacheCount 
		{
			get {return sC.Count; }
		}
  		
  		
		public override void Flush() 
		{
  			
			ClearSelectCache();
			CallDeleteDelegate();

			base.Flush();
  			
			StoreToObjectCache(this);
		}
  		
		// Ein Epo löschen. Da darin delete-rules gefolgt wird muss der
		// gesamte Cache abgeräumt werden. Könnte man später mal verbessern...
		public override bool Delete() 
		{
			bool answ;
		  			
			ClearSelectCache();
			ClearObjectCache();
			CallDeleteDelegate();

			answ = base.Delete();
		  			
			return answ;
		}
  		
  		
		public override int GetCount() 
		{
			ArrayList	arr;
  		
			arr = GetFromSelectCache("*");
			if(arr!=null) return arr.Count; 
  			
			return base.GetCount();
		}
  		 
		public override ArrayList Select() 
		{	
			ArrayList	answ;
			string		wildkey = orderKey + "." + GetType().Name + ":*";
  			
			answ = GetFromSelectCache(wildkey);
			if(answ!=null) return answ;
  			
			answ = base.Select();
			StoreToSelectCache(wildkey, answ);
  			
			return answ;
		}
  		
  		
		public override Epo ResolveOid(Oid oid) 
		{
			Epo ep;
  			
			if (oid==null) return null;
  			
			ep = GetFromObjectCache(oid);
			if(ep!=null) return ep;
  			
			ep = base.ResolveOid(oid);
			if(ep==null) return ep;
  			
			StoreToObjectCache(ep as EpoGenericCached);
  				
			return ep;
		}
  		
  		
		public override ArrayList Select(String where) 
		{
			string 		key = orderKey + "." + GetType().Name + ":";
			ArrayList	answ;
			  			
			if(where!=null)
				key += where;
			else
				key += "*";
			  				
			key += ":";
			  			
					  			
			answ = GetFromSelectCache(key);
			if(answ!=null) return answ;
			  			
			answ = base.Select(where);
			StoreToSelectCache(key, answ);
			  			
			return answ;
		}
  		
  		
  		
		public override ArrayList Select(IComparer comp) 
		{
			string 	key = orderKey + "." + GetType().Name + ":";
			ArrayList	answ;
				  			
		  	
			key += "*:";
				  			
				  	
			key += comp.GetType().Name;
				  	
				  			
			answ = GetFromSelectCache(key);
			if(answ!=null) return answ;
				  			
			answ = base.Select(comp);
			StoreToSelectCache(key, answ);
				  			
			return answ;
		}
  		
		public override ArrayList Select(String where, IComparer comp) 
		{
			string 		key = orderKey + "." + GetType().Name + ":";
			ArrayList	answ;
		  			
			if(where!=null)
				key += where;
			else
				key += "*";
		  				
			key += ":";
		  			
		  	
			key += comp.GetType().Name;
		  	
		  			
			answ = GetFromSelectCache(key);
			if(answ!=null) return answ;
		  			
			answ = base.Select(where, comp);
			StoreToSelectCache(key, answ);
		  			
			return answ;
		}
  		
  		
		public override ArrayList Select(String where, String orderBy) 
		{
			string 		key = orderKey + "." + GetType().Name + ":";
			ArrayList	answ;
  			
			if(where!=null)
				key += where;
			else
				key += "*";
  				
			key += ":";
  			
			if(orderBy!=null)
				key += orderBy;
  			
			answ = GetFromSelectCache(key);
			if(answ!=null) return answ;
  			
			answ = base.Select(where, orderBy);
			StoreToSelectCache(key, answ);
  			
			return answ;
		}
  		
  		
		public void ClearSelectCache() 
		{
			sC.Clear();	
			Trace.WriteLineIf(tsw.TraceVerbose, "Der Selekt-Cache wurde gelöscht");
		}
  		
  		
		public void ClearObjectCache() 
		{
			Hashtable sC = Hashtable.Synchronized(oC);

			sC.Clear();

			Trace.WriteLineIf(tsw.TraceVerbose, "Der Objekt Cache wurde gelöscht");
		}
  		
		private ArrayList GetFromSelectCache(string key) 
		{
			ArrayList	answ;
  			
			answ = sC[key] as ArrayList;
			if(answ!=null) 
			{
				schit++;
				if (tsw.TraceVerbose) 
				{
					Trace.WriteLineIf(tsw.TraceVerbose, String.Format("Select-Cache-Treffer: {0} für {1}-Elemente",
						key,
						answ.Count));
				}
			}
  			
			return answ;
		}
  		
  		
		private void StoreToSelectCache(string key, ArrayList arl) 
		{
			sC[key] = arl;
			Trace.WriteLineIf(tsw.TraceVerbose, "Selektionsergebnis für " + key + " auf dem Stack gespeichert");
			
			if(arl!=null)
				StoreToObjectCache(arl);
		}
  		
  		
		private string ObjectKey(Epo ep) 
		{
			return orderKey + ":" + ep.oid.OidStr;
		}


		private void StoreToObjectCache(EpoGenericCached cep) 
		{
			string ok = ObjectKey(cep);
			
			oC[ok] = cep;
			if (tsw.TraceVerbose) 
			{
				Trace.WriteLineIf(tsw.TraceVerbose, "GenericEpo für " + ok + "auf dem Object-Stack gespeichert");
			}
		}
  		
  		
		private void StoreToObjectCache(ArrayList arl) 
		{
			EpoGenericCached	cep;
			string				ok;
  			
			for(int i=0; i<arl.Count; i++) 
			{
				cep = arl[i] as EpoGenericCached;
				if(cep!=null) 
				{
					ok = ObjectKey(cep);
					oC[ok] = cep;
					if (tsw.TraceVerbose) 
					{
						Trace.WriteLineIf(tsw.TraceVerbose, "GenricEpo für " + ok + "auf dem Object-Stack gespeichert");
					}
				}
			}
		}
  		
		private EpoGenericCached GetFromObjectCache(Oid oid) 
		{
			EpoGenericCached	cep;
			string				ok = ObjectKey(this);
  			
			cep = oC[ok] as EpoGenericCached;
			if(cep!=null) 
			{
				ochit++;
				if (tsw.TraceVerbose) 
				{
					Trace.WriteLineIf(tsw.TraceVerbose, "GenericEpo für " + ok + "auf dem Object-Stack gefunden");
				}
			}
  			
			return cep;
		}


		/// <summary>
		/// Setzt den Delegaten der aufgerufen wird wenn der Select Cache gelöscht wird.
		/// </summary>
		/// <param name="orderKey">Der OrderKey</param>
		/// <param name="deleg">Der Delegat</param>
		public static void SetDeleteCacheDelegate(string ordKey, CacheDeleteDelegate deleg) 
		{
			if(cacheDelDelgates==null) cacheDelDelgates = new Hashtable();

			cacheDelDelgates[ordKey] = deleg;
		}

		/// <summary>
		/// Setzt den Delegaten der aufgerufen wird wenn der Select Cache gelöscht wird.
		/// </summary>
		/// <param name="deleg">Der Delegat</param>
		public void SetDeleteCacheDelegate(CacheDeleteDelegate deleg) 
		{
			if(cacheDelDelgates==null) cacheDelDelgates = new Hashtable();

			cacheDelDelgates[this.orderKey] = deleg;
		}


		/// <summary>
		/// Ruft den CacheDelete Delegaten auf sofern dieser gesetzt wurde
		/// </summary>
		private void CallDeleteDelegate() 
		{
			if(cacheDelDelgates==null) return;

			CacheDeleteDelegate cdel = cacheDelDelgates[orderKey] as CacheDeleteDelegate;
			if(cdel!=null)
				cdel();
		}





  		
	}
}
