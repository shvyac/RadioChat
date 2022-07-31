using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	/// <summary>
	/// RadioChatServer���󂯓��ꂽTcpChatClient
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
				return _loginState;
			}
			set
			{
				_loginState = value;
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
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		/// <summary>
		/// �R���X�g���N�^
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
