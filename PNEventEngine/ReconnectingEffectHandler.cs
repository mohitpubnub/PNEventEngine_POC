using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PNEventEngine
{
	public enum ReconnectionPolicy { Linear, Exponential }

	public interface IDelayUtil
	{
		void Delay(int previousAttemptsCount);
		bool ShouldGiveup(int previousAttemptsCount);
	}

	public class DelayUtil : IDelayUtil
	{
		public ReconnectionPolicy Policy { get; set; }

		public int DefaultDelayInterval { get; set; }

		public int MaxAttempts { get; set; }

		public DelayUtil(int maxAttempts, ReconnectionPolicy policy = ReconnectionPolicy.Exponential, int defaultIntervalInMilliseconds = 5000)
		{
			Policy = policy;
			DefaultDelayInterval = defaultIntervalInMilliseconds;
			MaxAttempts = maxAttempts;
		}

		public async void Delay(int previousAttemptsCount = 0)
		{

			var timeInMilliseconds = (DefaultDelayInterval * Math.Pow(2, previousAttemptsCount)); // TODO: as per policy!!!!!  a standard definition across SDK.

			await Task.Delay(Convert.ToInt32($"{timeInMilliseconds}"));
		}

		public bool ShouldGiveup(int previousAttemptsCount) => previousAttemptsCount >= MaxAttempts;

	}

	public class ReconnectingEffectHandler : IEffectHandler
	{
		EventEmitter emitter;
		HttpClient httpClient;
		IDelayUtil delayUtil;
		CancellationTokenSource cancellationTokenSource;

		public ReconnectingEffectHandler(HttpClient client, EventEmitter emitter, IDelayUtil delayUtil)
		{
			this.emitter = emitter;
			this.httpClient = client;
			this.delayUtil = delayUtil;
		}

		public async void Start(ExtendedState context)    // TODO: Implementation of retry  getDelay() as per policy
		{
			if (cancellationTokenSource != null && cancellationTokenSource.Token.CanBeCanceled) {
				Cancel();
			}
			var evnt = new Event();

			if (delayUtil.ShouldGiveup(context.AttemptedReconnection)) {
				evnt.Type = EventType.Giveup;
				emitter.emit(evnt);
				return;
			}

			await Task.Run(()=> delayUtil.Delay(context.AttemptedReconnection));

			cancellationTokenSource = new CancellationTokenSource();

			try {
				var res = await httpClient.GetAsync($"https://ps.pndsn.com/v2/subscribe/demo/{String.Join(",", context.Channels.ToArray())}/0?uuid=cSharpTest&channel-group={String.Join(",", context.ChannelGroups.ToArray())}&tt={context.Timetoken}&tr={context.Region}", cancellationTokenSource.Token);
				var receivedResponse = JsonConvert.DeserializeObject<ReceiveingResponse>(await res.Content.ReadAsStringAsync());
				evnt.EventPayload.Timetoken = receivedResponse.Timetoken.Timestamp;
				evnt.EventPayload.Region = receivedResponse.Timetoken.Region;
				evnt.Type = EventType.ReceiveSuccess;

				if (receivedResponse.Messages != null)
					Console.WriteLine($"Received Messages {receivedResponse.Messages}");

			} catch (Exception ex) {
				evnt.Type = EventType.ReconnectionFailed;
				evnt.EventPayload.ReconnectionAttemptsMade += 1;
				evnt.EventPayload.exception = ex;
			}
			emitter.emit(evnt);
		}

		public void Cancel()
		{
			if (cancellationTokenSource.Token.CanBeCanceled) cancellationTokenSource.Cancel();
			Console.WriteLine("Reconnecting Cancelled!!!");
		}
	}
}
