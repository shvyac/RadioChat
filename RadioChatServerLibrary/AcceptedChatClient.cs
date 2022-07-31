using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	/// <summary>
	/// DobonChatServerが受け入れたTcpChatClient
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
				return this._loginState;
			}
			set
			{
				this._loginState = value;
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
				return this._name;
			}
			set
			{
				this._name = value;
			}
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public AcceptedChatClient() : base()
		{
			this._loginState = LoginState.Parted;
		}

		public AcceptedChatClient(Socket soc) : base(soc)
		{
			this._loginState = LoginState.Parted;
		}
	}
}
