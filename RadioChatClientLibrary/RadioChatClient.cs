using System;
using System.Net;
using System.Net.Sockets;
using Radio.Net.Chat;

namespace Radio.Net.Chat
{
    #region �f���Q�[�g
    //�C�x���g���������郁�\�b�h��\���f���Q�[�g
    public delegate void ReceivedMessageEventHandler(object sender, ReceivedMessageEventArgs e);
	public delegate void MemberEventHandler(object sender, MemberEventArgs e);
	public delegate void MembersListEventHandler(object sender, MembersListEventArgs e);
	public delegate void ReceivedErrorEventHandler(object sender, ReceivedErrorEventArgs e);
	#endregion

	#region �񋓌^
	/// <summary>
	/// �`���b�g�ւ̎Q�����
	/// </summary>
	public enum LoginState
	{
		/// <summary>
		/// �Q����v�����Ă���
		/// </summary>
		WaitJoin,
		/// <summary>
		/// �Q�����Ă���
		/// </summary>
		Joined,
		/// <summary>
		/// �ގ���v�����Ă���
		/// </summary>
		WaitPart,
		/// <summary>
		/// �ގ����Ă���
		/// </summary>
		Parted
	}
	#endregion

	/// <summary>
	/// Dobon Chat Client�̋@�\��񋟂���
	/// </summary>
	public class RadioChatClient : TcpChatClient
	{
		#region �C�x���g
		/// <summary>
		/// ���b�Z�[�W���󂯎����
		/// </summary>
		public event ReceivedMessageEventHandler ReceivedMessage;
		private void OnReceivedMessage(ReceivedMessageEventArgs e)
		{
			if (this.ReceivedMessage != null)
			{
				this.ReceivedMessage(this, e);
			}
		}

		/// <summary>
		/// �G���[���󂯎����
		/// </summary>
		public event ReceivedErrorEventHandler ReceivedError;
		private void OnReceivedError(ReceivedErrorEventArgs e)
		{
			//�Q���v�������s������
			if (this.LoginState == LoginState.WaitJoin)
			{
				this._loginState = LoginState.Parted;
			}

			if (this.ReceivedError != null)
			{
				this.ReceivedError(this, e);
			}
		}

		/// <summary>
		/// �����o�[���Q������
		/// </summary>
		public event MemberEventHandler JoinedMember;
		private void OnJoinedMember(MemberEventArgs e)
		{
			//�����̎��͎Q����Ԃ�
			if (this._name == e.Name)
			{
				this._loginState = LoginState.Joined;
			}

			if (this.JoinedMember != null)
			{
				this.JoinedMember(this, e);
			}
		}

		/// <summary>
		/// �����o�[���ގ�����
		/// </summary>
		public event MemberEventHandler PartedMember;
		private void OnPartedMember(MemberEventArgs e)
		{
			//�����̎��͑ގ���Ԃ�
			if (this._name == e.Name)
			{
				this._loginState = LoginState.Parted;
			}

			if (this.PartedMember != null)
			{
				this.PartedMember(this, e);
			}
		}

		/// <summary>
		/// �����o�[���X�g�������Ă���
		/// </summary>
		public event MembersListEventHandler UpdatedMembers;
		private void OnUpdatedMembers(MembersListEventArgs e)
		{
			if (this.UpdatedMembers != null)
			{
				this.UpdatedMembers(this, e);
			}
		}
		#endregion
		
		#region �v���p�e�B
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
		}
		#endregion

		/// <summary>
		/// �R���X�g���N�^
		/// </summary>
		public RadioChatClient() : base()
		{
			this._loginState = LoginState.Parted;
		}
		public RadioChatClient(Socket soc) : base(soc)
		{
			this._loginState = LoginState.Parted;
		}

		/// <summary>
		/// �T�[�o�[�ɐڑ�����
		/// </summary>
		/// <param name="host">�T�[�o�[�̃z�X�g��</param>
		/// <param name="port">�|�[�g�ԍ�</param>
		/// <param name="nickName">�����o�[��</param>
		public void Connect(string host, int port, string nickName)
		{
			if (nickName.Length == 0)
				throw new ApplicationException("���O���w�肳��Ă��܂���B");
			if (nickName.IndexOf(' ') >= 0)
				throw new ApplicationException("���O�ɃX�y�[�X���������邱�Ƃ͂ł��܂���B");
			if (nickName.StartsWith("_"))
				throw new ApplicationException("'_'�Ŏn�܂閼�O�͕t���邱�Ƃ��ł��܂���B");

			//���O��ۑ�����
			this._name = nickName;
			//�ڑ�����
			base.Connect(host, port);
		}

		/// <summary>
		/// ���b�Z�[�W�𑗐M����
		/// </summary>
		/// <param name="msg">���b�Z�[�W</param>
		public override void SendMessage(string msg)
		{
			if (this.LoginState != LoginState.Joined)
				throw new ApplicationException("�`���b�g�ɎQ�����Ă��܂���B");

			//CRLF���폜
			msg = msg.Replace("\r\n", "");

			this.Send(ClientCommands.Message + " " + msg);
		}

		/// <summary>
		/// �v���C�x�[�g���b�Z�[�W�𑗐M����
		/// </summary>
		/// <param name="msg">���b�Z�[�W</param>
		/// <param name="to">���b�Z�[�W�𑗂鑊��̖��O</param>
		public void SendPrivateMessage(string msg, string to)
		{
			if (this.LoginState != LoginState.Joined)
				throw new ApplicationException("�`���b�g�ɎQ�����Ă��܂���B");

			//CRLF���폜
			msg = msg.Replace("\r\n", "");

			this.Send(ClientCommands.PrivateMessage + " " + to + " " + msg);
		}

		//�f�[�^����M������
		protected override void OnReceivedData(ReceivedDataEventArgs e)
		{
			base.OnReceivedData(e);

			RadioChatClient client = (RadioChatClient) e.Client;

			//��M����������𕪉�����
			string[] cmds = e.ReceivedString.Split(new char[] {' '}, 3);

			//�R�}���h�����߂���
			if (ServerCommands.Error == cmds[0])
			{
				//�G���[�R�}���h
				this.OnReceivedError(new ReceivedErrorEventArgs(cmds[2]));
			}
			else if (ServerCommands.Message == cmds[0])
			{
				//���b�Z�[�W�R�}���h
				this.OnReceivedMessage(
					new ReceivedMessageEventArgs(cmds[1], cmds[2]));
			}
			else if (ServerCommands.PrivateMessage == cmds[0])
			{
				//�v���C�x�[�g���b�Z�[�W�R�}���h
				this.OnReceivedMessage(
					new ReceivedMessageEventArgs(cmds[1], cmds[2], true));
			}
			else if (ServerCommands.JoinMember == cmds[0])
			{
				//�����o�[�Q���R�}���h
				this.OnJoinedMember(new MemberEventArgs(cmds[2]));
			}
			else if (ServerCommands.PartMember == cmds[0])
			{
				//�����o�[�ގ��R�}���h
				this.OnPartedMember(new MemberEventArgs(cmds[2]));
			}
			else if (ServerCommands.MembersList == cmds[0])
			{
				//�����o�[���X�g�R�}���h
				this.OnUpdatedMembers(new MembersListEventArgs(cmds[2].Split(' ')));
			}
		}

		protected override void OnConnected(EventArgs e)
		{
			base.OnConnected(e);

			//�`���b�g�ւ̎Q����v������
			this._loginState = LoginState.WaitJoin;
			string line = ClientCommands.Login + " " + this._name;
			this.Send(line);
		}

	}
}
