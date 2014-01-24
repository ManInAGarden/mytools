namespace HSp.CsEpo {

   using System;

   public class Posinfo {
      public int x,y,b,h;

      public Posinfo(int x, int y, int b, int h) {
             this.x = x;
             this.y = y;
             this.b = b;
             this.h = h;
      }
   }


   public class EpoVisInfo {
      public static int JEPO_VISITYP_ATTRIBUT = 0,
                        JEPO_VISITYP_NSIDE = 1;
      private String    name, vortext, controlClName;
      private int       x, y, b, h,
                        type;
      private String    lasche;
      private bool      visible;


      #region constructors
      public EpoVisInfo() {
         BasicInitialize();
      }

      public EpoVisInfo(int x, int y, int b, int h) {
         BasicInitialize();
         this.x = x;
         this.y = y;
         this.h = h;
         this.b = b;
      }

      #endregion

      #region properties
      public String Name {
         get { return name;}
         set { name = value;}
      }

      public String Vortext {
         get { return vortext;}
         set { vortext = value;}
      }

      public String ControlClName {
         get {return controlClName;}
         set {controlClName = value;}
      }

      public bool Visible {
        set {visible = value;}
        get {return visible;}
      }


      public String Lasche {
        set {lasche = value;}
        get {return lasche;}
      }

      public int Type {
        set {type = value;}
        get {return type;}
      }

      public Posinfo PosAndSize {
             set {
                 Posinfo po = value as Posinfo;
                 x = po.x;
                 y = po.y;
                 b = po.b;
                 h = po.h;
             }
             get {
                Posinfo po = new Posinfo(x,y,b,h);
                return po;
              }
      }
      #endregion



      private void BasicInitialize() {
         type = JEPO_VISITYP_ATTRIBUT;
         visible = true;
         // Hier folgen Adressen für ein GridBagLayout...
         x = 1;
         y = 1;
         b = 1;
         h = 1;
      }
   }
}