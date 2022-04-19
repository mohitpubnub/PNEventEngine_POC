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

			var delayUtil = new DelayUtil(5, ReconnectionPolicy.Exponential, 5000);

			var reconnectionEffectHandler = new ReconnectingEffectHandler(client, eventEmitter, delayUtil);

			var handshakeReconnectionEffectHandler = new HandshakeReconnectingEffectHandler(client, eventEmitter, delayUtil);

			effectDispatcher.Register(EffectType.SendHandshakeRequest, handshakeEffect);

			effectDispatcher.Register(EffectType.ReceiveEventRequest, receivingEffect);

			effectDispatcher.Register(EffectType.ReceiveReconnection, reconnectionEffectHandler);

			effectDispatcher.Register(EffectType.HandshakeReconnection, handshakeReconnectionEffectHandler);

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
				.On(EventType.HandshakeFailed, StateType.HandshakeReconnecting)
				.Effect(EffectType.SendHandshakeRequest);

			engine.CreateState(StateType.Receiving)
				.OnEntry(() => { Console.WriteLine("Receiving: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("Receiving: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Receiving)
				.On(EventType.ReceiveSuccess, StateType.Receiving)
				.On(EventType.ReceiveFailed, StateType.Reconnecting)
				.Effect(EffectType.ReceiveEventRequest);

			engine.CreateState(StateType.Reconnecting)
				.OnEntry(() => { Console.WriteLine("Reconnecting: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("Reconnecting: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Receiving)
				.On(EventType.ReconnectionFailed, StateType.Reconnecting)
				.On(EventType.ReceiveSuccess, StateType.Receiving)
				.On(EventType.Giveup, StateType.ReconnectingFailed)
				.Effect(EffectType.ReceiveReconnection);

			engine.CreateState(StateType.HandshakeReconnecting)
				.OnEntry(() => { Console.WriteLine("HandshakeReconnecting: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("HandshakeReconnecting: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Handshaking)
				.On(EventType.HandshakeReconnectionFailed, StateType.HandshakeReconnecting)
				.On(EventType.HandshakeSuccess, StateType.Receiving)
				.On(EventType.Giveup, StateType.HandshakingFailed)
				.Effect(EffectType.HandshakeReconnection);

			engine.CreateState(StateType.HandshakingFailed)
				.OnEntry(() => { Console.WriteLine("HandshakingFailed: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("HandshakingFailed: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Handshaking)			
				.On(EventType.Restore, StateType.HandshakeReconnecting);

			engine.CreateState(StateType.ReconnectingFailed)
				.OnEntry(() => { Console.WriteLine("ReconnectingFailed: OnEntry()"); return true; })
				.OnExit(() => { Console.WriteLine("ReconnectingFailed: OnExit()"); return true; })
				.On(EventType.SubscriptionChange, StateType.Receiving)
				.On(EventType.Restore, StateType.Reconnecting);

			engine.InitialState(initState);

			engine.Subscribe(new List<string> { "ch1", "ch2"}, null);

			Console.WriteLine("In Program!");
			Console.ReadLine();
		}
	}
}
