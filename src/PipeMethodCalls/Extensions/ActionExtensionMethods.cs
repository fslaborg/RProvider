using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Extension methods for actions.
	/// </summary>
	internal static class ActionExtensionMethods
	{
		/// <summary>
		/// Invokes the given action as a logger.
		/// </summary>
		/// <param name="logger">The action to invoke as a logger.</param>
		/// <param name="messageFunc">The function to produce the string to log.</param>
		public static void Log(this Action<string> logger, Func<string> messageFunc)
		{
			logger?.Invoke(messageFunc());
		}
	}
}
