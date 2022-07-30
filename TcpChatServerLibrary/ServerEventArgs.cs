using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// ServerEventArgs ‚ÌŠT—v‚Ìà–¾‚Å‚·B
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
			this._client = c;
		}
	}
}
