using System;
using System.Diagnostics;
using System.Collections;

namespace HSp.CsEpo
{
	/// <summary>
	/// Klasse zum Speichern Userabhängiger Epo-Infos, wird für Web-Applikationen gebraucht die i.a. keine
	/// static Variablen vertragen
	/// </summary>
	public class EpoClassInfoStore
	{
		private Hashtable		_ci, _kv;
		
		public EpoClassInfoStore()
		{
			_ci = new Hashtable();
			_kv = new Hashtable();
		}

		public EpoClassInfoStore(string	okey) : this()
		{
			Hashtable	classes, verwos;

			classes = new Hashtable();
			verwos = new Hashtable();
			_ci[okey] = classes;
			_kv[okey] = verwos;
		}


		public Hashtable GetClassInfos(string uname) 
		{
			Hashtable	answ;
			lock(_ci) 
			{
				answ = _ci[uname] as Hashtable;
			}

			return answ;
		}

		public EpoClassInfo GetClassInfo(string uname, string className)
		{
			EpoClassInfo	answ;
			Hashtable		citab;

			lock(_ci) 
			{
				citab = _ci[uname] as Hashtable;
				if(citab==null) return null;

				answ = citab[className] as EpoClassInfo;
			}

			return answ;
	}


		public Hashtable GetClassInfos() 
		{
			Hashtable	answ;

			lock(_ci) 
			{
				answ = _ci['*'] as Hashtable;
			}

			return answ;
		}

		public Hashtable GetAllClassInfos() 
		{
			return _ci;
		}


		public Hashtable GetAllKnownVerwos() 
		{
			return _kv;
		}


		public void RemoveClassInfo(string orderKey, string className) 
		{
			Hashtable	oci;

			oci = GetClassInfos(orderKey);
			
			if(oci==null) return;

			lock(oci) 
			{
				oci.Remove(className);
			}
		}


		public void SetClassInfo(string className, EpoClassInfo eci) 
		{
			SetClassInfo(Epo.GetStdOrderKey(), className, eci);
		}


		public void SetClassInfo(string orderKey, string className, EpoClassInfo eci) 
		{
			Hashtable	cis;


			cis = GetClassInfos(orderKey);
			if(cis==null) 
			{
				cis = new Hashtable();
				lock(_ci) 
				{
					_ci.Add(orderKey, cis);
				}
			}

			lock(cis) 
			{
				cis[className] = eci;
			}
		}

		public void SetKnownVerwo(string className, Epo epv) 
		{
			SetKnownVerwo(Epo.GetStdOrderKey(), className, epv);
		}


		public void SetKnownVerwo(string orderKey, string className, Epo epv) 
		{
			Hashtable	kvs;

			kvs = GetKnownVerwos(orderKey);

			if(kvs==null) 
			{
				kvs = new Hashtable();
				lock(_kv) 
				{
					_kv[orderKey] =  kvs;
				}
			}

			lock(kvs) 
			{
				kvs[className] = epv;
			}
		}

		
		public Hashtable GetKnownVerwos() 
		{
			return GetKnownVerwos(Epo.GetStdOrderKey());
		}


		public Hashtable GetKnownVerwos(string orderKey) 
		{
			Hashtable	answ;

			lock(_kv) 
			{
				answ = _kv[orderKey] as Hashtable;
			}

			return answ;
		}


		public Epo GetKnownVerwo(string orderKey, string className) 
		{
			Hashtable	kvs;
			Epo			answ;

			kvs = GetKnownVerwos(orderKey);
			if(kvs==null) return null;

			lock(kvs) 
			{
				answ = kvs[className] as Epo;
			}

			return answ;
		}


	}
}
