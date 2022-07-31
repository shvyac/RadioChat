using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// ReceivedErrorEventArgs �̊T�v�̐����ł��B
	/// </summary>
	public class ReceivedErrorEventArgs : EventArgs
	{
		private string _errorMessage;
		public string ErrorMessage
		{
			get
			{
				return _errorMessage;
			}
		}

		public ReceivedErrorEventArgs(string msg)
		{
			_errorMessage = msg;
		}
	}
}
