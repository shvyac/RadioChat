using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// ServerEventArgs の概要の説明です。
	/// </summary>
	public class ServerEventArgs
	{
		private TcpChatClient _client;
		public TcpChatClient Client
		{
			get
			{
				return _client;
			}
		}

		public ServerEventArgs(TcpChatClient c)
		{
			_client = c;
		}
	}
}
