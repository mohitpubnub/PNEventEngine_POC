using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PNEventEngine
{
	public class EffectDispatcher
	{
		public Dictionary<EffectType, IEffectHandler> effectActionMap;
		public EffectDispatcher()
		{
			effectActionMap = new Dictionary<EffectType, IEffectHandler>();
		}

		public void dispatch(EffectType effect, ExtendedState stateContext)
		{
			IEffectHandler handler;
			if (effectActionMap.TryGetValue(effect, out handler)) {
				handler.Start(stateContext);	// TODO: Refactor - Some Threading specific
				//await handler.Start(stateContext);
			}
		}

		public void Register(EffectType type, IEffectHandler handler)
		{
			effectActionMap.Add(type, handler);
		}
	}
}
