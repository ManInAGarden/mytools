using System.Configuration;


namespace HSp.CsEpo 
{

	using System;
	using System.Collections;
	using System.Windows.Forms;
	using System.Diagnostics;
	using System.Drawing;
	using System.ComponentModel;
	using System.Reflection;

	public class EpoView : EpoBaseView 
	{

		private bool		debugView = GetDebugViewSetting();

		private	object		par; // sollte entweder Form oder EpoView sein
		private Hashtable  	miTagList = new Hashtable();
		private String		lastFieldName;
		private ListBox		lastLb;
		protected int		colWidth = 10, lineHeight,
							lineSpacing = 2;
		protected Form      myWinForm;
		public bool			chainedRefresh = false;
		private bool		doFocus = false;



		private static bool GetDebugViewSetting() 
		{
			string oStr;

			oStr = ConfigurationSettings.AppSettings["DebugView"];
			if (oStr==null) return false;
			
			return oStr.ToUpper().Equals("TRUE");
		}

		public EpoView() : base() 
		{
		}

		public EpoView(Epo viewedEpo, object parent) : base(viewedEpo, parent) 
		{

			Debug.Assert((parent is Form) || (parent is EpoView),
				"Der Parent für einen EpoView muss entweder selbst von EpoView oder Form abgeleitet sein");


			isVO = false;
			myWinForm = new Form();

			myWinForm.Load += new EventHandler(myWinForm_Load);
			myWinForm.Activated += new EventHandler(myWinForm_Activated);
			myWinForm.ShowInTaskbar = false;

			ep = viewedEpo;
			par = parent;

			if (parent is Form)
				myWinForm.Owner = parent as Form;
			else
				myWinForm.Owner = (parent as EpoView).AssocForm;

			epoControls = new Hashtable();
			InitSpecialControls();
			GenerateTabIndex();
			InitializeComponents();
		}


		public Form AssocForm 
		{
			get { return myWinForm;}
		}


		// Fügt die Editierbuttons zu einer Linkliste hinzu. Die Butons für
		// neu, del, dup und open werden waagerecht an der durch line und
		// col definiertn Position platziert
		public void SetLinkEditable(string name, int col, int line) 
		{
			EpoControl	econ;
			
			econ = epoControls[name] as EpoControl;
			
			Debug.Assert(econ != null);
			
			econ.buLine = line;
			econ.buCol = col;
			econ.edilink = true;
		}
		
		
		public static void AddEpoViewRelation(Epo epoVO,
			EpoView epoViewVO) 
		{

			Debug.Assert(epoVO != null);
			Debug.Assert(epoViewVO != null);

			viewKnowledge[epoVO.GetType().AssemblyQualifiedName] = epoViewVO.GetType().AssemblyQualifiedName;
		}


		//Sollte bei Sonderwünschen überladen werden
		//Ansonsten wird hier in der vorgefundenen Reihenfolge
		//je ein Label und ein Control definiert.
		protected virtual void InitSpecialControls() 
		{
			EpoClassInfo   			eci;
			IDictionaryEnumerator	fiEnum;
			FieldInfo				fi;
			int						aktCol = 11,
				aktLine = 1;
			EpoControl				econ;
			string					currKey;
			
			Debug.Assert(ep != null);
			Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeeignet für diese Operation");
			
			eci = Epo.ClassInfo(ep);
			fiEnum = eci.fieldMap.GetEnumerator();

			
			fiEnum.Reset();
			while(fiEnum.MoveNext()) 
			{
				currKey = fiEnum.Key as String;
				if(currKey!="oid") 
				{
					fi = fiEnum.Value as FieldInfo;
				
					if (Epo.tsw.TraceVerbose) 
					{
						Trace.WriteLine("Controleintrag für " + currKey + " wird erzeugt");
					}
				
					econ = new EpoControl(currKey,
						aktCol,
						aktLine,
						10,
						1);
					
					switch(fi.csTypeName) 
					{
						case "String":
							econ.ctrl = new TextBox();
							break;
						case "int":
							econ.ctrl = new TextBox();
							break;
						case "Int32":
							econ.ctrl = new TextBox();
							break;
						case "DateTime":
							econ.ctrl = new DateTimePicker();
							break;
						case "TimeSpan":
							DateTimePicker mydtp;
							
							mydtp = new DateTimePicker();
							mydtp.Format = DateTimePickerFormat.Custom;
							mydtp.ShowUpDown = true;
							mydtp.CustomFormat = "HH:mm";
							econ.ctrl = mydtp;
							break;
						case "Boolean":
							econ.ctrl = new CheckBox();
							break;
						case "Float":
							econ.ctrl = new TextBox();
							break;
						case "Double":
							econ.ctrl = new TextBox();
							break;
						case "Decimal":
							econ.ctrl = new TextBox();
							break;
						case "Stream":
							econ.ctrl = new TextBox();
							break;
						case "EpoMemo":
							TextBox mtb;
							mtb = new TextBox();
							mtb.Multiline = true;
							mtb.WordWrap = true;
							econ.ctrl = mtb;
							break;
						case "Oid":
							//Wenn zu dieser Oid Joins definiert wurden eine ComboBox
							//sonst ein normales Textfeld anlegen
							if(ep.GetJoins(currKey)!=null)
								econ.ctrl = new ComboBox();
							else
								econ.ctrl = new TextBox();
							break;
						default:
							String errstr = String.Format("Unbekannte Typ: <{0}. Kann Control für View nicht ermitteln>", fi.csTypeName);
							Trace.WriteLine(errstr);
							throw new Exception(errstr);
					}	


					econ.enabled = fi.persist;

					epoControls[currKey] = econ;
					
					
					aktLine += econ.lineSpan;
				}
			}
			
			
		}
		
		
		//Override für andere Titel
		protected virtual string DialogTitle 
		{
			get 
			{
				return ep.ToString();
			}
		}
		
		
		//Position und Größe setzen
		protected void SetGeometry(string fieldName,
			int labColumn, int labLine,
			int column, int line, 
			int colSpan, int lineSpan) 
		{
								   
			Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeeignet für diese Operation");
			
			EpoControl	econ;
			
			Debug.Assert(epoControls != null);
			
			econ = epoControls[fieldName] as EpoControl;
			
			Debug.Assert(econ != null, "Unbekannter Feldname ins SetGeometry");
			
			econ.labColumn = labColumn;
			econ.labLine = labLine;
			econ.column = column;
			econ.line = line;
			econ.colSpan = colSpan;
			econ.lineSpan = lineSpan;

		}
		
		
		//Das Control mit dem Namen <fieldName> zurückgeben
		protected Control GetControl(string fieldName) 
		{
			Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeiegnet für diese Operation");
			
			EpoControl	econ;
					
			Debug.Assert(epoControls != null);

			econ = epoControls[fieldName] as EpoControl;
					
			Debug.Assert(econ != null, "Unbekannter Feldname ins SetGeometry");
					
			return econ.ctrl;
		}
		
		//Setzt das Label für das Bedienelement auf einen anderen Wert
		protected void SetLabel(string fieldName, string label) 
		{
			EpoControl	econ;
			
			Debug.Assert(epoControls != null);
			
			econ = epoControls[fieldName] as EpoControl;
			
			Debug.Assert(econ != null, "Unbekannter Feldname <" + fieldName + "> in SetLabel");
			
			econ.label = label;
			
		}
		
		//Setzt das Label für das Bedienelement auf einen anderen Wert
		protected void SetEnabled(string fieldName, bool enastat) 
		{
			EpoControl	econ;
			
			Debug.Assert(epoControls != null);
			
			econ = epoControls[fieldName] as EpoControl;
			
			Debug.Assert(econ != null, "Unbekannter Feldname <" + fieldName + "> in SetLabel");
			
			econ.enabled = enastat;
			
		}


	

		//setzt den Sichtbarkeitsstatus auf einen anderen Wert
		protected void SetVisibility(string fieldName, bool vis) 
		{
			Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeeignet für diese Operation");
			
			EpoControl	econ;
					
			Debug.Assert(epoControls != null);
					
			econ = epoControls[fieldName] as EpoControl;
					
			Debug.Assert(econ != null);
					
			econ.visible = vis;
		
		}
		
		
		//Die EpoCtrl Number anhand der Positionen setzen. Diese wirkt sich
		//später auf den TabIndex jedes einzelnen Controls aus
		protected void GenerateTabIndex() 
		{
			IDictionaryEnumerator	ecoEnum;
			ArrayList				sortableEcos;
			int						ct;
			
			sortableEcos = new ArrayList(epoControls.Count);
			ecoEnum = epoControls.GetEnumerator();
			ecoEnum.Reset();
			while(ecoEnum.MoveNext()) 
			{
				sortableEcos.Add(ecoEnum.Value);
			}
			
			sortableEcos.Sort();
			
			//Nun ist alles nach Positionen sortiert
			ct = 1;
			foreach (EpoControl myEc in sortableEcos) 
			{
				myEc.number = ct;
				ct++;
			}
			
		}
		
		
		//Aus den EpoControls den Dialog aufbauen
		private void InitializeComponents() 
		{
			EpoControl				eco;
			Label					lab;
			IDictionaryEnumerator   ecoEnum;
			int						lowestLine = 0,
				leftestCol = Int32.MaxValue,
				rightestCol = 0,
				buttonLine;
			int						labWidth;
			Font					frmFont;
			int						buttonTabIdx = 1000;
		
			Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeeignet für diese Operation");
			components = new System.ComponentModel.Container ();
			
			frmFont = myWinForm.Font;
			
			lineHeight = (int)(frmFont.Height * 1.9);
			
			ToolTip	tt = new ToolTip();
			
			
			ecoEnum = epoControls.GetEnumerator();
			ecoEnum.Reset();
			while(ecoEnum.MoveNext()) 
			{
				eco = ecoEnum.Value as EpoControl;
				
				Debug.Assert(eco!=null);

				if(eco.visible) 
				{
					//Zuerst das Label
					if((eco.label!=null) && (eco.label.Length>0) && !(eco.ctrl is Button)) 
					{
						if(eco.labLine==eco.line)
							labWidth = (eco.column - eco.labColumn) * colWidth;
						else
							labWidth = eco.label.Length * colWidth;

						lab = NewLabel(eco.label,
							eco.labColumn * colWidth,
							eco.labLine * lineHeight + (int)(lineHeight * 0.1),
							labWidth,
							eco.lineSpan * lineHeight - 5);


						if(debugView) tt.SetToolTip(lab, "LAB:" + (ecoEnum.Key as String));

						myWinForm.Controls.Add(lab);
					}


					//Nun das Control
					eco.ctrl.Location = new Point(eco.column * colWidth, eco.line * lineHeight);
					eco.ctrl.Size = new Size(eco.colSpan * colWidth, eco.lineSpan * lineHeight - lineSpacing);
					eco.ctrl.Enabled = eco.enabled;
					eco.ctrl.TabIndex = eco.number;

					if(eco.ctrl is ListBox) 
					{
						(eco.ctrl as ListBox).DoubleClick += new EventHandler(ListDbl_Click);
						if (eco.edilink) 
						{
							Button	goBU, delBU, dupBU, newBU;

							goBU = NewButton(">",
								eco.buCol * colWidth,
								eco.buLine * lineHeight + (int)(0.1*lineHeight),
								(int)(1.8 * colWidth),
								(int)(0.8 * lineHeight));
							goBU.Tag = eco.ctrl;
							goBU.Click += new EventHandler(GoBuClicked);
							goBU.TabIndex = buttonTabIdx++;
							myWinForm.Controls.Add(goBU);
							
							newBU = NewButton(Epo.GetStdOrderKey(),
								(eco.buCol + 2) * colWidth,
								eco.buLine * lineHeight + (int)(0.1*lineHeight),
								(int)(1.8*colWidth),
								(int)(0.8 * lineHeight));
							newBU.Tag = eco.ctrl;
							newBU.Click += new EventHandler(NewBuClicked);
							newBU.TabIndex = buttonTabIdx++;
							myWinForm.Controls.Add(newBU);

							dupBU = NewButton("2",
								(eco.buCol + 4) * colWidth,
								eco.buLine * lineHeight + (int)(0.1*lineHeight),
								(int)(1.8 * colWidth),
								(int)(0.8 * lineHeight));
							dupBU.Tag = eco.ctrl;
							dupBU.Click += new EventHandler(DupBuClicked);
							dupBU.TabIndex = buttonTabIdx++;
							myWinForm.Controls.Add(dupBU);

							delBU = NewButton("X",
								(eco.buCol + 6) * colWidth,
								eco.buLine * lineHeight + (int)(0.1*lineHeight),
								(int)(1.8 * colWidth),
								(int)(0.8 * lineHeight));
							delBU.Tag = eco.ctrl;
							delBU.Click += new EventHandler(DelBuClicked);
							delBU.TabIndex = buttonTabIdx++;
							myWinForm.Controls.Add(delBU);
						}
					} 
					else if (eco.ctrl is Button) 
					{
						Button	bu = eco.ctrl as Button;

						bu.Text = eco.label;
					}

					if (debugView) tt.SetToolTip(eco.ctrl, ecoEnum.Key as string);					

					myWinForm.Controls.Add(eco.ctrl);

					if(eco.labLine > lowestLine) lowestLine = eco.labLine;
					if((eco.line + eco.lineSpan) > lowestLine) lowestLine = eco.line + eco.lineSpan - 1;
					if(eco.labColumn < leftestCol) leftestCol = eco.labColumn;
					if(eco.column < leftestCol) leftestCol = eco.column;
					if((eco.column + eco.colSpan) > rightestCol) rightestCol = eco.column + eco.colSpan;
				}
			}


			//Nun die Buttons anhängen
			
			buttonLine = lowestLine + 2;
			Button okB = NewButton("OK", 
				leftestCol * colWidth,
				buttonLine * lineHeight, 
				80,
				lineHeight);
									
			okB.TabIndex = buttonTabIdx++;
			okB.Click += new EventHandler(OkB_Click);
			
			Button cancB = NewButton("Abbrechen", 
				leftestCol * colWidth + 90,
				buttonLine * lineHeight,
				80,
				lineHeight);
								  
			cancB.TabIndex = buttonTabIdx++;
			cancB.Click += new EventHandler(CancB_Click);

			
			myWinForm.Controls.Add(okB);
			myWinForm.Controls.Add(cancB);
			
			myWinForm.Deactivate += new EventHandler(ThisForm_Deactivate);

			myWinForm.Text = DialogTitle;
			myWinForm.Size = new Size((rightestCol + 2) * colWidth, (buttonLine + 2) * lineHeight + 30);

			myWinForm.AcceptButton = okB;
			myWinForm.CancelButton = cancB;

			myWinForm.FormBorderStyle = FormBorderStyle.FixedDialog;
			myWinForm.MaximizeBox = false;
			myWinForm.MinimizeBox = false;

			myWinForm.StartPosition = FormStartPosition.Manual;
			int centerX = myWinForm.Owner.DesktopLocation.X + (int)((myWinForm.Owner.Size.Width - myWinForm.Size.Width) / 2);
			int centerY = myWinForm.Owner.DesktopLocation.Y + (int)((myWinForm.Owner.Size.Height - myWinForm.Size.Height) / 2);

			if (centerX < 0) centerX = 0;
			if (centerY < 0) centerY = 0;

			myWinForm.Location = new Point(centerX, centerY);

			FillWithData();
		}


		protected virtual bool UnloadFromData() 
		{
			bool 					answ = true;
			EpoControl				eco;
			IDictionaryEnumerator	ecoEnum;
			TextBox					tb;
			CheckBox				cb;
			ComboBox				comb;
			DateTimePicker			dtp;
			FieldInfo				fi;
			
			Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeiegnet für diese Operation");
			
			//Über alle Controls laufen und die Werte aus dem Control wieder in das epo schreiben
			ecoEnum = epoControls.GetEnumerator();
			ecoEnum.Reset();
			while(ecoEnum.MoveNext()) 
			{
				eco = ecoEnum.Value as EpoControl;
				if(eco.visible) 
				{
					if(eco.ctrl is TextBox) 
					{
						fi = ep.GetFieldInfo(ecoEnum.Key as string);
						if (fi.persist) 
						{
							tb = eco.ctrl as TextBox;
							if (tb.Text != null) 
							{
								switch (fi.csTypeName) 
								{
									case "EpoMemo":
										EpoMemo epm = new EpoMemo(tb.Text);
										ep.SetPropValue(ecoEnum.Key as string, epm);
										break;
									case "Double":
										double d;
										try 
										{
											d = Double.Parse(tb.Text);
											ep.SetPropValue(ecoEnum.Key as string, d);
										} 
										catch(Exception exc) 
										{
											Trace.WriteLine(exc.Message);
											MessageBox.Show("Bitte einen Double Wert für "+  ecoEnum.Key + " eingeben.");
											answ = false;
										}
										break;
									case "Int32":
										int myint;
										try 
										{
											if (tb.Text.Length > 0) 
											{
												myint = Int32.Parse(tb.Text);
												ep.SetPropValue(ecoEnum.Key as string, myint);
											} 
											else
												ep.SetPropValue(ecoEnum.Key as string, null);
										} 
										catch(Exception exc) 
										{
											Trace.WriteLine(exc.Message);
											MessageBox.Show("Bitte einen Integer Wert für "+  ecoEnum.Key + " eingeben.");
											answ = false;
										}
										break;

									case "Oid":
										Oid myOid;

										if (tb.Text.Length>0)
											myOid = new Oid(tb.Text);
										else
											myOid = null;

										ep.SetPropValue(ecoEnum.Key as string, myOid);

										break;
								
									default:
										string 	txt;
									
										if (tb.Text.Length > fi.size) 
										{
											MessageBox.Show(String.Format("Der Text im Feld {0} darf maximal {1} Zeichen lang sein.",
												ecoEnum.Key, fi.size));
											Trace.WriteLine(String.Format("Der Text in Feld {0} wurde auf {1} Zeichen gekürzt",
												ecoEnum.Key, fi.size));
											txt = tb.Text.Substring(0, fi.size);
											answ = false;
										} 
										else 
										{
											txt = tb.Text;
										}
									
										txt = txt.Replace("'","_");
										tb.Text = txt;
									
										ep.SetPropValue(ecoEnum.Key as string, txt);
									
										break;
								}
							} 
							else
								ep.SetPropValue(ecoEnum.Key as string, null);
							
						
						}

					}
					else if(eco.ctrl is CheckBox) 
					{
						cb = eco.ctrl as CheckBox;
						ep.SetPropValue(ecoEnum.Key as string, cb.Checked);
					} 
					else if(eco.ctrl is ComboBox) 
					{
						fi = ep.GetFieldInfo(ecoEnum.Key as string);
						if (fi.persist) 
						{
							comb = eco.ctrl as ComboBox;
							Epo	selEpo = comb.SelectedItem as Epo;
							Oid	selOid;
						
							if(selEpo != null) 
							{
								selOid = selEpo.oid;
								ep.SetPropValue(ecoEnum.Key as string, selOid);
							} 
							else
							{
								ep.SetPropValue(ecoEnum.Key as string, null);
							}
						}
					} 
					else if(eco.ctrl is DateTimePicker) 
					{
						dtp = eco.ctrl as DateTimePicker;
						fi = ep.GetFieldInfo(ecoEnum.Key as string);
						
						fi = ep.GetFieldInfo(ecoEnum.Key as string);
						if (fi.persist) 
						{

							switch (fi.csTypeName) 
							{
								case "DateTime":
									ep.SetPropValue(ecoEnum.Key as string, dtp.Value);
									break;
								case "TimeSpan":
									TimeSpan	ts;
									DateTime	myDt;
							
									myDt = dtp.Value;
									ts = new TimeSpan(myDt.Hour,
										myDt.Minute,
										0);
									ep.SetPropValue(ecoEnum.Key as String, ts);
									break;
							}
						}
					} 
				}
				
			}
			
			
			return answ;
		}
		
		
		//Der Anwender hat eine Klasse ausgewhaelt die er neu anlegen will
		protected virtual void EpoUserChoice(object sender, System.EventArgs e) 
		{
		
			EpoViewedLink	evl;
			MenuItem		mi;
			Epo				newEpo;
			
			
			mi = sender as MenuItem;
			evl = miTagList[mi.Text] as EpoViewedLink;
			
			Debug.Assert(mi != null);
			newEpo = Epo.NewByClassName(Epo.GetStdOrderKey(), evl.className);
			newEpo.SetPropValue(evl.foreignKeyName, ep.oid);
							
			
						
			if (newEpo!=null) 
			{
				newEpo.Flush();
				FillListReflective(lastLb, lastFieldName);
			}
		}
		
		
		protected virtual void GoBuClicked(object sender, System.EventArgs e) 
		{
			ListBox	lb;
			Button	bu;
			String	lbFieldname;
			
			bu = sender as Button;
			Debug.Assert(bu != null);
			
			lb = bu.Tag as ListBox;
			Debug.Assert(lb != null, "Listen-Edit-Button ohne ListBox-Tag gefunden");
			
			OpenSelectedEpo(lb);
			
			lbFieldname = lb.Tag as string;
			Debug.Assert(lbFieldname != null);
			FillListReflective(lb, lbFieldname);
		}
		
		protected virtual void DupBuClicked(object sender, System.EventArgs e) 
		{
			ListBox	lb;
			Button	bu;
			String	lbFieldname;
			Epo		toDup, newEpo;
			
			bu = sender as Button;
			Debug.Assert(bu != null);
						
			lb = bu.Tag as ListBox;
			Debug.Assert(lb != null, "Listen-Edit-Button ohne ListBox-Tag gefunden");
				
			
			toDup = lb.SelectedItem as Epo;
			
			if (toDup != null) 
			{
				newEpo = toDup.Clone() as Epo;
				if (newEpo != null) newEpo.Flush(); //Clone enthält kein Flush
			} 
			else 
			{
				MessageBox.Show("Um diese Funktion zu benutzen muss das zu duplizierendes Element in zugehörigen Liste selektiert sein");
			}
					
			lbFieldname = lb.Tag as string;
			Debug.Assert(lbFieldname != null);
			FillListReflective(lb, lbFieldname);
		}
		
		
		
		protected virtual void NewBuClicked(object sender, System.EventArgs e) 
		{
			string					fieldName;
			ListBox					lb;
			Button					bu;
			Hashtable				evlt;
			Epo       				newEpo;
			EpoViewedLink			evl;
			IDictionaryEnumerator	enu;

			
			bu = sender as Button;
			Debug.Assert(bu != null);
			lb = bu.Tag as ListBox;
			Debug.Assert(lb != null);
			fieldName = lb.Tag as string;
			Debug.Assert(fieldName != null);
			
			//Nun haben wir einen Feldnamen
			evlt = viewedLinks[fieldName] as Hashtable;	
			if(evlt==null) 
			{
				Trace.WriteLine("ListBox ohne defininierte ViewedLinks");
				return;
			}
				
			enu = evlt.GetEnumerator();
			if(enu==null) 
			{
				Trace.WriteLine("Leere ViewedLinks");
				return;
			}

			newEpo = null;
			
			//Mehr als eine Möglichkeit, dem Anwender die Auswahl überlassen
			if (evlt.Count > 1) 
			{
				ContextMenu		ctm = new ContextMenu();
				MenuItem    	mi;
					   		  
				//Diesen Scheiss muss ich nur machen weil MenuItems keinen Tag haben
				//und ich keine Lust hatte MenuItem zu beerben und den Tag zu ergänzen
				miTagList.Clear();
				while(enu.MoveNext()) 
				{
					evl = enu.Value as EpoViewedLink;
					newEpo = Epo.NewByClassName(Epo.GetStdOrderKey(), evl.className);
					mi = new MenuItem();
					miTagList[newEpo.DisplayName()] = evl;
					mi.Text = newEpo.DisplayName();
					mi.Click += new EventHandler(EpoUserChoice);
					
					ctm.MenuItems.Add(mi);
				}
				
				//Oh Gott Oh Gott hätt ich doch nen Tag am MenuItem
				lastLb = lb;
				lastFieldName = fieldName;
				
				ctm.Show(myWinForm, myWinForm.PointToClient(Form.MousePosition));
				
			} 
			else 
			{
				// Es war nur einer, also direkt anlegen
				enu.MoveNext();
				evl = enu.Value as EpoViewedLink;
				newEpo = Epo.NewByClassName(Epo.GetStdOrderKey(), evl.className);
				newEpo.SetPropValue(evl.foreignKeyName, ep.oid);
				
			}
			
			if (newEpo!=null) 
			{
				newEpo.Flush();
				FillListReflective(lb, fieldName);
			}
		}
		
		protected virtual void DelBuClicked(object sender, System.EventArgs e) 
		{
			Epo				ep;
			ListBox			lb;
			Button			clickedBU;
			DialogResult	res;
			String 			lbFieldname;
			
			clickedBU = sender as Button;
			Debug.Assert(clickedBU != null);
			lb = clickedBU.Tag as ListBox;
					
			Debug.Assert(lb != null, "Listen-Edit-Button ohne ListBox-Tag gefunden");
			
			ep = lb.SelectedItem as Epo;
						
			if(ep == null) return;
			
			res = MessageBox.Show(myWinForm,
				"Soll das Element wirklich gelöscht werden?",
				"Rückfrage",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button1,
				MessageBoxOptions.RightAlign);
						
			if (res!=DialogResult.Yes) return;
		
			ep.Delete();
			
			lbFieldname = lb.Tag as string;
			Debug.Assert(lbFieldname != null);
			
			FillListReflective(lb, lbFieldname);
		}
		
		

		protected virtual void OpenSelectedEpo(ListBox lb) 
		{
			EpoView 		epv;
			Epo				epo;
			ConstructorInfo constrInf;
			string			fullViewerName = null;
			Type[]			types;
			Type			viewerType;
			object[]		parms;
			bool			unlSucc;


			epo = lb.SelectedItem as Epo;

			if(epo==null) return;

			//Dies ergibt nicht die Classinfo von Epo sondern von der
			//Klasse deren Name in clName übergeben wurde!

			fullViewerName = viewKnowledge[epo.GetType().AssemblyQualifiedName] as String;

			if(fullViewerName == null) return;

			//Nun erst mal das aktuelle Epo flushen
			unlSucc = UnloadFromData();
			if(!unlSucc) return;

			ep.Flush();


			types = new Type[2];
			types[0] = epo.GetType();;
			types[1] = this.GetType();
			viewerType = Type.GetType(fullViewerName);
			constrInf = viewerType.GetConstructor(types);

			if (constrInf==null) 
			{
				throw new ApplicationException("Die Klasse <" 
					+ viewerType.FullName 
					+ "> hat keinen Konstruktor mit der Signatur (" 
					+ epo.GetType().Name 
					+ ", " 
					+ this.GetType().Name 
					+ ")" );
			}

			parms = new object[2];
			parms[0] = epo;
			parms[1] = this;
			epv = constrInf.Invoke(parms) as EpoView;

			epv.chainedRefresh = true;
			epv.Show();
		}


		public EpoView GetEpoViewForEpo(Epo epo, Form parent) 
		{
			ConstructorInfo constrInf;
			string			fullViewerName = null;
			Type[]			types;
			Type			viewerType;
			object[]		parms;
			EpoView         epv = null;

			fullViewerName = viewKnowledge[epo.GetType().AssemblyQualifiedName] as String;

			if(fullViewerName == null) return null;

			types = new Type[2];
			types[0] = epo.GetType();;
			types[1] = parent.GetType();
			viewerType = Type.GetType(fullViewerName);
			constrInf = viewerType.GetConstructor(types);

			parms = new object[2];
			parms[0] = epo;
			parms[1] = parent;
			epv = constrInf.Invoke(parms) as EpoView;

			return epv;
		}

		public void Show() 
		{
			myWinForm.Show();
		}


		public DialogResult ShowDialog() 
		{
			return myWinForm.ShowDialog();
		}



		protected virtual void ListDbl_Click(object sender, System.EventArgs e) 
		{
			ListBox	lb;
			string	fieldName;
			
			lb = sender as ListBox;
			OpenSelectedEpo(lb);
			
			fieldName = lb.Tag as string;
			FillListReflective(lb, fieldName);
		}
		
		
		protected virtual void OkB_Click(object sender, System.EventArgs e) 
		{
		         
			bool		  unlSucc;
		     
			unlSucc = UnloadFromData();
			if(!unlSucc) return;
		     
			ep.Flush();

			if (chainedRefresh) 
			{
				if (par is EpoView) 
				{
					EpoView epPar = par as EpoView;
					epPar.FillWithData();
				}
			}

			myWinForm.Dispose();
		}
   		
		//Event-Handler für Input-cursor verlässt das aktuelle Form-Objekt
		//Rettet die verwalteten Daten, damit Kindobjekte korrekt angezeigt
		//werden und bei deren Refreshsignalen (bei denen die Daten wieder aus der
		//DB gelesen werden) nichts verloren geht.
		protected virtual void ThisForm_Deactivate(object sender, System.EventArgs e) 
		{	
			//bool  unlSucc;
   			
			//Das hier geht auf keine Fall, weil dann der Abbrechen-Button komplett nutzlos wird
			//auch beim Click auf Abbrechen wird dieser Event natuerlich aktiv!!! MIST!!!
			//unlSucc = UnloadFromData();
			//ep.Flush();
			
			if (chainedRefresh) 
			{
				if (par is EpoView) 
				{
					EpoView epPar = par as EpoView;
					epPar.FillWithData();
				}
			}
		}

   		
		protected virtual void CancB_Click(object sender, System.EventArgs e) 
		{
			myWinForm.Dispose();
		}
   		
   		
		protected virtual void FillWithData() 
		{
			EpoControl				eco;
			IDictionaryEnumerator   ecoEnum;
			string					fieldName;
			object					propVal;
			
			Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeeignet für diese Operation");
			Debug.Assert(ep != null);
			
			ecoEnum = epoControls.GetEnumerator();
			
			while(ecoEnum.MoveNext()) 
			{
				
				eco = ecoEnum.Value as EpoControl;
				fieldName = ecoEnum.Key as String;
				
				if(eco.visible) 
				{	
					
					if(eco.ctrl is TextBox) 
					{
						TextBox tb = eco.ctrl as TextBox;
						string	s;
						
						propVal = ep.GetPropValue(fieldName);
						if(propVal is EpoMemo) 
						{
							EpoMemo em = propVal as EpoMemo;
							s = em.ToString();
						} 
						else if(propVal is Oid) 
						{
							s = (propVal as Oid).OidStr;
						} 
						else if (propVal is double) 
						{
							double	d = (double)propVal;
							s = d.ToString();
						} 
						else if (propVal is int) 
						{
							int	myint = (int)propVal;
							s = myint.ToString();
						} 
						else 
						{
							s = propVal as String;
						}
						
						tb.Text = s;
					} 
					else if (eco.ctrl is DateTimePicker) 
					{
						DateTimePicker	dtp = eco.ctrl as DateTimePicker;
						DateTime		dt;
						
						propVal = ep.GetPropValue(fieldName);
						if(propVal is DateTime) 
						{
							dt = (DateTime)propVal;
							dtp.Value = dt;
						} 
						else if (propVal is TimeSpan) 
						{
							TimeSpan ts;
							
							ts = (TimeSpan)propVal;
							dt = new DateTime(2004,1,1,ts.Hours,ts.Minutes,0);
							dtp.Value = dt;
						}
					
					} 
					else if (eco.ctrl is ComboBox) 
					{
						ComboBox	cob = eco.ctrl as ComboBox;
						
						FillComboReflective(cob, fieldName);
					} 
					else if (eco.ctrl is CheckBox) 
					{
						propVal = ep.GetPropValue(fieldName);
						CheckBox	cb = eco.ctrl as CheckBox;
						cb.Checked = (bool)propVal;
					} 
					else if (eco.ctrl is ListBox) 
					{
						ListBox	lb = eco.ctrl as ListBox;
						
						FillListReflective(lb, fieldName);
					}
				}
			}
		}
		
		
		protected Label NewLabel(String txt,
			int locx, int locy,
			int sizex, int sizey) 
		{
			Label   answ;
		
			answ = new Label();
			answ.Text = txt;
			answ.Location = new Point(locx, locy);
			answ.Size = new Size(sizex, sizey);
		
			return answ;
		}
		
		
		protected Button NewButton(String txt,
			int locx, int locy,
			int sizex, int sizey) 
		{
			Button   answ;
		
			answ = new Button();
			answ.Text = txt;
			answ.Location = new Point(locx, locy);
			answ.Size = new Size(sizex, sizey);
		
			return answ;
		}
		
		protected TextBox NewTextBox(int locx, int locy,
			int sizex, int sizey) 
		{
			TextBox   answ;
		
			answ = new TextBox();
			answ.Location = new Point(locx, locy);
			answ.Size = new Size(sizex, sizey);
		
			return answ;
		}
		   
		protected TextBox NewTextBox(int locx, int locy,
			int sizex, int sizey, 
			int maxchars) 
		{
			TextBox   answ;
		   
			answ = new TextBox();
			answ.Location = new Point(locx, locy);
			answ.Size = new Size(sizex, sizey);
			answ.MaxLength = maxchars;
		   
			return answ;
		}
		   
		   
		protected TextBox NewMultilineTextBox(int locx, int locy,
			int sizex, int sizey) 
		{
			TextBox   answ;
		   
			answ = NewTextBox(locx, locy, sizex, sizey);
			answ.Multiline = true;
			answ.WordWrap = true;
		   
			return answ;
		}
		   
		   
		protected TextBox NewMultilineTextBox(int locx, int locy,
			int sizex, int sizey,
			int maxchars) 
		{
			TextBox   answ;
		      
			answ = NewTextBox(locx, locy, sizex, sizey, maxchars);
			answ.Multiline = true;
			answ.WordWrap = true;
		      
			return answ;
		}
		
		
		protected ComboBox NewComboBox(int locx, int locy,
			int sizex, int sizey) 
		{
			ComboBox   answ;
		
			answ = new ComboBox();
			answ.Location = new Point(locx, locy);
			answ.Size = new Size(sizex, sizey);
		
			return answ;
		}
		
		protected CheckBox NewCheckBox(int locx, int locy) 
		{
			CheckBox   answ;
		
			answ = new CheckBox();
			answ.Location = new Point(locx, locy);
		
			return answ;
		}
		   
		   
		protected ListBox NewListBox(int locx, int locy,
			int sizex, int sizey) 
		{
			ListBox   answ;
		   
			answ = new ListBox();
			answ.Location = new Point(locx, locy);
			answ.Size = new Size(sizex, sizey);
		   
			return answ;
		}
		
		protected PictureBox NewPictureBox(int locx, int locy,
			int sizex, int sizey) 
		{
			PictureBox   answ;
		
			answ = new PictureBox();
			answ.Location = new Point(locx, locy);
			answ.Size = new Size(sizex, sizey);
		
			return answ;
		}
   			

		/// <summary>
		/// Holt die View-Einstellung zum Join auf einem Feld
		/// </summary>
		/// <param name="fName">Der Name des Feldes auf dem ein Jon liegen muss</param>
		/// <returns>Der gefunde JoindView oder null  wenn nichts gefunden wurde.</returns>
		private JoinViewSetting	GetJoinViewSetting(string fName) 
		{
			if (joinViewSettings == null) return null;

			return joinViewSettings[fName] as JoinViewSetting;
		}


		private ArrayList GetJoinRestriction(string fName) 
		{
			ArrayList				answ = new ArrayList();
			EpoClassInfo			eci;
			EpoPick					epick;
			EpoPickInfo				eppinf;
			IDictionaryEnumerator	pfEnu;
			string					nativeF;

			//Einzelrestriktion hinzufügen
			if (restrictedJoins != null) 
			{
				if (restrictedJoins[fName] != null)
					answ.Add(restrictedJoins[fName]);
			}

			//Contrained JoinPicks als Restiktionen auslegen
			eci = Epo.ClassInfo(ep);
			if(eci.picksMap.Count>0) 
			{
				if(eci.picksMap.Contains(fName)) {
					epick = eci.picksMap[fName] as EpoPick;
					
					pfEnu = epick.pickfields.GetEnumerator();
					while(pfEnu.MoveNext()) 
					{
						nativeF = pfEnu.Key as string;
						eppinf = pfEnu.Value as EpoPickInfo;
						if (eppinf.constrain) 
						{
							answ.Add(new EpoJoinRestrict(eppinf.foreignField, nativeF));
						}
					}
				}
			}
			

			return answ;
		}

   			
		protected void FillComboReflective(ComboBox cb, 
			string fieldName) 
		{
			   
			ArrayList 		erg;
			Epo       		verwo;
			EpoJoin			joins;
			Epo				mep;
			Oid				joid;
			ArrayList		jrests;
			string			wc;
			JoinViewSetting	jvset;
			bool			first;
				
			joid = ep.GetPropValue(fieldName) as Oid;
			
			joins = ep.GetJoins(fieldName);
			jrests = GetJoinRestriction(fieldName);
			jvset = GetJoinViewSetting(fieldName);
				
			Debug.Assert(joins != null, "Joins für das Feld " + fieldName + " konnten nicht gefunden werden");
				
			cb.BeginUpdate();
			for(int k=0; k<joins.foreignClassNames.Count; k++) 
			{
				verwo = Epo.NewByClassName(Epo.GetStdOrderKey(), joins.foreignClassNames[k] as string);
				if (jrests.Count==0) 
				{
					erg = verwo.Select();
				} 
				else 
				{
					wc = "";
					first = true;
					foreach(EpoJoinRestrict jr in jrests) 
					{
						if(ep.GetPropValue(jr.nativeFieldName)!=null) 
						{
							if(!first) 
							{
								wc += " AND ";
							}

							wc += "[" + jr.foreignFieldName + "]='" + ep.GetPropValue(jr.nativeFieldName) + "'";
							first = false;
						}
					}

					if(wc.Length>0)
						erg = verwo.Select(wc);
					else
						erg = verwo.Select();
				}

				for(int i=0; i<erg.Count; i++) 
				{
					mep = erg[i] as Epo;
					cb.Items.Add(mep);
					if(joid!=null) 
					{
						if(joid.Equals(mep.oid)) 
						{
							cb.SelectedItem = erg[i];
						}
					}
				}

				if (jvset!=null) 
				{
					Debug.Assert(jvset.targetProperty!=null);
					Debug.Assert(jvset.targetProperty.Length > 0);
					cb.DisplayMember = jvset.targetProperty;
				}
					 
			}
				
			cb.EndUpdate();
			
		}
			
			
		/// <summary>
		/// Die ListBox lb die für da Feld in fieldname dargestellt werden soll
		/// mit den Daten aus dem Epo fuellen, dass an diese Instanz des
		/// Viewers gebunden ist.
		/// </summary>
		/// <param name="lb">Die zu fuellende ListBox</param>
		/// <param name="fieldName">Der Feldname für den die ListBox steht.</param>
		protected void FillListReflective(ListBox lb, 
			string fieldName) 
		{
						   
			ArrayList 				erg;
			Epo       				verwo;
			Epo						mep;
			Oid						dataOid;
			EpoViewedLink			evl;
			Hashtable				evlt;
			IDictionaryEnumerator	enu;
			string					whereClause;
							
							
			dataOid = ep.oid;
			evlt = viewedLinks[fieldName] as Hashtable;
				
			if(evlt==null) 
			{
				Trace.WriteLine("ListBox ohne defininierte ViewedLinks");
				return;
			}
				
			enu = evlt.GetEnumerator();
			if(enu==null) 
			{
				Trace.WriteLine("Leere ViewedLinks");
				return;
			}
				
			lb.BeginUpdate();
			lb.Items.Clear();
			while(enu.MoveNext()) 
			{
				evl = enu.Value as EpoViewedLink;
				verwo = Epo.NewByClassName(Epo.GetStdOrderKey(), evl.className);
				whereClause = "[" + evl.foreignKeyName + "]='" + dataOid + "'";
				if(evl.additionalWhere!=null && evl.additionalWhere.Length > 0)
					whereClause = "(" + whereClause + ") AND ("+ evl.additionalWhere + ")";
						
				erg = verwo.Select(whereClause);
				if(erg!=null) 
				{
					for(int i=0; i<erg.Count; i++) 
					{
						mep = erg[i] as Epo;
						lb.Items.Add(mep);
					}

					if (evl.displayMethName!=null)
						lb.DisplayMember = evl.displayMethName;
				}
			}			
			lb.EndUpdate();
						
		}

		private void myWinForm_Load(object sender, EventArgs e)
		{
			doFocus = true;
		}

		
		private void myWinForm_Activated(object sender, EventArgs e)
		{
			IDictionaryEnumerator	ecoEnum;
			EpoControl				econ;
			Control					actControl = null;

			if (!doFocus) return;

			//Den Focus auf dasjenige Control setzten, dass keine ComboBox ist weil
			//diese sonst leicht versehentlich mit dem Mausrad verändert werden kann.

			ecoEnum = epoControls.GetEnumerator();
			while(ecoEnum.MoveNext()) 
			{
				econ = ecoEnum.Value as EpoControl;
				if(econ.ctrl is TextBox && econ.ctrl.CanFocus) 
				{
					if(actControl!=null) 
					{
						if(econ.ctrl.TabIndex<actControl.TabIndex) 
						{
							actControl = econ.ctrl;
						}
					} 
					else 
					{
						actControl = econ.ctrl;
					}
				}
			}


			if(actControl!=null)
				actControl.Focus();
		
			
			doFocus = false;
		}
	}
}
