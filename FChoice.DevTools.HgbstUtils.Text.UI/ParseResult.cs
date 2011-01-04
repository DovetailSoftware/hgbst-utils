using System;

namespace FChoice.DevTools.HgbstUtils.Text.UI
{
	public class ParseResult
	{
		internal ParseResult()
		{
			ErrorArgIndex = -1;
			WasSuccess = true;
		}

		public bool WasSuccess { get; private set; }
		public int ErrorArgIndex { get; private set; }
		public string ErrorArgValue { get; private set; }
		public string GroupName { get; private set; }
		public string Message { get; private set; }

		internal void UpdateStatus(bool wasSuccess, int argIdx, string argVal)
		{
			WasSuccess = wasSuccess;
			ErrorArgIndex = argIdx;
			ErrorArgValue = argVal;
		}

		internal void UpdateStatus(bool wasSuccess, string groupName)
		{
			WasSuccess = wasSuccess;
			GroupName = groupName;
		}

		internal void UpdateStatus(bool wasSuccess, int argIdx, string argVal, string format, params object[] args)
		{
			UpdateStatus(wasSuccess, argIdx, argVal);
			Message = String.Format(format, args);
		}
	}
}