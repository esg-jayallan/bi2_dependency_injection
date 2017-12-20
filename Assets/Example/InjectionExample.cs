using Injection;
using UnityEngine;

namespace Example
{
	public class InjectionExample : MonoBehaviour
	{
		private Injector _injector;

		public class InjectableA : IInjectable
		{
			public readonly string HelloMessage = "Hello from A";
		}

		public class InjectableB : IInjectable
		{
			public readonly string GoodbyeMessage = "Goodbye from B";
		}

		public class Injected
		{
			[Inject] protected InjectableA A;
			[Inject] protected InjectableB B;

			public void PrintMessages()
			{
				Debug.Log(A.HelloMessage);
				Debug.Log(B.GoodbyeMessage);
			}
		}

		protected void Start ()
		{
			_injector = new Injector();
		
			_injector.Bind<InjectableA>(new InjectableA());
			_injector.Bind<InjectableB>(new InjectableB());
			
			var injected = new Injected();
			_injector.Inject(injected);

			injected.PrintMessages();
		}
	
	}
}
