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
			Close();
		}
		#endregion

		#region �C�x���g
		/// <summary>
		/// �N���C�A���g���󂯓��ꂽ
		/// </summary>
		public event ServerEventHandler AcceptedClient;
		protected virtual void OnAcceptedClient(ServerEventArgs e)
		{
			if (AcceptedClient != null)
			{
				AcceptedClient(this, e);
			}
		}

		/// <summary>
		/// �N���C�A���g���f�[�^����M����
		/// </summary>
		public event ReceivedDataEventHandler ReceivedData;
		protected virtual void OnReceivedData(ReceivedDataEventArgs e)
		{
			if (ReceivedData != null)
			{
				ReceivedData(this, e);
			}
		}

		/// <summary>
		/// �N���C�A���g���ؒf����
		/// </summary>
		public event ServerEventHandler DisconnectedClient;
		protected virtual void OnDisconnectedClient(ServerEventArgs e)
		{
			if (DisconnectedClient != null)
			{
				DisconnectedClient(this, e);
			}
		}
		#endregion

		#region �v���p�e�B
		private Socket? _server;
		/// <summary>
		/// ��ɂȂ�Socket
		/// </summary>
		protected Socket Server
		{
			get
			{
				return _server;
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
				return _serverState;
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
				return _socketEP;
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
				return (TcpChatClient[]) _acceptedClients.ToArray(typeof(TcpChatClient));
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
				return _maxClients;
			}
			set
			{
				_maxClients = value;
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
			_maxClients = 100;
			_server = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);
			_acceptedClients =
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
			if (_server == null)	throw new ApplicationException("�j������Ă��܂��B");
			if (ServerState != ServerState.None)	throw new ApplicationException("���ł�Listen���ł��B");

            //_socketEP = new IPEndPoint(Dns.Resolve(host).AddressList[0], portNum); //IPAddress.Parse(host)
            _socketEP = new IPEndPoint(IPAddress.Parse(host), portNum); 
            _server.Bind(_socketEP);            

            //Listen���J�n����
            _server.Listen(backlog);
			_serverState = ServerState.Listening;

			//�ڑ��v���{�s���J�n����
			_server.BeginAccept(new AsyncCallback(AcceptCallback), null);
		}
		public void Listen(string host, int portNum)
		{
			Listen(host, portNum, 100);
		}

		/// <summary>
		/// �ڑ����̂��ׂẴN���C�A���g�Ƀ��b�Z�[�W�𑗐M����
		/// </summary>
		/// <param name="str">���M���镶����</param>
		public virtual void SendMessageToAllClients(string msg)
		{
			//CRLF���폜
			msg = msg.Replace("\r\n", "");

			SendToAllClients(msg);
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
				if (_server == null)
					return;
				_server.Close();
				_server = null;
				_serverState = ServerState.Stopped;
			}

		}

		/// <summary>
		/// ����
		/// </summary>
		public void Close()
		{
			StopListen();
			CloseAllClients();
		}

		/// <summary>
		/// �ڑ����̃N���C�A���g�����
		/// </summary>
		public void CloseClient(TcpChatClient client)
		{
			_acceptedClients.Remove(client);
			client.Close();
		}

		/// <summary>
		/// �ڑ����̂��ׂẴN���C�A���g�����
		/// </summary>
		public void CloseAllClients()
		{
			lock (_acceptedClients.SyncRoot)
			{
				while (_acceptedClients.Count > 0)
				{
					CloseClient((TcpChatClient) _acceptedClients[0]);
				}
			}
		}

		/// <summary>
		/// �ڑ����̂��ׂẴN���C�A���g�ɕ�����𑗐M����
		/// </summary>
		/// <param name="str">���M���镶����</param>
		protected void SendToAllClients(string str)
		{
			lock (_acceptedClients.SyncRoot)
			{
				for (int i = 0; i < _acceptedClients.Count; i++)
				{
					((TcpChatClient) _acceptedClients[i]).Send(str);
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
					if(_server != null)
					soc = _server.EndAccept(ar);
				}
			}
			catch
			{
				Close();
				return;
			}

			//TcpChatClient�̍쐬
			TcpChatClient client = CreateChatClient(soc);
			//�ő吔�𒴂��Ă��Ȃ���
			if (_acceptedClients.Count >= MaxClients)
			{
				client.Close();
			}
			else
			{
				//�R���N�V�����ɒǉ�
				_acceptedClients.Add(client);
				//�C�x���g�n���h���̒ǉ�
				client.Disconnected += new EventHandler(client_Disconnected);
				client.ReceivedData += new ReceivedDataEventHandler(client_ReceivedData);
				//�C�x���g�𔭐�
				OnAcceptedClient(new ServerEventArgs(client));
				//�f�[�^��M�J�n
				if (!client.IsClosed)
				{
					client.StartReceive();
				}
			}

			//�ڑ��v���{�s���ĊJ����
			_server.BeginAccept(new AsyncCallback(AcceptCallback), null);
		}

		#region �N���C�A���g�̃C�x���g�n���h��
		//�N���C�A���g���ؒf������
		private void client_Disconnected(object sender, EventArgs e)
		{
			//���X�g����폜����
			_acceptedClients.Remove((TcpChatClient) sender);
			//�C�x���g�𔭐�
			OnDisconnectedClient(new ServerEventArgs((TcpChatClient) sender));
		}

		//�N���C�A���g����f�[�^����M������
		private void client_ReceivedData(object sender, ReceivedDataEventArgs e)
		{
			//�C�x���g�𔭐�
			OnReceivedData(new ReceivedDataEventArgs(
				(TcpChatClient) sender, e.ReceivedString));
		}
		#endregion
	}
}
