using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	/// <summary>
	/// DobonChatServer���󂯓��ꂽTcpChatClient
	/// </summary>
	public class AcceptedChatClient : TcpChatClient
	{
		private LoginState _loginState;
		/// <summary>
		/// ���O�C�����
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
		/// �����o�[��
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
		/// �R���X�g���N�^
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
