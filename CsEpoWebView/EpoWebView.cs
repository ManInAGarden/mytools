using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;


using HSp.CsEpo;

namespace HSp.CsEpoWebView
{
    public class EpoWebView : EpoBaseView
    {
        
        Dictionary<string,EpoWebControl> m_epoWebControls;
        PlaceHolder m_ph;

        public EpoWebView() : base() 
		{
		}

        public EpoWebView(Epo viewedEpo, PlaceHolder ph)
            : base(viewedEpo, null)
        {
            ep = viewedEpo;
            m_ph = ph;

            m_epoWebControls = new Dictionary<string,EpoWebControl>();
            InitSpecialControls(ph);
            GenerateTabIndex();
            InitializeComponents(ph);
        }

       

        public void CreateGUI()
        {
           
        }


        //Sollte bei Sonderwünschen überladen werden
        //Ansonsten wird hier in der vorgefundenen Reihenfolge
        //je ein Label und ein Control definiert.
        protected virtual void InitSpecialControls(PlaceHolder ph)
        {
            EpoClassInfo eci;
            IDictionaryEnumerator fiEnum;
            FieldInfo fi;
            int aktCol = 0,
                aktLine = 0;
            EpoWebControl econ;
            string currKey;

            Debug.Assert(ph != null);
            Debug.Assert(ep != null);
            Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeeignet für diese Operation");

            eci = Epo.ClassInfo(ep);
            fiEnum = eci.fieldMap.GetEnumerator();


            fiEnum.Reset();
            while (fiEnum.MoveNext())
            {
                currKey = fiEnum.Key as String;
                if (currKey != "oid")
                {
                    fi = fiEnum.Value as FieldInfo;

                    if (Epo.tsw.TraceVerbose)
                    {
                        Trace.WriteLine("Controleintrag für " + currKey + " wird erzeugt");
                    }

                    econ = new EpoWebControl(currKey,
                        aktCol,
                        aktLine,
                        1,
                        1);

                    switch (fi.csTypeName)
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
                            econ.ctrl = new TextBox();
                            break;
                        case "TimeSpan":
                            TextBox mydtp;
                            mydtp = new TextBox();
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
                            mtb.Wrap = true;
                            mtb.TextMode = TextBoxMode.MultiLine;
                            econ.ctrl = mtb;
                            break;
                        case "Oid":
                            //Wenn zu dieser Oid Joins definiert wurden eine ComboBox
                            //sonst ein normales Textfeld anlegen
                            if (ep.GetJoins(currKey) != null)
                                econ.ctrl = new DropDownList();
                            else
                                econ.ctrl = new TextBox();
                            break;
                        default:
                            String errstr = String.Format("Unbekannte Typ: <{0}. Kann Web Control für View nicht ermitteln>", fi.csTypeName);
                            Trace.WriteLine(errstr);
                            throw new Exception(errstr);
                    }


                    econ.enabled = fi.persist;
                    econ.ctrl.ID = currKey;

                    //ph.Controls.Add(econ.ctrl);

                    m_epoWebControls.Add(currKey, econ);

                    aktLine += econ.lineSpan;
                }
            }


        }


        //Die EpoCtrl Number anhand der Positionen setzen. Diese wirkt sich
        //später auf den TabIndex jedes einzelnen Controls aus
        protected void GenerateTabIndex()
        {
           
            ArrayList sortableEcos;
            int ct;

            sortableEcos = new ArrayList(m_epoWebControls.Count);

            foreach (EpoWebControl ewc in m_epoWebControls.Values)
            {
                sortableEcos.Add(ewc);
            }
            
            sortableEcos.Sort();

            //Nun ist alles nach Positionen sortiert
            ct = 1;
            foreach (EpoWebControl myEc in sortableEcos)
            {
                myEc.number = ct;
                ct++;
            }

        }


        //Aus den EpoControls den Dialog aufbauen
        private void InitializeComponents(PlaceHolder ph)
        {
            EpoWebControl ecw;
            Label lab;
            IDictionaryEnumerator ecoEnum;
            int lowestRow,
                leftestCol,
                rightestCol,
                highestRow,
                buttonLine;
            int labWidth;
            //Font frmFont;
            int buttonTabIdx = 1000;

            Debug.Assert(!isVO, "Verwaltungs-View-Objekt ungeeignet für diese Operation");
            components = new System.ComponentModel.Container();

            GetTableRange(out leftestCol, out rightestCol, out highestRow, out lowestRow);
            Table theTable = CreateTable(leftestCol, rightestCol, highestRow, lowestRow);

            theTable.ApplyStyleSheetSkin(ph.Page);

            foreach (string key in m_epoWebControls.Keys)
            {
                ecw = m_epoWebControls[key];
                if (ecw.visible)
                {
                    //Zuerst das Label
                    if ((ecw.label != null) && (ecw.label.Length > 0) && !(ecw.ctrl is Button))
                    {
                        lab = NewLabel(key, ecw.label);
                        theTable.Rows[ecw.labLine].Cells[ecw.labColumn].Controls.Add(lab);
                        theTable.Rows[ecw.labLine].Cells[ecw.labColumn].ColumnSpan = ecw.colSpan;

                        //if (debugView) lab.ToolTip="LAB:" + key;


                    }

                    //Nun das Control
                    if (ecw.ctrl is DataGrid)
                    {
                    }
                    else if (ecw.ctrl is TextBox)
                    {
                        TextBox tb = ecw.ctrl as TextBox;
                        tb.TabIndex = (short)ecw.number;
                        tb.Enabled = ecw.enabled;
                    }

                    theTable.Rows[ecw.line].Cells[ecw.column].Controls.Add(ecw.ctrl);
                    theTable.Rows[ecw.line].Cells[ecw.column].ColumnSpan = ecw.colSpan;
                }

                ph.Controls.Add(theTable);
            }

            //ecoEnum = epoControls.GetEnumerator();
            //ecoEnum.Reset();
            //while (ecoEnum.MoveNext())
            //{
            //    eco = ecoEnum.Value as EpoControl;

            //    Debug.Assert(eco != null);

            //    if (eco.visible)
            //    {
            //        //Zuerst das Label
            //        if ((eco.label != null) && (eco.label.Length > 0) && !(eco.ctrl is Button))
            //        {
            //            if (eco.labLine == eco.line)
            //                labWidth = (eco.column - eco.labColumn) * colWidth;
            //            else
            //                labWidth = eco.label.Length * colWidth;

            //            lab = NewLabel(eco.label,
            //                eco.labColumn * colWidth,
            //                eco.labLine * lineHeight + (int)(lineHeight * 0.1),
            //                labWidth,
            //                eco.lineSpan * lineHeight - 5);


            //            if (debugView) tt.SetToolTip(lab, "LAB:" + (ecoEnum.Key as String));

            //            myWinForm.Controls.Add(lab);
            //        }


            //        //Nun das Control
            //        eco.ctrl.Location = new Point(eco.column * colWidth, eco.line * lineHeight);
            //        eco.ctrl.Size = new Size(eco.colSpan * colWidth, eco.lineSpan * lineHeight - lineSpacing);
            //        eco.ctrl.Enabled = eco.enabled;
            //        eco.ctrl.TabIndex = eco.number;

            //        if (eco.ctrl is ListBox)
            //        {
            //            (eco.ctrl as ListBox).DoubleClick += new EventHandler(ListDbl_Click);
            //            if (eco.edilink)
            //            {
            //                Button goBU, delBU, dupBU, newBU;

            //                goBU = NewButton(">",
            //                    eco.buCol * colWidth,
            //                    eco.buLine * lineHeight + (int)(0.1 * lineHeight),
            //                    (int)(1.8 * colWidth),
            //                    (int)(0.8 * lineHeight));
            //                goBU.Tag = eco.ctrl;
            //                goBU.Click += new EventHandler(GoBuClicked);
            //                goBU.TabIndex = buttonTabIdx++;
            //                myWinForm.Controls.Add(goBU);

            //                newBU = NewButton(Epo.GetStdOrderKey(),
            //                    (eco.buCol + 2) * colWidth,
            //                    eco.buLine * lineHeight + (int)(0.1 * lineHeight),
            //                    (int)(1.8 * colWidth),
            //                    (int)(0.8 * lineHeight));
            //                newBU.Tag = eco.ctrl;
            //                newBU.Click += new EventHandler(NewBuClicked);
            //                newBU.TabIndex = buttonTabIdx++;
            //                myWinForm.Controls.Add(newBU);

            //                dupBU = NewButton("2",
            //                    (eco.buCol + 4) * colWidth,
            //                    eco.buLine * lineHeight + (int)(0.1 * lineHeight),
            //                    (int)(1.8 * colWidth),
            //                    (int)(0.8 * lineHeight));
            //                dupBU.Tag = eco.ctrl;
            //                dupBU.Click += new EventHandler(DupBuClicked);
            //                dupBU.TabIndex = buttonTabIdx++;
            //                myWinForm.Controls.Add(dupBU);

            //                delBU = NewButton("X",
            //                    (eco.buCol + 6) * colWidth,
            //                    eco.buLine * lineHeight + (int)(0.1 * lineHeight),
            //                    (int)(1.8 * colWidth),
            //                    (int)(0.8 * lineHeight));
            //                delBU.Tag = eco.ctrl;
            //                delBU.Click += new EventHandler(DelBuClicked);
            //                delBU.TabIndex = buttonTabIdx++;
            //                myWinForm.Controls.Add(delBU);
            //            }
            //        }
            //        else if (eco.ctrl is Button)
            //        {
            //            Button bu = eco.ctrl as Button;

            //            bu.Text = eco.label;
            //        }

            //        if (debugView) tt.SetToolTip(eco.ctrl, ecoEnum.Key as string);

            //        myWinForm.Controls.Add(eco.ctrl);

            //        if (eco.labLine > lowestLine) lowestLine = eco.labLine;
            //        if ((eco.line + eco.lineSpan) > lowestLine) lowestLine = eco.line + eco.lineSpan - 1;
            //        if (eco.labColumn < leftestCol) leftestCol = eco.labColumn;
            //        if (eco.column < leftestCol) leftestCol = eco.column;
            //        if ((eco.column + eco.colSpan) > rightestCol) rightestCol = eco.column + eco.colSpan;
            //    }
            //}


            ////Nun die Buttons anhängen

            //buttonLine = lowestLine + 2;
            //Button okB = NewButton("OK",
            //    leftestCol * colWidth,
            //    buttonLine * lineHeight,
            //    80,
            //    lineHeight);

            //okB.TabIndex = buttonTabIdx++;
            //okB.Click += new EventHandler(OkB_Click);

            //Button cancB = NewButton("Abbrechen",
            //    leftestCol * colWidth + 90,
            //    buttonLine * lineHeight,
            //    80,
            //    lineHeight);

            //cancB.TabIndex = buttonTabIdx++;
            //cancB.Click += new EventHandler(CancB_Click);


            //myWinForm.Controls.Add(okB);
            //myWinForm.Controls.Add(cancB);

            //myWinForm.Deactivate += new EventHandler(ThisForm_Deactivate);

            //myWinForm.Text = DialogTitle;
            //myWinForm.Size = new Size((rightestCol + 2) * colWidth, (buttonLine + 2) * lineHeight + 30);

            //myWinForm.AcceptButton = okB;
            //myWinForm.CancelButton = cancB;

            //myWinForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            //myWinForm.MaximizeBox = false;
            //myWinForm.MinimizeBox = false;

            //myWinForm.StartPosition = FormStartPosition.Manual;
            //int centerX = myWinForm.Owner.DesktopLocation.X + (int)((myWinForm.Owner.Size.Width - myWinForm.Size.Width) / 2);
            //int centerY = myWinForm.Owner.DesktopLocation.Y + (int)((myWinForm.Owner.Size.Height - myWinForm.Size.Height) / 2);

            //if (centerX < 0) centerX = 0;
            //if (centerY < 0) centerY = 0;

            //myWinForm.Location = new Point(centerX, centerY);

            //FillWithData();
        }

        private Label NewLabel(string key, string text)
        {
            Label l = new Label();
            l.Text = text;
            l.ID = "L_" + key;

            return l;
        }

        private Table CreateTable(int leftestCol, int rightestCol, int highestRow, int lowestRow)
        {
            Table answ = new Table();
            TableRow tr;
            TableCell tc;

            for (int r = 0; r <= lowestRow; r++)
            {
                tr = new TableRow();
                for (int c = 0; c <= rightestCol; c++)
                {
                    tc = new TableCell();
                    tr.Cells.Add(tc);
                }
                answ.Rows.Add(tr);
            }

            answ.ID = "THE_TABLE";
            answ.SkinID = "DetailDataView";
            answ.EnableTheming = true;

            return answ;
        }

        /// <summary>
        /// Calculates the ranges for the tab by stepping through all the labels and controls
        /// </summary>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <param name="?"></param>
        protected void GetTableRange(out int leftestCol, out int rightestCol, out int highestRow, out int lowestRow)
        {
            leftestCol = Int32.MaxValue;
            rightestCol = Int32.MinValue;
            lowestRow = Int32.MinValue;
            highestRow = Int32.MaxValue;

            foreach (EpoWebControl ewc in m_epoWebControls.Values)
            {
                //check da labels
                if (ewc.labColumn < leftestCol)
                    leftestCol = ewc.labColumn;
                if (ewc.labColumn > rightestCol)
                    rightestCol = ewc.labColumn;
                if (ewc.labLine > lowestRow)
                    lowestRow = ewc.labLine;
                if (ewc.labLine < highestRow)
                    highestRow = ewc.labLine;

                //check da controls
                if (ewc.column < leftestCol)
                    leftestCol = ewc.column;
                if (ewc.column > rightestCol)
                    rightestCol = ewc.column + ewc.colSpan;
                if (ewc.line > lowestRow)
                    lowestRow = ewc.line + ewc.lineSpan;
                if (ewc.lineSpan < highestRow)
                    highestRow = ewc.lineSpan;
            }
        }
    }
}
