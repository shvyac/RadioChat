using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// ReceivedDataEventArgs の概要の説明です。
	/// </summary>
	public class ReceivedDataEventArgs : EventArgs
	{
		private string _receivedString;
		public string ReceivedString
		{
			get
			{
				return _receivedString;
			}
		}

		private TcpChatClient _client;
		public TcpChatClient Client
		{
			get
			{
				return _client;
			}
		}

		public ReceivedDataEventArgs(TcpChatClient c, string str)
		{
			_client = c;
			_receivedString = str;
		}
	}
}
