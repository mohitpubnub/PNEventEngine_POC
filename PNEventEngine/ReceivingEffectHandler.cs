using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PNEventEngine
{
	public class ReceiveingResponse
	{
		[JsonProperty("t")]
		public Timetoken Timetoken { get; set; }

		[JsonProperty("m")]
		public object[] Messages { get; set; }
	}

	public class ReceivingEffectHandler : IEffectHandler
	{
		EventEmitter emitter;
		HttpClient httpClient;
		CancellationTokenSource cancellationTokenSource;
		public ReceivingEffectHandler(HttpClient client, EventEmitter emitter)
		{
			this.emitter = emitter;
			httpClient = client;
		}

		public async void Start(ExtendedState context)
		{
			if (cancellationTokenSource != null && cancellationTokenSource.Token.CanBeCanceled) {
				Cancel();
			}
			cancellationTokenSource = new CancellationTokenSource();
			var evnt = new Event();
			// TODO: Replace with stateless Utility method...
			try {
				var res = await httpClient.GetAsync($"https://ps.pndsn.com/v2/subscribe/demo/{String.Join(",", context.Channels.ToArray())}/0?uuid=cSharpTest&channel-group={String.Join(",", context.ChannelGroups.ToArray())}&tt={context.Timetoken}&tr={context.Region}", cancellationTokenSource.Token);
				var receivedResponse = JsonConvert.DeserializeObject<ReceiveingResponse>(await res.Content.ReadAsStringAsync());
				evnt.EventPayload.Timetoken = receivedResponse.Timetoken.Timestamp;
				evnt.EventPayload.Region = receivedResponse.Timetoken.Region;
				evnt.Type = EventType.ReceiveSuccess;

				if (receivedResponse.Messages != null)
					Console.WriteLine($"Received Messages {receivedResponse.Messages}");    //WIP: Define "DELIVERING" Effect. and transition

			} catch (Exception ex) {
				evnt.EventPayload.exception = ex;
			}
			emitter.emit(evnt);
		}
		public void Cancel()
		{
			Console.WriteLine("Attempting cancellation");
			cancellationTokenSource.Cancel();
		}
	}
}
