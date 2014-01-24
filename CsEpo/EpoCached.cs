namespace HSp.CsEpo {
	using System;
	using System.Diagnostics;
  	using System.Collections;

  	
	/// <summary>
	/// Der Delegate den man mit SetCacheDeleteDelegate setzen kann, der dann bei jedem Cache Delete
	/// aktiviert wird.
	/// </summary>
	public delegate void CacheDeleteDelegate();

  	///<summary>
  	///Implementiert eine Epo-Klasse, die die Selects cached und bei erneutem Select auf den
  	///Cache zugreift.
  	///</summary>
  	public class EpoCached : Epo {
  		private static EpoCacheStorage 	selCache = new EpoCacheStorage(100);
  		private static EpoCacheStorage 	objCache = new EpoCacheStorage(1000);

		private static Hashtable		sC = Hashtable.Synchronized(selCache);
		private static Hashtable		oC = Hashtable.Synchronized(objCache);

  		private static int 				ochit=0,
  										schit=0;
		private static Hashtable		cacheDelDelgates;
  		
  		
  		public EpoCached(String connString) : base(connString) {
  		}


		public EpoCached(String connString, string orderKey) : base(connString, orderKey) 
		{
		}
  		 
  		public EpoCached() : base() {
  		}
  		
		public static int ObjCacheCount 
		{
			get {return oC.Count; }
		}

		public static int SelCacheCount 
		{
			get {return sC.Count; }
		}


        /// <summary>
        /// Löscht alle Caches ud aktiviert registrierte Cache Delete-Delegaten
        /// </summary>
        public void ClearCaches() {
            ClearSelectCache();
            ClearObjectCache();
            CallDeleteDelegate();
        }


  		public override void Flush() {
  			
  			ClearSelectCache();
			CallDeleteDelegate();

  			base.Flush();
  			
  			StoreToObjectCache(this);
  		}
  		
  		//Ein Epo löschen. Da darin delete-rules gefolgt wird muss der
		// gesamte Cache abgeräumt werden. Könnte man später mal verbessern...
		public override bool Delete() {
			bool answ;
		  			
		  	ClearSelectCache();
		  	ClearObjectCache();
			CallDeleteDelegate();
			
			answ = base.Delete();
		  		  			
			return answ;
		}
  		
		public override int GetCount(string where) 
		{
			string 		key = orderKey + "." + GetType().Name + ":";
			ArrayList	arr;
			  			
			if(where!=null)
				key += where;
			else
				key += "*";
			
  		
			arr = GetFromSelectCache(key);
			if(arr!=null) return arr.Count; 
  			
			return base.GetCount(where);
		}
  		

  		public override int GetCount() {
  			return base.GetCount(null);
  		}
  		 
  		public override ArrayList Select() {	
  			ArrayList	answ;
  			string		wildkey = orderKey + "." + GetType().Name + ":*";
  			
  			answ = GetFromSelectCache(wildkey);
  			if(answ!=null) return answ;
  			
  			answ = base.Select();
  			StoreToSelectCache(wildkey, answ);
  			
  			return answ;
  		}
  		
  		
		public override Epo ResolveOid(string oidstr) 
		{
			Oid oid;

			oid = new Oid(oidstr);

			return ResolveOid(oid);
		}



  		public override Epo ResolveOid(Oid oid) {
  			Epo ep;
  			
  			if (oid==null) return null;
  			
  			ep = GetFromObjectCache(oid);
  			if(ep!=null) return ep;
  			
  			ep = base.ResolveOid(oid);
  			if(ep==null) return ep;
  			
  			StoreToObjectCache(ep as EpoCached);
  				
  			return ep;
  		}
  		
  		
  		public override ArrayList Select(String where) {
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
  		
  		
  		
  		public override ArrayList Select(IComparer comp) {
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
  		
  		public override ArrayList Select(String where, IComparer comp) {
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
  		
  		
  		public override ArrayList Select(String where, String orderBy) {
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
  		
  		
  		private void ClearSelectCache() {
  			sC.Clear();
  			Trace.WriteLineIf(tsw.TraceVerbose, "Der Selekt-Cache wurde gelöscht");
  		}
  		
  		
  		private void ClearObjectCache() {
  			oC.Clear();
  			Trace.WriteLineIf(tsw.TraceVerbose, "Der Objekt Cache wurde gelöscht");
  		}
  		
  		private ArrayList GetFromSelectCache(string key) {
  			ArrayList	answ;
  			
  			answ = sC[key] as ArrayList;
  			if(answ!=null) {
  				schit++;
  				if (tsw.TraceVerbose) {
  					Trace.WriteLine(String.Format("Selekt-Cache-Treffer: {0} für {1}-Elemente",
  												  key,
  												  answ.Count));
  				}
  			}
  			
  			return answ;
  		}
  		
  		
  		private void StoreToSelectCache(string key, ArrayList arl) {
			sC[key] = arl;
			Trace.WriteLineIf(tsw.TraceVerbose, "Selektionsergebnis für " + key + " auf dem Stack gespeichert");
			
			if(arl!=null)
				StoreToObjectCache(arl);
  		}
  		

		private string ObjectKey(Epo ep) 
		{
			return orderKey + ":" + ep.oid.OidStr;
		}

		private string ObjectKey(Oid o) 
		{
			return orderKey + ":" + o.OidStr;
		}
  		
		private void StoreToObjectCache(EpoCached cep) 
		{
			string ok = ObjectKey(cep);
			
			oC[ok] = cep;
			if (tsw.TraceVerbose) 
			{
				Trace.WriteLineIf(tsw.TraceVerbose, "Epo für " + ok + "auf dem Object-Stack gespeichert");
			}
		}
  		
  		
		private void StoreToObjectCache(ArrayList arl) 
		{
			EpoCached	cep;
			string		ok;
  			
			for(int i=0; i<arl.Count; i++) 
			{
				cep = arl[i] as EpoCached;
				if(cep!=null) 
				{
					ok = ObjectKey(cep);
					oC[ok] = cep;
					if (tsw.TraceVerbose) 
					{
						Trace.WriteLineIf(tsw.TraceVerbose, "Epo für " + ok + "auf dem Object-Stack gespeichert");
					}
				}
			}
		}
  		
		/// <summary>
		/// Liest ein Objekt vom Cache
		/// </summary>
		/// <param name="oid">Die oid des gesuchten Objektes.</param>
		/// <returns>Das gfundene Epo-Objekt oder null wenn es nicht auf dem Cache gefunden wurde.</returns>
		private EpoCached GetFromObjectCache(Oid oid) 
		{
			EpoCached			cep;
			string				ok = ObjectKey(oid);
  			
			cep = oC[ok] as EpoCached;
			if(cep!=null) 
			{
				ochit++;
				if (tsw.TraceVerbose) 
				{
					Trace.WriteLineIf(tsw.TraceVerbose, "Epo für " + ok + "auf dem Object-Stack gefunden");
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
