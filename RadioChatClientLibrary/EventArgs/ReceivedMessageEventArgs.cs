using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// ReceivedMessageEventArgs ‚ÌŠT—v‚Ìà–¾‚Å‚·B
	/// </summary>
	public class ReceivedMessageEventArgs : EventArgs
	{
		private string _from;
		public string From
		{
			get
			{
				return this._from;
			}
		}

		private string _message;
		public string Message
		{
			get
			{
				return this._message;
			}
		}

		private bool _privateMessage;
		public bool PrivateMessage
		{
			get
			{
				return this._privateMessage;
			}
		}

		public ReceivedMessageEventArgs(string fromMem, string msg) : this(fromMem, msg, false)
		{
		}

		public ReceivedMessageEventArgs(string fromMem, string msg, bool privMsg)
		{
			this._from = fromMem;
			this._message = msg;
			this._privateMessage = privMsg;
		}
	}
}
