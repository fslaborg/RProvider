using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	internal interface IMessage
	{
		/// <summary>
		/// The call ID.
		/// </summary>
		long CallId { get; }
	}
}
