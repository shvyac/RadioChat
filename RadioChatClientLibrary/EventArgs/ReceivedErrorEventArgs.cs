using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// ReceivedErrorEventArgs の概要の説明です。
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
