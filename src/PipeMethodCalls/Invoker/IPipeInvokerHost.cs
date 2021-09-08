using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Holds an Invoker property.
	/// </summary>
	/// <typeparam name="TRequesting">The type to make method calls for.</typeparam>
	public interface IPipeInvokerHost<TRequesting>
		where TRequesting : class
	{
		/// <summary>
		/// Get the invoker.
		/// </summary>
		IPipeInvoker<TRequesting> Invoker { get; }
	}
}
