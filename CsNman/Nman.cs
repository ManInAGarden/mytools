namespace HSp.CsNman {
	using System.Diagnostics;
	using System.Collections;
	
	
	public delegate void NDelegate(NMessage msg);
	
	//Nachichtenmanager
	public class Nman {
		private static Nman		instance;
		private	Hashtable		registry;
		
		
		public Nman () {
			registry  = new Hashtable();
		}
		
		public static Nman Instance {
			get {
				if (instance==null)
					instance = new Nman();
					
				return instance;
			}
		}



		//Alle Abonnenten zu einem titel entfernen
		//--alle Abbos dieser Zeitung kündigen--
		public void UnregisterByTitle(string title) {
			Debug.Assert(title!=null);
			
			registry.Remove(title);
		}
		
		
		//Das Abonnentenobjekt aus allen titeln entfernen
		//--Alle Abbos kündigen--
		public void UnRegister(object subscribingObj) {
			IDictionaryEnumerator	titleEnum;
			Hashtable				subs;
			ArrayList				remList = new ArrayList();
							
			titleEnum = registry.GetEnumerator();
			titleEnum.Reset();
			
			while(titleEnum.MoveNext()) {
				subs = titleEnum.Value as Hashtable;
				if(subs!=null) {
					if(subs.ContainsKey(subscribingObj)) {
						subs.Remove(subscribingObj);
					}
					
					if(subs.Count==0)
						remList.Add(titleEnum.Key);
				}
			}
			
			//Titel bereinigen
			for(int i=0; i<remList.Count; i++) {
				registry.Remove(remList[i] as string);
			}
		}
		
		//Einen Delegate für Nachrichten eines titlels registrieren
		public void Register(string		ntitle,
							 object		subscribingObj,
							 NDelegate	handlingRoutine) {
							 
			Subscriber	subs = new Subscriber(subscribingObj, handlingRoutine);
			Hashtable	subsForTitle;
			
			Debug.Assert(ntitle!=null);
			subsForTitle = registry[ntitle] as Hashtable;
			
			if(subsForTitle == null) {
				subsForTitle = new Hashtable();
				registry[ntitle] = subsForTitle;
			}
			
			subsForTitle[subscribingObj] = subs;
		}
		
		
		public void Send(NMessage	msg) {
			Hashtable				subsForTitle;
			Subscriber				subs;
			IDictionaryEnumerator	subsEnum;
			ArrayList				subsToExec = new ArrayList();
			
			//Die Nachricht an alle Abonnenten weiterleiten
			
			subsForTitle = registry[msg.Title] as Hashtable;
			if(subsForTitle!=null) {
				subsEnum = subsForTitle.GetEnumerator();
				subsEnum.Reset();
				while(subsEnum.MoveNext()) {
					subs = subsEnum.Value as Subscriber;
					subsToExec.Add(subs);
				}
				
				for(int i=0; i<subsToExec.Count; i++) {
					(subsToExec[i] as Subscriber).ExecDelegate(msg);
				}
			}
		}
	}
}