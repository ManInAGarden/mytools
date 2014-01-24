namespace HSp.CsNman {
	class Subscriber {
		object			subscriberObj;
		NDelegate		del;
		
		
		public Subscriber(object subscriberObj, NDelegate del) {
			this.subscriberObj = subscriberObj;
			this.del = del;
		}
		
		public void ExecDelegate(NMessage  msg) {
			del(msg);
		}
		
		public object SubscriberObj {
			get {return subscriberObj;}
		}
	}
}