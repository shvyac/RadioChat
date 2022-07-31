using System;
using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	/// <summary>
	/// Dobon Chat Server�̋@�\��񋟂���
	/// </summary>
	public class RadioChatServer : TcpChatServer
	{
		#region �C�x���g
		/// <summary>
		/// �����o�����O�C������
		/// </summary>
		public event ServerEventHandler LoggedinMember;
		private void OnLoggedinMember(ServerEventArgs e)
		{
			if (this.LoggedinMember != null)
			{
				this.LoggedinMember(this, e);
			}
		}

		/// <summary>
		/// �����o�����O�A�E�g����
		/// </summary>
		public event ServerEventHandler LoggedoutMember;
		private void OnLoggedoutMember(ServerEventArgs e)
		{
			if (this.LoggedoutMember != null)
			{
				this.LoggedoutMember(this, e);
			}
		}
		#endregion

		/// <summary>
		/// DobonChatServer�̃R���X�g���N�^
		/// </summary>
		public RadioChatServer() : base()
		{
		}

		public override void SendMessageToAllClients(string msg)
		{
			//CRLF���폜
			msg = msg.Replace("\r\n", "");

			this.SendToAllClients(ServerCommands.Message + " _HOST " + msg);
		}

		/// <summary>
		/// ���O����N���C�A���g��T��
		/// </summary>
		/// <param name="nickName">�T�����O</param>
		/// <returns>������������AcceptedChatClient�I�u�W�F�N�g</returns>
		public AcceptedChatClient FindMember(string nickName)
		{
			lock (this._acceptedClients.SyncRoot)
			{
				foreach (AcceptedChatClient c in this._acceptedClients)
				{
					if (c.Name == nickName)
						return c;
				}
			}

			return null;
		}

		//�N���C�A���g����̃f�[�^����M������
		protected override void OnReceivedData(ReceivedDataEventArgs e)
		{
			base.OnReceivedData(e);

			AcceptedChatClient client = (AcceptedChatClient) e.Client;

			//��M����������𕪉�����
			string[] cmds = e.ReceivedString.Split(new char[] {' '}, 2);

			//�R�}���h�𒲂ׂ�
			if (ClientCommands.Login == cmds[0])
			{
				//�`���b�g�Q���R�}���h
				if (client.LoginState != LoginState.Parted)
				{
					this.SendErrorMessage(client, "���łɎQ�����Ă��܂��B");
					return;
				}

				//���O���K����
				string nickName = cmds[1];
				if (nickName.Length == 0 || nickName.IndexOf(' ') >= 0 ||
					nickName.StartsWith("_"))
				{
					this.SendErrorMessage(client, "���O���s���ł��B");
					return;
				}

				//�������O���Ȃ������ׂ�
				lock (this._acceptedClients.SyncRoot)
				{
					foreach (AcceptedChatClient c in this._acceptedClients)
					{
						if (nickName == c.Name)
						{
							this.SendErrorMessage(client, "�������O�̃����o�[�����łɃ��O�C�����Ă��܂��B");
							return;
						}
					}

					//���O�A��Ԃ̍X�V
					client.Name = nickName;
					client.LoginState = LoginState.Joined;
				}

				//�C�x���g����
				this.OnLoggedinMember(new ServerEventArgs(client));
				//�N���C�A���g�ɒʒm
				this.SendToAllClients(ServerCommands.JoinMember + " _HOST " + client.Name);
				//�����o���X�g�𑗂�
				this.SendMembersList(client);
			}
			else if (ClientCommands.Logout == cmds[0])
			{
				//�ގ��R�}���h
				if (client.LoginState != LoginState.Joined)
				{
					this.SendErrorMessage(client, "�`���b�g�ɎQ�����Ă��܂���B");
					return;
				}
				
				//��Ԃ̍X�V
				client.LoginState = LoginState.Parted;
				//�C�x���g����
				this.OnLoggedoutMember(new ServerEventArgs(client));
				//�N���C�A���g�ɒʒm
				this.SendToAllClients(ServerCommands.PartMember + " _HOST " + client.Name);
			}
			else if (ClientCommands.MembersList == cmds[0])
			{
				//�����o���X�g�v���R�}���h
				if (client.LoginState != LoginState.Joined)
				{
					this.SendErrorMessage(client, "�`���b�g�ɎQ�����Ă��܂���B");
					return;
				}

				//�����o���X�g�𑗂�
				this.SendMembersList(client);
			}
			else if (ClientCommands.Message == cmds[0])
			{
				//���b�Z�[�W���M�R�}���h
				if (client.LoginState != LoginState.Joined)
				{
					this.SendErrorMessage(client, "�`���b�g�ɎQ�����Ă��܂���B");
					return;
				}

				//�N���C�A���g�Ƀ��b�Z�[�W�𑗐M
				this.SendToAllClients(ServerCommands.Message + " " + client.Name + " " + cmds[1]);
			}
			else if (ClientCommands.PrivateMessage == cmds[0])
			{
				//�v���C�x�[�g���b�Z�[�W���M�R�}���h
				if (client.LoginState != LoginState.Joined)
				{
					this.SendErrorMessage(client, "�`���b�g�ɎQ�����Ă��܂���B");
					return;
				}

				string[] msgs = cmds[1].Split(new char[] {' '}, 2);

				//���O����N���C�A���g��T��
				AcceptedChatClient toClient = this.FindMember(msgs[0]);
				if (toClient == null)
				{
					this.SendErrorMessage(client, "���M��̎Q���҂�������܂���B");
					return;
				}

				//�N���C�A���g�Ƀ��b�Z�[�W�𑗐M
				toClient.Send(ServerCommands.PrivateMessage + " " + client.Name + " " + msgs[1]);
			}
		}

		//�N���C�A���g���ؒf������
		protected override void OnDisconnectedClient(ServerEventArgs e)
		{
			AcceptedChatClient ac = (AcceptedChatClient) e.Client;
			//���O�C�����Ă���Ƃ��́A���O���o���Ă���
			string clientName = "";
			if (ac.LoginState == LoginState.Joined)
			{
				clientName = ac.Name;
			}
			
			//��{�N���X��OnDisconnectedClient������
			base.OnDisconnectedClient(e);

			//�����o�̃��O�A�E�g��ʒm����
			if (clientName.Length > 0)
				this.SendToAllClients(ServerCommands.PartMember + " _HOST " + clientName);
		}

		protected override TcpChatClient CreateChatClient(Socket soc)
		{
			return new AcceptedChatClient(soc);
		}

		/// <summary>
		/// �G���[���b�Z�[�W���N���C�A���g�ɑ��M
		/// </summary>
		/// <param name="client">���M��̃N���C�A���g</param>
		/// <param name="msg">���b�Z�[�W</param>
		protected override void SendErrorMessage(TcpChatClient client, string msg)
		{
			client.Send(ServerCommands.Error + " _HOST " + msg);
		}

		/// <summary>
		/// �����o���X�g�𑗐M����
		/// </summary>
		private void SendMembersList(TcpChatClient client)
		{
			string msg = "";
			lock (this._acceptedClients.SyncRoot)
			{
				foreach (AcceptedChatClient c in this._acceptedClients)
				{
					msg += c.Name + " ";
				}
			}
			msg = msg.TrimEnd(' ');

			client.Send(ServerCommands.MembersList + " _HOST " + msg);
		}
	}
}
