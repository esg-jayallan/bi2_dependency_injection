﻿// MIT License
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

using System.Reflection;
using NUnit.Framework;

// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Injection.Editor
{
	[TestFixture]
	public class InjectorTests
	{
		private class TestClass : IInjectable
		{
		}

		private interface ITestInterface : IInjectable
		{
		}

		private class TestClassFromInterface : ITestInterface
		{
		}

		private class TestInjectingClass : IInjectable
		{
			[Inject]
			protected TestClass _testClass;
			[Inject]
			protected ITestInterface _otherTestClass;

			public bool PostConstructExecuted { get; private set; }

			public TestClass GetTestClass()
			{
				return _testClass;
			}

			public ITestInterface GetOtherTestClass()
			{
				return _otherTestClass;
			}

			[PostConstruct]
			private void PostConstruct()
			{
				PostConstructExecuted = true;
			}
		}

		#region - binding

		[Test]
		public void TestGetUnbound()
		{
			var injector = new Injector();

			Assert.Throws<InjectorException>( () => { injector.GetInstance<TestClass>(); },
			                                  "Empty injector should throw on get." );
		}

		[Test]
		public void TestBindClass()
		{
			var injector = new Injector();

			var testClass = new TestClass();

			injector.Bind<TestClass>( testClass );

			Assert.DoesNotThrow( () => { injector.GetInstance<TestClass>(); }, "Injector should have instance." );

			Assert.AreEqual( testClass, injector.GetInstance<TestClass>(), "instance should be the same" );
		}

		[Test]
		public void TestGetUnboundInterface()
		{
			var injector = new Injector();

			Assert.Throws<InjectorException>( () => { injector.GetInstance<ITestInterface>(); },
			                                  "Empty injector should throw on get interface." );
		}

		[Test]
		public void TestBindClassGetInterface()
		{
			var injector = new Injector();

			var testClass = new TestClassFromInterface();

			injector.Bind<TestClassFromInterface>( testClass );

			Assert.Throws<InjectorException>( () => { injector.GetInstance<ITestInterface>(); },
			                                  "injector should throw on get interface if just the class was bound." );
		}

		[Test]
		public void TestBindInterface()
		{
			var injector = new Injector();
			var testClass = new TestClassFromInterface();

			injector.Bind<ITestInterface>( testClass );

			Assert.Throws<InjectorException>( () => { injector.GetInstance<TestClassFromInterface>(); },
			                                  "getting class should throw if only interface bound." );

			Assert.DoesNotThrow( () => { injector.GetInstance<ITestInterface>(); }, "Injector should have instance." );

			Assert.AreEqual( testClass, injector.GetInstance<ITestInterface>(), "instance should be the same" );
		}

		#endregion

		#region - parent injector

		[Test]
		public void TestGetFromParent()
		{
			var parentInjector = new Injector();
			var injector = new Injector( parentInjector );

			var testClass = new TestClass();

			parentInjector.Bind( testClass );

			Assert.DoesNotThrow( () => { injector.GetInstance<TestClass>(); }, "Injector should have instance from parent." );

			Assert.AreEqual( testClass, injector.GetInstance<TestClass>(), "instance from parent should be the same" );
		}

		[Test]
		public void TestGetFromAncestor()
		{
			var parentInjector = new Injector();

			var testClass = new TestClass();
			parentInjector.Bind( testClass );

			Injector injector = null;
			for (var i = 0; i < 10; i++)
			{
				injector = new Injector( parentInjector );
				parentInjector = injector;
			}

			Assert.DoesNotThrow( () => { injector.GetInstance<TestClass>(); }, "Injector should have instance from ancestor." );

			Assert.AreEqual( testClass, injector.GetInstance<TestClass>(), "instance from ancestor should be the same" );
		}

		[Test]
		public void TestOverrideParent()
		{
			var parentInjector = new Injector();
			var injector = new Injector( parentInjector );

			var testClassInParent = new TestClass();
			parentInjector.Bind( testClassInParent );

			var testClassInChild = new TestClass();
			injector.Bind( testClassInChild );

			Assert.DoesNotThrow( () => { injector.GetInstance<TestClass>(); }, "Injector should have instance." );

			Assert.AreEqual( testClassInChild, injector.GetInstance<TestClass>(), "instance from parent should be overridden" );
		}

		#endregion

		#region - injecting

		[Test]
		public void TestInject()
		{
			var injector = new Injector();
			var testClass = new TestClass();
			var otherTestClass = new TestClassFromInterface();

			injector.Bind<TestClass>( testClass );
			injector.Bind<ITestInterface>( otherTestClass );

			var injectingClass = new TestInjectingClass();
			injector.Inject( injectingClass );

			Assert.AreEqual( testClass, injectingClass.GetTestClass(), "instance should be injected" );
			Assert.AreEqual( otherTestClass, injectingClass.GetOtherTestClass(), "instance should be injected" );
		}

		[Test]
		public void TestPostBindings()
		{
			var injector = new Injector();
			var testClass = new TestClass();
			var otherTestClass = new TestClassFromInterface();

			injector.Bind<TestClass>( testClass );
			injector.Bind<ITestInterface>( otherTestClass );

			var injectingClass = new TestInjectingClass();
			injector.Bind<TestInjectingClass>( injectingClass );
			injector.PostBindings();

			Assert.AreEqual( injectingClass, injector.GetInstance<TestInjectingClass>(), "should return bound instance" );

			Assert.AreEqual( testClass, injectingClass.GetTestClass(), "instance should be injected" );
			Assert.AreEqual( otherTestClass, injectingClass.GetOtherTestClass(), "instance should be injected" );
		}

		[Test]
		public void TestPostConstruct()
		{
			var injector = new Injector();
			var testClass = new TestClass();
			var otherTestClass = new TestClassFromInterface();

			injector.Bind<TestClass>( testClass );
			injector.Bind<ITestInterface>(otherTestClass);

			var injectingClass = new TestInjectingClass();
			injector.Bind<TestInjectingClass>( injectingClass );
			injector.PostBindings();

			Assert.IsTrue( injectingClass.PostConstructExecuted, "PostConstruct method should be executed" );
		}

		#endregion

		#region - audit

		[Test]
		public void TestAuditProtectionLevel()
		{
			var assembly = typeof(Injector).Assembly;

			var types = assembly.GetTypes();
			for (var typeIndex = 0; typeIndex < types.Length; typeIndex++)
			{
				var type = types[ typeIndex ];

				AuditType( type );
			}
		}

		public static void AuditType( System.Type type )
		{
			var fields = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy );

			for (var fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
			{
				var field = fields[ fieldIndex ];
				var attr = field.GetCustomAttributes( typeof(Inject), inherit: false );
				if (attr.Length > 0)
				{
					Assert.IsTrue( field.IsFamily, "Field " + field.Name + " in " + type.Name + " should be protected" );
					Assert.IsFalse( field.IsPublic, "Field " + field.Name + " in " + type.Name + " should not be public" );
					Assert.IsFalse( field.IsPrivate, "Field " + field.Name + " in " + type.Name + " must not be private" );
				}
			}
		}

		#endregion
	}
}
