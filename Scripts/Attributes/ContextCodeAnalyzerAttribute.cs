using System;
using System.Text;

namespace Expecto
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
	public class ContextCodeAnalyzerAttribute : Attribute
	{
#if UNITY_EDITOR
		public string Context { get; private set; }
#endif

		public ContextCodeAnalyzerAttribute(string @purpose, string @usage, string @params = null, string @returns = null, string @notes = null)
		{
#if UNITY_EDITOR
			StringBuilder sb = new();
			sb.Append($"Purpose: {@purpose}");
			sb.Append($"; ");
			sb.Append($"Usage: {@usage}");
			sb.Append($"; ");
			if (@params != null)
			{
				sb.Append($"Params: {@params}");
				sb.Append($"; ");
			}
			if (@returns != null)
			{
				sb.Append($"Returns: {@returns}");
				sb.Append($"; ");
			}
			if (@notes != null)
			{
				sb.Append($"Notes: {@notes}");
				sb.Append($"; ");
			}
			sb.Remove(sb.Length - 2, 2);
			Context = sb.ToString();
#endif
		}
	}
}
