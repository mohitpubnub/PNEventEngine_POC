﻿using System;

namespace PNEventEngine
{
	public class ReconnectingEffectHandler : IEffectHandler
	{
		EventEmitter eventEmitter;
		public ReconnectingEffectHandler(EventEmitter emitter)
		{
			this.eventEmitter = emitter;
		}

		public void Start(ExtendedState context)    // TODO: Implementation of retry  getDelay() as per policy
		{
			var evnt = new Event();
			evnt.EventPayload.Timetoken = context.Timetoken;
			evnt.EventPayload.Region = context.Region;
			evnt.Type = EventType.ReceiveSuccess;
			this.eventEmitter.emit(evnt);
		}

		public void Cancel()
		{
			Console.WriteLine("Reconnecting Cancelled!!!");
		}
	}
}
