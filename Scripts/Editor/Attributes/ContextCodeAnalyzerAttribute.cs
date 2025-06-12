using System;
using System.Text;

namespace Expecto
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
	public class ContextCodeAnalyzerAttribute : Attribute
	{
#if UNITY_EDITOR
		public string Context { get; private set; }
#endif

		public ContextCodeAnalyzerAttribute(string @purpose, string @usage, string @params = null, string @returns = null, string @notes = null)
		{
#if UNITY_EDITOR
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"Purpose: {@purpose}");
			sb.AppendLine($"Usage: {@usage}");
			if (@params != null)
			{
				sb.AppendLine($"Params: {@params}");
			}
			if (@returns != null)
			{
				sb.AppendLine($"Returns: {@returns}");
			}
			if (@notes != null)
			{
				sb.AppendLine($"Notes: {@notes}");
			}
			Context = sb.ToString();
#endif
		}
	}
}
