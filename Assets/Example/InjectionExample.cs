// MIT License
// 
// Copyright (c) 2017 Wooga
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 

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
