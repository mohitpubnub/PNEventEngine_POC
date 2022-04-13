using System;

namespace PNEventEngine
{
	public class EventEmitter
	{
		public Action<Event> handler;

		public void RegisterHandler(Action<Event> eventHandler)
		{
			this.handler = eventHandler;
		}

		public void emit(Event e)
		{
			this.handler(e);
		}
	}
}
