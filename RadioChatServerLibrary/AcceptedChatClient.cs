using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	/// <summary>
	/// RadioChatServerが受け入れたTcpChatClient
	/// </summary>
	public class AcceptedChatClient : TcpChatClient
	{
		private LoginState _loginState;
		/// <summary>
		/// ログイン状態
		/// </summary>
		public LoginState LoginState
		{
			get
			{
				return _loginState;
			}
			set
			{
				_loginState = value;
			}
		}

		private string _name = "";
		/// <summary>
		/// メンバー名
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public AcceptedChatClient() : base()
		{
			_loginState = LoginState.Parted;
		}

		public AcceptedChatClient(Socket soc) : base(soc)
		{
			_loginState = LoginState.Parted;
		}
	}
}
