using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PNEventEngine
{
	public class HandshakeReconnectingEffectHandler : IEffectHandler
	{
		EventEmitter emitter;
		HttpClient httpClient;
		IDelayUtil delayUtil;

		public HandshakeReconnectingEffectHandler(HttpClient client, EventEmitter emitter, IDelayUtil delayUtil)
		{
			this.emitter = emitter;
			this.httpClient = client;
			this.delayUtil = delayUtil;
		}

		public async void Start(ExtendedState context)
		{
			var evnt = new Event();

			if (delayUtil.ShouldGiveup(context.AttemptedReconnection)) {
				evnt.Type = EventType.Giveup;
				emitter.emit(evnt);
				return;
			}

			await Task.Run(() => delayUtil.Delay(context.AttemptedReconnection));

			try {
				var res = await httpClient.GetAsync($"https://ps.pndsn.com/v2/subscribe/demo/{String.Join(",", context.Channels.ToArray())}/0?uuid=cSharpTest&tt=0&tr=0&channel-group={String.Join(", ", context.ChannelGroups.ToArray())}");
				var handshakeResponse = JsonConvert.DeserializeObject<HandshakeResponse>(await res.Content.ReadAsStringAsync());
				evnt.EventPayload.Timetoken = handshakeResponse.Timetoken.Timestamp;
				evnt.EventPayload.Region = handshakeResponse.Timetoken.Region;
				evnt.EventPayload.ReconnectionAttemptsMade = 0;
				evnt.Type = EventType.HandshakeSuccess;
			} catch (Exception ex) {
				evnt.Type = EventType.HandshakeReconnectionFailed;
				evnt.EventPayload.ReconnectionAttemptsMade = context.AttemptedReconnection + 1;
				evnt.EventPayload.exception = ex;
			}
			emitter.emit(evnt);
		}

		public void Cancel()
		{
			Console.WriteLine("Handshake Reconnection attempt Cancelled!!!");
		}
	}
}
