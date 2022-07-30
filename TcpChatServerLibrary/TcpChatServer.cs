using System;
using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	#region デリゲート
	/// <summary>
	/// クライアント情報を持つイベントを処理するメソッドを表す
	/// </summary>
	public delegate void ServerEventHandler(object sender, ServerEventArgs e);
	#endregion

	#region 列挙型
	/// <summary>
	/// サーバーの状態
	/// </summary>
	public enum ServerState
	{
		None,
		Listening,
		Stopped
	}
	#endregion

	/// <summary>
	/// TCPチャットサーバーの基本的な機能を提供する
	/// </summary>
	public class TcpChatServer : IDisposable
	{
		#region IDisposable メンバ
		/// <summary>
		/// 破棄する
		/// </summary>
		public virtual void Dispose()
		{
			this.Close();
		}
		#endregion

		#region イベント
		/// <summary>
		/// クライアントを受け入れた
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
		/// クライアントがデータを受信した
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
		/// クライアントが切断した
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

		#region プロパティ
		private Socket _server;
		/// <summary>
		/// 基になるSocket
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
		/// 状態
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
		/// ローカルエンドポイント
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
		/// 接続中のクライアント
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
		/// 同時接続を許可するクライアント数
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

		#region フィールド
		#endregion

		/// <summary>
		/// TcpChatServerのコンストラクタ
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
		/// Listenを開始する
		/// </summary>
		/// <param name="host">ホスト名</param>
		/// <param name="portNum">ポート番号</param>
		public void Listen(string host, int portNum, int backlog)
		{
			if (this._server == null)
				throw new ApplicationException("破棄されています。");
			if (this.ServerState != ServerState.None)
				throw new ApplicationException("すでにListen中です。");

			this._socketEP = new IPEndPoint(
				Dns.Resolve(host).AddressList[0], portNum);
			this._server.Bind(this._socketEP);
				
			//Listenを開始する
			this._server.Listen(backlog);
			this._serverState = ServerState.Listening;

			//接続要求施行を開始する
			this._server.BeginAccept(new AsyncCallback(this.AcceptCallback), null);
		}
		public void Listen(string host, int portNum)
		{
			this.Listen(host, portNum, 100);
		}

		/// <summary>
		/// 接続中のすべてのクライアントにメッセージを送信する
		/// </summary>
		/// <param name="str">送信する文字列</param>
		public virtual void SendMessageToAllClients(string msg)
		{
			//CRLFを削除
			msg = msg.Replace("\r\n", "");

			this.SendToAllClients(msg);
		}

		/// <summary>
		/// クライアントにエラーメッセージを送信する
		/// </summary>
		/// <param name="client">送信先のクライアント</param>
		/// <param name="msg">送信するエラーメッセージ</param>
		protected virtual void SendErrorMessage(TcpChatClient client, string msg)
		{
			client.SendMessage(msg);
		}

		/// <summary>
		/// 監視を中止（復帰は不可）
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
		/// 閉じる
		/// </summary>
		public void Close()
		{
			this.StopListen();
			this.CloseAllClients();
		}

		/// <summary>
		/// 接続中のクライアントを閉じる
		/// </summary>
		public void CloseClient(TcpChatClient client)
		{
			this._acceptedClients.Remove(client);
			client.Close();
		}

		/// <summary>
		/// 接続中のすべてのクライアントを閉じる
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
		/// 接続中のすべてのクライアントに文字列を送信する
		/// </summary>
		/// <param name="str">送信する文字列</param>
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
		/// サーバーで使用するクライアントクラスを作成する
		/// </summary>
		/// <param name="soc">基になるSocket</param>
		/// <returns>クライアントクラス</returns>
		protected virtual TcpChatClient CreateChatClient(Socket soc)
		{
			return new TcpChatClient(soc);
		}

		//BeginAcceptのコールバック
		private void AcceptCallback(IAsyncResult ar)
		{
			//接続要求を受け入れる
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

			//TcpChatClientの作成
			TcpChatClient client = this.CreateChatClient(soc);
			//最大数を超えていないか
			if (this._acceptedClients.Count >= this.MaxClients)
			{
				client.Close();
			}
			else
			{
				//コレクションに追加
				this._acceptedClients.Add(client);
				//イベントハンドラの追加
				client.Disconnected += new EventHandler(client_Disconnected);
				client.ReceivedData += new ReceivedDataEventHandler(client_ReceivedData);
				//イベントを発生
				this.OnAcceptedClient(new ServerEventArgs(client));
				//データ受信開始
				if (!client.IsClosed)
				{
					client.StartReceive();
				}
			}

			//接続要求施行を再開する
			this._server.BeginAccept(new AsyncCallback(this.AcceptCallback), null);
		}

		#region クライアントのイベントハンドラ
		//クライアントが切断した時
		private void client_Disconnected(object sender, EventArgs e)
		{
			//リストから削除する
			this._acceptedClients.Remove((TcpChatClient) sender);
			//イベントを発生
			this.OnDisconnectedClient(new ServerEventArgs((TcpChatClient) sender));
		}

		//クライアントからデータを受信した時
		private void client_ReceivedData(object sender, ReceivedDataEventArgs e)
		{
			//イベントを発生
			this.OnReceivedData(new ReceivedDataEventArgs(
				(TcpChatClient) sender, e.ReceivedString));
		}
		#endregion
	}
}
