using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe server with a callback channel.
	/// </summary>
	/// <typeparam name="TRequesting">The callback channel interface that the client will be handling.</typeparam>
	public interface IPipeServerWithCallback<TRequesting> : IPipeServer, IPipeInvokerHost<TRequesting>
		where TRequesting : class
	{
	}
}