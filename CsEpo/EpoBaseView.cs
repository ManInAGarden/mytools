namespace HSp.CsEpo {

	using System;
	using System.Collections;
	using System.Windows.Forms;
	using System.Diagnostics;
	using System.Drawing;
	using System.ComponentModel;
	using System.Reflection;

	/// <summary>
	/// Die Basisklasse fuer alle EpoViews
	/// </summary>
	public class EpoBaseView {
   		protected bool		           isVO = false; //Kennung für Verwaltungsobjekt
	    protected Container	           components;
		protected Hashtable	           viewedLinks;
		protected Hashtable			   restrictedJoins;
		protected Hashtable			   joinViewSettings;
		protected static Hashtable     viewKnowledge = new Hashtable();
    	protected Epo		           ep = null;
		protected Hashtable	           epoControls = null;


		public EpoBaseView() : base() {
			isVO = true;
		}

        public EpoBaseView(Epo viewedEpo, object parent) {
        }


		/// <summary>
		/// Fügt eine Liste hinzu die alle Verknüpfungen von
		/// Objekten der Klassen in classNames auf das aktulle geviewte Objekt
		/// anzeigt. Dabei wir für jede Zeile der Liste die Methode deren Name
		/// in what genannt wird aktiviert
		/// </summary>
		/// <param name="name">Der Name der Liste unter dem diese referenziert werden kann</param>
		/// <param name="className">Der Name der Klasse deren Inhalte angezeigt werden sollen</param>
		/// <param name="foreignKeyName">Der Name der Property die den ForeignKey enthaelt der auf die aktuelle Klasse verweist.</param>
		/// <param name="addWhere">Zusätzliche where clause zur Einschränkung der Liste. Kann auch null oder Nothing sein um nicht zusätzlich einzuschränken</param>
		/// <param name="what">Der Name der Methode die für die Darstellung der Listenobjete benutt wird.</param>
		public void AddViewedLink(string name,
			string className,
			string foreignKeyName,
			string addWhere,
			string what) 
		{
			Hashtable			evlt;
			EpoViewedLink		evl;
			EpoControl			econ;


			Debug.Assert(name!=null);
			Debug.Assert(className!=null);
			Debug.Assert(foreignKeyName!=null);

			if(viewedLinks == null)
				viewedLinks = new Hashtable();

			evlt = viewedLinks[name] as Hashtable;
			if(evlt==null) 
			{
				evlt = new Hashtable();
				viewedLinks[name] = evlt;
			}

			if (what==null)
				evl = new EpoViewedLink(className, foreignKeyName, addWhere);
			else
				evl = new EpoViewedLink(className, foreignKeyName, addWhere, what);

			evlt[className + ":" + foreignKeyName] = evl;

			econ = new EpoControl(name,
				1,
				1,
				10,
				50);

			econ.ctrl = new ListBox();
			econ.ctrl.Tag = name;
			epoControls[name] = econ;

		}


		/// <summary>
		/// Fügt einen Button hinzu
		/// </summary>
		/// <param name="butName"></param>
		/// <param name="e"></param>
		protected void AddButton(string name, int col, int line, int width, int height, EventHandler e) 
		{
			Button		bu;
			EpoControl	econ;

			econ = new EpoControl(name,
				col,
				line,
				width,
				height);

			bu = new Button();
			bu.Click += e;
			bu.Text = name;

			econ.ctrl = bu;
			econ.ctrl.Tag = name;
			epoControls[name] = econ;
		}


		/// <summary>
		/// Fügt eine Liste hinzu die alle Verknüpfungen von
		/// Objekten der Klassen in classNames auf das aktulle geviewte Objekt
		/// anzeigt. Die Dartsellung der Zeilen erfolgt dabei durch aktivierung der Methode
		/// ToString.
		/// </summary>
		/// <param name="name">Der Name der Liste unter dem diese referenziert werden kann</param>
		/// <param name="className">Der Name der Klasse deren Inhalte angezeigt werden sollen</param>
		/// <param name="foreignKeyName">Der Name der Property die den ForeignKey enthaelt der auf die aktuelle Klasse verweist.</param>
		/// <param name="addWhere">Zusätzliche where clause zur Einschränkung der Liste. Kann auch null oder Nothing sein um nicht zusätzlich einzuschränken</param>
		public void AddViewedLink(string name,
								  string className,
								  string foreignKeyName,
								  string addWhere) {

			AddViewedLink(name, className, foreignKeyName, addWhere, null);		
		}


		/// <summary>
		/// Schränkt einen im Epo definiertn Join zusätzlich ein, so dass nicht mehr alle 
		/// Objekte der Zielklasse verbunden werden können sondern nur diejenigen für die 
		/// die whereClause erfüllt ist. 
		/// </summary>
		/// <param name="fieldName">Der Feldname hinter dem der Joind liegt</param>
		/// <param name="foreignField">Das Feld im Ziel</param>
		/// <param name="myField">Das eigene Feld</param>
		public void RestrictJoin(string fieldName, string foreignField, string nativeField) 
		{


			Debug.Assert(fieldName!=null, "Der Feldname muss gesetzt ein");
			Debug.Assert(foreignField !=null);
			Debug.Assert(nativeField !=null);
	
			if(restrictedJoins == null)
				restrictedJoins = new Hashtable();


			restrictedJoins[fieldName] = new EpoJoinRestrict(foreignField, nativeField);
		}

		/// <summary>
		/// Legt fest welche Property in einer ComboBox angezeigt wird. Defaulteinstellung ist, dass
		/// von den Objekten in der ComboBox der Darstellungswert mit ToString ermittelt.
		/// </summary>
		/// <param name="fname">Der Feldname auf dem die ComboBox liegt</param>
		/// <param name="targetProp">Die Property die dargstellt werden soll</param>
		public void AddJoinViewSetting(string fname, string targetProp) 
		{
			JoinViewSetting		joyset;

			Debug.Assert(fname != null);
			Debug.Assert(targetProp != null);

			if (joinViewSettings == null)
				joinViewSettings = new Hashtable();

			joyset = new JoinViewSetting();
			joyset.targetProperty = targetProp;
			joinViewSettings[fname] = joyset;
		}
	}
}
