using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PNEventEngine
{
	class Program
	{
		static void Main(string[] args)
		{
			var effectDispatcher = new EffectDispatcher();

			var client = new HttpClient();		// Networking utility

			var eventEmitter = new EventEmitter();

			var handshakeEffect = new HandshakeEffectHandler(client, eventEmitter);

			var receivingEffect = new ReceivingEffectHandler(client, eventEmitter);

			var reconnectionEffect = new ReconnectingEffectHandler( eventEmitter);

			effectDispatcher.Register(EffectType.SendHandshakeRequest, handshakeEffect);

			effectDispatcher.Register(EffectType.ReceiveEventRequest, receivingEffect);

			effectDispatcher.Register(EffectType.ReconnectionAttempt, reconnectionEffect);

			var engine = new EventEngine(effectDispatcher, eventEmitter);

			var initState = engine.CreateState(StateType.Unsubscribed)
				.OnEntry(() => { Console.WriteLine("Unsubscribed: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("Unsubscribed: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Handshaking);

			engine.CreateState(StateType.Handshaking)
				.OnEntry(() => { Console.WriteLine("Handshaking: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("Handshaking: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Handshaking)
				.On(EventType.HandshakeSuccess, StateType.Receiving)
				.On(EventType.HandshakeFailed, StateType.Reconnecting)
				.Effect(EffectType.SendHandshakeRequest);

			engine.CreateState(StateType.Receiving)
				.OnEntry(() => { Console.WriteLine("Receiving: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("Receiving: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Handshaking)
				.On(EventType.ReceiveSuccess, StateType.Receiving)
				.On(EventType.ReceiveFailed, StateType.Reconnecting)
				.Effect(EffectType.ReceiveEventRequest);

			engine.CreateState(StateType.HandshakingFailed)
				.OnEntry(() => { Console.WriteLine("HandshakingFailed: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("HandshakingFailed: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Handshaking)
				.On(EventType.HandshakeSuccess, StateType.Receiving)
				.On(EventType.HandshakeFailed, StateType.Reconnecting);

			engine.CreateState(StateType.Reconnecting)
				.OnEntry(() => { Console.WriteLine("Reconnecting: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("Reconnecting: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Handshaking)
				.On(EventType.HandshakeSuccess, StateType.Receiving)
				.On(EventType.HandshakeFailed, StateType.Reconnecting);

			engine.InitialState(initState);

			engine.Subscribe(new List<string> { "ch1", "ch2"}, null);

			Console.WriteLine("In Program!");
			Console.ReadLine();
		}
	}
}
