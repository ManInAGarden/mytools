using System;
using System.Runtime.Serialization;

namespace HSp.CsEpo {

   ///<summary>
   ///Verwaltung von OIDs für EPO.
   ///</summary>
    [Serializable]
    public class Oid 
    {
      private static Random   ranGen;
      private Object          objRep;
      private String          stringRep;

      #region properties
      public String OidStr {
         get {
            return stringRep;
         }
         set {
            stringRep = value;
         }
      }

      public Object Obj {
         get {
            return objRep;
         }
         set {
            objRep = value;
         }
      }

      public static int DbSize {
         get {
            return 50;
         }
      }

      public String ClassName {
         get {
            int posOfUnderscore;

            posOfUnderscore = stringRep.IndexOf('_');
            return stringRep.Substring(0, posOfUnderscore);
         }
     }

     #endregion

     ///Constructor
     public Oid(Object o) {
        Type       	t = o.GetType();
        String    	clNamePart = t.Name;
        String    	timePart;
        DateTime	tim = DateTime.Now;
        int        	myRanNumber,
        			myRanNumber2;


        if (clNamePart.Length>(Oid.DbSize-29))
           throw new Exception(String.Format("Class name {0} is to long for CSEpo", t.Name));
        if(ranGen == null)
           ranGen = new Random();

        timePart = tim.ToString("yyyyMMdd_hhmmss");
        myRanNumber = ranGen.Next(32000);
        myRanNumber2 = ranGen.Next(32000);
        stringRep = String.Format("{0}_{1}_{2}_{3}",
                                  clNamePart,
                                  timePart,
                                  myRanNumber,
                                  myRanNumber2);

        objRep = null;
     }

     ///<summary>
     /// Create a new Oid with the given OID-String
     ///</summary>
     public Oid(String str) {
        if(str!=null)
           stringRep = str;
     }

     ///<summary>
     ///Return the String Representation, which is the OID as a String
     ///</summary>
     public override String ToString() {
        return stringRep;
     }

     ///<summary>
     ///Two OIDs are equal when there String represantations are equal
     public override bool Equals(Object testOid) {
        Oid oid;

        if(testOid==null) return false;
        
        oid = testOid as Oid;

        //Dies hier kann bei Benutzung mit WPF passieren
        if (oid == null)
            return false;
        
        return stringRep.Equals(oid.OidStr);
     }

     public static new bool Equals(Object o1, Object o2) {
        Oid   oid1, oid2;

        oid1 = o1 as Oid;
        oid2 = o2 as Oid;
        return oid1.Equals(oid2);
     }

     ///<summary>
     ///Returns a HashCode for the OId. This is the HashCode of its
     /// string represantation
     ///</summary>
     public override int GetHashCode() {
        return stringRep.GetHashCode();
     }

     public static bool IsValidOid(String str) {
        throw new Exception("EpoTODO: Oid::IsValidOid muss realisert werden");
     }



    
   }

}