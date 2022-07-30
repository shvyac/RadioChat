using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// ReceivedErrorEventArgs ‚ÌŠT—v‚Ìà–¾‚Å‚·B
	/// </summary>
	public class ReceivedErrorEventArgs : EventArgs
	{
		private string _errorMessage;
		public string ErrorMessage
		{
			get
			{
				return this._errorMessage;
			}
		}

		public ReceivedErrorEventArgs(string msg)
		{
			this._errorMessage = msg;
		}
	}
}
