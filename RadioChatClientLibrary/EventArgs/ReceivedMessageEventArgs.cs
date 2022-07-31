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
				return _from;
			}
		}

		private string _message;
		public string Message
		{
			get
			{
				return _message;
			}
		}

		private bool _privateMessage;
		public bool PrivateMessage
		{
			get
			{
				return _privateMessage;
			}
		}

		public ReceivedMessageEventArgs(string fromMem, string msg) : this(fromMem, msg, false)
		{
		}

		public ReceivedMessageEventArgs(string fromMem, string msg, bool privMsg)
		{
			_from = fromMem;
			_message = msg;
			_privateMessage = privMsg;
		}
	}
}
