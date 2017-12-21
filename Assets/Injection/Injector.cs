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

using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Injection
{
	public interface IInjectable
	{
	}

	[System.AttributeUsage( System.AttributeTargets.Field )]
	[MeansImplicitUse]
	public sealed class Inject : System.Attribute
	{
	}

	public class InjectorException : System.Exception
	{
		public InjectorException( string message ) : base( message )
		{
		}
	}

	public class Injector : IInjectable
	{
		public Injector( Injector parent = null )
		{
			_parentInjector = parent;
			Bind<Injector>( this );
		}

		private readonly Injector _parentInjector;

		private readonly Dictionary<System.Type, object> _objects = new Dictionary<System.Type, object>();

		public void Bind<T>( T obj ) where T : IInjectable
		{
			var type = typeof(T);

			//Debug.Log( "INJECTOR binding " + type.Name + " to " + obj.ToString() );
			_objects[ type ] = obj;
		}

		public void PostBindings()
		{
			foreach (var objKvp in _objects)
			{
				//Debug.Log( "Injecting bound " + objKvp.Key.Name );
				Inject( objKvp.Value );
			}
		}

		public void Inject( object obj )
		{
			//Debug.Log( "INJECTOR injecting " + obj.GetType().Name );

			var fields = Reflector.Reflect( obj.GetType() );
			var fieldsLength = fields.Length;
			for (var i = 0; i < fieldsLength; i++)
			{
				var field = fields[ i ];
				var value = Get( field.FieldType );
				field.SetValue( obj, value );

				//Debug.Log( "setting " + field.Name + " to " + value.ToString() );
			}
		}

		private object Get( System.Type type )
		{
			object obj = null;

			if (!_objects.TryGetValue( type, out obj ))
			{
				if (_parentInjector != null)
				{
					return _parentInjector.Get( type );
				}

				throw new InjectorException( "Could not get " + type.FullName + " from injector" );
			}

			return obj;
		}

		public T GetInstance<T>()
		{
			return (T)Get( typeof(T) );
		}

		private static class Reflector
		{
			private static readonly System.Type _injectAttributeType = typeof(Inject);
			private static readonly Dictionary<System.Type, FieldInfo[]> cachedFieldInfos = new Dictionary<System.Type, FieldInfo[]>();
			private static readonly List<FieldInfo> _reusableList = new List<FieldInfo>( 1024 );

			public static FieldInfo[] Reflect( System.Type type )
			{
				Assert.AreEqual( 0, _reusableList.Count, "Reusable list in Reflector was not empty!" );

				FieldInfo[] cachedResult;
				if (cachedFieldInfos.TryGetValue( type, out cachedResult ))
				{
					return cachedResult;
				}

				var fields = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy );
				for (var fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
				{
					var field = fields[ fieldIndex ];
					var hasInjectAttribute = field.IsDefined( _injectAttributeType, inherit: false );
					if (hasInjectAttribute)
					{
						_reusableList.Add( field );
					}
				}
				var resultAsArray = _reusableList.ToArray();
				_reusableList.Clear();
				cachedFieldInfos[ type ] = resultAsArray;
				return resultAsArray;
			}
		}
	}
}