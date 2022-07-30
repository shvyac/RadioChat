using System;
using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	#region �f���Q�[�g
	/// <summary>
	/// �N���C�A���g�������C�x���g���������郁�\�b�h��\��
	/// </summary>
	public delegate void ServerEventHandler(object sender, ServerEventArgs e);
	#endregion

	#region �񋓌^
	/// <summary>
	/// �T�[�o�[�̏��
	/// </summary>
	public enum ServerState
	{
		None,
		Listening,
		Stopped
	}
	#endregion

	/// <summary>
	/// TCP�`���b�g�T�[�o�[�̊�{�I�ȋ@�\��񋟂���
	/// </summary>
	public class TcpChatServer : IDisposable
	{
		#region IDisposable �����o
		/// <summary>
		/// �j������
		/// </summary>
		public virtual void Dispose()
		{
			this.Close();
		}
		#endregion

		#region �C�x���g
		/// <summary>
		/// �N���C�A���g���󂯓��ꂽ
		/// </summary>
		public event ServerEventHandler AcceptedClient;
		protected virtual void OnAcceptedClient(ServerEventArgs e)
		{
			if (this.AcceptedClient != null)
			{
				this.AcceptedClient(this, e);
			}
		}

		/// <summary>
		/// �N���C�A���g���f�[�^����M����
		/// </summary>
		public event ReceivedDataEventHandler ReceivedData;
		protected virtual void OnReceivedData(ReceivedDataEventArgs e)
		{
			if (this.ReceivedData != null)
			{
				this.ReceivedData(this, e);
			}
		}

		/// <summary>
		/// �N���C�A���g���ؒf����
		/// </summary>
		public event ServerEventHandler DisconnectedClient;
		protected virtual void OnDisconnectedClient(ServerEventArgs e)
		{
			if (this.DisconnectedClient != null)
			{
				this.DisconnectedClient(this, e);
			}
		}
		#endregion

		#region �v���p�e�B
		private Socket _server;
		/// <summary>
		/// ��ɂȂ�Socket
		/// </summary>
		protected Socket Server
		{
			get
			{
				return this._server;
			}
		}

		protected ServerState _serverState;
		/// <summary>
		/// ���
		/// </summary>
		public ServerState ServerState
		{
			get
			{
				return this._serverState;
			}
		}

		private IPEndPoint _socketEP;
		/// <summary>
		/// ���[�J���G���h�|�C���g
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get
			{
				return this._socketEP;
			}
		}

		protected System.Collections.ArrayList _acceptedClients;
		/// <summary>
		/// �ڑ����̃N���C�A���g
		/// </summary>
		public virtual TcpChatClient[] AcceptedClients
		{
			get
			{
				return (TcpChatClient[]) this._acceptedClients.ToArray(typeof(TcpChatClient));
			}
		}

		private int _maxClients;
		/// <summary>
		/// �����ڑ���������N���C�A���g��
		/// </summary>
		public int MaxClients
		{
			get
			{
				return this._maxClients;
			}
			set
			{
				this._maxClients = value;
			}
		}
		#endregion

		#region �t�B�[���h
		#endregion

		/// <summary>
		/// TcpChatServer�̃R���X�g���N�^
		/// </summary>
		public TcpChatServer()
		{
			this._maxClients = 100;
			this._server = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);
			this._acceptedClients =
				System.Collections.ArrayList.Synchronized(
				new System.Collections.ArrayList());
		}

		/// <summary>
		/// Listen���J�n����
		/// </summary>
		/// <param name="host">�z�X�g��</param>
		/// <param name="portNum">�|�[�g�ԍ�</param>
		public void Listen(string host, int portNum, int backlog)
		{
			if (this._server == null)
				throw new ApplicationException("�j������Ă��܂��B");
			if (this.ServerState != ServerState.None)
				throw new ApplicationException("���ł�Listen���ł��B");

			this._socketEP = new IPEndPoint(
				Dns.Resolve(host).AddressList[0], portNum);
			this._server.Bind(this._socketEP);
				
			//Listen���J�n����
			this._server.Listen(backlog);
			this._serverState = ServerState.Listening;

			//�ڑ��v���{�s���J�n����
			this._server.BeginAccept(new AsyncCallback(this.AcceptCallback), null);
		}
		public void Listen(string host, int portNum)
		{
			this.Listen(host, portNum, 100);
		}

		/// <summary>
		/// �ڑ����̂��ׂẴN���C�A���g�Ƀ��b�Z�[�W�𑗐M����
		/// </summary>
		/// <param name="str">���M���镶����</param>
		public virtual void SendMessageToAllClients(string msg)
		{
			//CRLF���폜
			msg = msg.Replace("\r\n", "");

			this.SendToAllClients(msg);
		}

		/// <summary>
		/// �N���C�A���g�ɃG���[���b�Z�[�W�𑗐M����
		/// </summary>
		/// <param name="client">���M��̃N���C�A���g</param>
		/// <param name="msg">���M����G���[���b�Z�[�W</param>
		protected virtual void SendErrorMessage(TcpChatClient client, string msg)
		{
			client.SendMessage(msg);
		}

		/// <summary>
		/// �Ď��𒆎~�i���A�͕s�j
		/// </summary>
		public void StopListen()
		{
			lock (this)
			{
				if (this._server == null)
					return;
				this._server.Close();
				this._server = null;
				this._serverState = ServerState.Stopped;
			}

		}

		/// <summary>
		/// ����
		/// </summary>
		public void Close()
		{
			this.StopListen();
			this.CloseAllClients();
		}

		/// <summary>
		/// �ڑ����̃N���C�A���g�����
		/// </summary>
		public void CloseClient(TcpChatClient client)
		{
			this._acceptedClients.Remove(client);
			client.Close();
		}

		/// <summary>
		/// �ڑ����̂��ׂẴN���C�A���g�����
		/// </summary>
		public void CloseAllClients()
		{
			lock (this._acceptedClients.SyncRoot)
			{
				while (this._acceptedClients.Count > 0)
				{
					this.CloseClient((TcpChatClient) this._acceptedClients[0]);
				}
			}
		}

		/// <summary>
		/// �ڑ����̂��ׂẴN���C�A���g�ɕ�����𑗐M����
		/// </summary>
		/// <param name="str">���M���镶����</param>
		protected void SendToAllClients(string str)
		{
			lock (this._acceptedClients.SyncRoot)
			{
				for (int i = 0; i < this._acceptedClients.Count; i++)
				{
					((TcpChatClient) this._acceptedClients[i]).Send(str);
				}
			}
		}

		/// <summary>
		/// �T�[�o�[�Ŏg�p����N���C�A���g�N���X���쐬����
		/// </summary>
		/// <param name="soc">��ɂȂ�Socket</param>
		/// <returns>�N���C�A���g�N���X</returns>
		protected virtual TcpChatClient CreateChatClient(Socket soc)
		{
			return new TcpChatClient(soc);
		}

		//BeginAccept�̃R�[���o�b�N
		private void AcceptCallback(IAsyncResult ar)
		{
			//�ڑ��v�����󂯓����
			Socket soc = null;
			try
			{
				lock (this)
				{
					soc = this._server.EndAccept(ar);
				}
			}
			catch
			{
				this.Close();
				return;
			}

			//TcpChatClient�̍쐬
			TcpChatClient client = this.CreateChatClient(soc);
			//�ő吔�𒴂��Ă��Ȃ���
			if (this._acceptedClients.Count >= this.MaxClients)
			{
				client.Close();
			}
			else
			{
				//�R���N�V�����ɒǉ�
				this._acceptedClients.Add(client);
				//�C�x���g�n���h���̒ǉ�
				client.Disconnected += new EventHandler(client_Disconnected);
				client.ReceivedData += new ReceivedDataEventHandler(client_ReceivedData);
				//�C�x���g�𔭐�
				this.OnAcceptedClient(new ServerEventArgs(client));
				//�f�[�^��M�J�n
				if (!client.IsClosed)
				{
					client.StartReceive();
				}
			}

			//�ڑ��v���{�s���ĊJ����
			this._server.BeginAccept(new AsyncCallback(this.AcceptCallback), null);
		}

		#region �N���C�A���g�̃C�x���g�n���h��
		//�N���C�A���g���ؒf������
		private void client_Disconnected(object sender, EventArgs e)
		{
			//���X�g����폜����
			this._acceptedClients.Remove((TcpChatClient) sender);
			//�C�x���g�𔭐�
			this.OnDisconnectedClient(new ServerEventArgs((TcpChatClient) sender));
		}

		//�N���C�A���g����f�[�^����M������
		private void client_ReceivedData(object sender, ReceivedDataEventArgs e)
		{
			//�C�x���g�𔭐�
			this.OnReceivedData(new ReceivedDataEventArgs(
				(TcpChatClient) sender, e.ReceivedString));
		}
		#endregion
	}
}
