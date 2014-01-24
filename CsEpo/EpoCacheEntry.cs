using System;

namespace HSp.CsEpo
{
	/// <summary>
	/// Eintrag auf einem EpoCache - wertet die gespeichern Daten mit zusätzlicher
	/// Verwaltungsinformation auf
	/// </summary>
	public class EpoCacheEntry
	{

		//Diese Variable unterstützt die GarbageCollection auf dem Cache
		//und ist n i c h t  persistent!!
		private DateTime			_cacheLstHit = DateTime.Now;
		private object				_o;


		public object Value 
		{
			get {return _o; }
		}


		public EpoCacheEntry(Object o)
		{
			_o = o;
			_cacheLstHit = DateTime.Now;
		}


		public void RefreshHitTime() 
		{
			_cacheLstHit = DateTime.Now;
		}

		public bool IsDecayed(DateTime deathT, TimeSpan	decayTime) 
		{
			return (deathT - _cacheLstHit) > decayTime;
		}

	}
}
