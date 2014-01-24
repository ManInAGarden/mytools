namespace HSp.CsEpo {

  using System;
  using System.Diagnostics;

  public class FieldInfo {
	public Type		csType;
	public String	dbTypeName, csTypeName;
	public int		size, 
					precision;
	public int  	backInfo;
    public bool		persist;


	public FieldInfo(Type csType) {
	  this.csType = csType;
	  csTypeName = csType.Name;
	}

	public void Complete() {
	  switch(csType.Name) {
	  case "String":
		dbTypeName = "VARCHAR";
		if (size == 0) size = 50;
		break;
	  case "int":
		dbTypeName = "INTEGER";
		break;
	  case "Int32":
		dbTypeName = "INTEGER";
		break;
	  case "DateTime":
		if (Epo.dbMode==EnumDbMode.MsAccess)
			dbTypeName = "TIMESTAMP";
		else 
			dbTypeName = "DATE";
		break;
	  case "TimeSpan":
	  	dbTypeName = "VARCHAR";
	  	size = 20;
		break;
	  case "Boolean":
		dbTypeName = "INTEGER";
		break;
	  case "Float":
		dbTypeName = "FLOAT";
		break;
	  case "Double":
		dbTypeName = "DOUBLE";
		break;
	  case "EpoMemo":
		dbTypeName = "LONGTEXT";
		break;
	  case "Oid":
		dbTypeName = "VARCHAR";
		size = Oid.DbSize;
		break;
	  case "FileStream":
		  if(Epo.dbMode==EnumDbMode.Oracle)
			  dbTypeName = "BFILE";
		  else 
		  {
			  String nerrstr = String.Format("Unbekannte Typ: <{0}. Kann persitenten Type nicht bestimmen>", csType.Name);
			  Trace.WriteLine(nerrstr);
			  throw new Exception(nerrstr);
		  }

		  break;
	  case "Decimal":
		  if(Epo.dbMode==EnumDbMode.Oracle) 
		  {
			  dbTypeName = "NUMBER";
			  size = 9;
			  precision = 2;
		  } 
		  else 
		  {
			  String nerrstr = String.Format("Unbekannte Typ: <{0}. Kann persitenten Type nicht bestimmen>", csType.Name);
			  Trace.WriteLine(nerrstr);
			  throw new Exception(nerrstr);
		  }
		  break;

	  default:
		String errstr = String.Format("Unbekannte Typ: <{0}. Kann persitenten Type nicht bestimmen>", csType.Name);
		Trace.WriteLine(errstr);
		throw new Exception(errstr);
	  }
	}
  }
}
