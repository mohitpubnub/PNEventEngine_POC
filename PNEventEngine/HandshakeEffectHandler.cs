using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace PNEventEngine
{
	public class HandshakeResponse
	{
		[JsonProperty("t")]
		public Timetoken Timetoken { get; set; }

		[JsonProperty("m")]
		public object[] Messages { get; set; }
	}

	public class Timetoken
	{
		[JsonProperty("t")]
		public string Timestamp { get; set; }

		[JsonProperty("r")]
		public int Region { get; set; }

	}

	public class HandshakeEffectHandler : IEffectHandler
	{
		EventEmitter emitter;
		HttpClient httpClient;
		public HandshakeEffectHandler(HttpClient client, EventEmitter emitter)
		{
			this.emitter = emitter;
			httpClient = client;
		}

		public async void Start(ExtendedState context)
		{
			var evnt = new Event();
			// TODO: Replace with Stateless Utility Methods
			// TODO: Fetch Configuration from PubNub instance
			try {
				var res = await httpClient.GetAsync($"https://ps.pndsn.com/v2/subscribe/demo/{String.Join(",", context.Channels.ToArray())}/0?uuid=cSharpTest&tt=0&tr=0&channel-group={String.Join(", ", context.ChannelGroups.ToArray())}");
				var handshakeResponse = JsonConvert.DeserializeObject<HandshakeResponse>(await res.Content.ReadAsStringAsync());
				evnt.EventPayload.Timetoken = handshakeResponse.Timetoken.Timestamp;
				evnt.EventPayload.Region = handshakeResponse.Timetoken.Region;
				evnt.Type = EventType.HandshakeSuccess;
			} catch (Exception ex) {
				evnt.Type = EventType.HandshakeFailed;
				evnt.EventPayload.exception = ex;
			}
			emitter.emit(evnt);
		}
		public void Cancel()
		{
			Console.WriteLine("Handshake can not be cancelled. Something is not right here!");
		}
	}
}
