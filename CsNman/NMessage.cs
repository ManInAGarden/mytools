namespace HSp.CsNman {
	public class NMessage {
		string		title;
		object		sender;
		string		senderName;
		object		msgData;
		
		
		
		public NMessage(object sender,
						string title,
						object msgData) {
						
			this.senderName = sender.ToString();
			this.title = title;
			this.sender = sender;
			this.msgData = msgData;
		}
		
		
		public NMessage(object sender,
						string senderName,
						string title,
						object msgData) {
						
			this.senderName = senderName;
			this.sender = sender;
			this.title = title;
			this.msgData = msgData;
		}
		
		
		
		public object Sender {
			get {return sender;}
			set {sender = value;}
		}
		
		
		public string SenderName {
			get {return senderName;}
			set {senderName = value;}
		}
		
		
		public string Title {
			get { return title;}
			set {title = value;}
		}
		
		
		public object MsgData {
			get {return msgData;}
			set {msgData = value;}
		}
		
		
		public override string ToString() {
			return senderName + "[" + msgData.ToString() + "]";
		}
	}
}