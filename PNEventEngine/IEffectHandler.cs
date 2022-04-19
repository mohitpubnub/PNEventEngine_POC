namespace PNEventEngine
{

	public enum EffectType
	{
		SendHandshakeRequest,  // oneshot
		ReceiveEventRequest,   // long running
		ReceiveReconnection,
		HandshakeReconnection
	}

	public interface IEffectHandler
	{
		void Start(ExtendedState context);
		void Cancel();
	}
}
