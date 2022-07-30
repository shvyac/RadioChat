using System;
using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	#region デリゲート
	/// <summary>
	/// 受信したデータを持つイベントを処理するメソッドを表す
	/// </summary>
	public delegate void ReceivedDataEventHandler(object sender, ReceivedDataEventArgs e);
	#endregion

	/// <summary>
	/// TCPチャットクライアントの基本的な機能を提供する
	/// </summary>
	public class TcpChatClient : IDisposable
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
		/// データを受信した
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
		/// サーバーに接続した
		/// </summary>
		public event EventHandler Connected;
		protected virtual void OnConnected(EventArgs e)
		{
			if (this.Connected != null)
			{
				this.Connected(this, e);
			}
		}

		/// <summary>
		/// サーバーから切断された、あるいは切断した
		/// </summary>
		public event EventHandler Disconnected;
		protected virtual void OnDisconnected(EventArgs e)
		{
			if (this.Disconnected != null)
			{
				this.Disconnected(this, e);
			}
		}
		#endregion

		#region プロパティ
		private System.Text.Encoding _encoding;
		/// <summary>
		/// 使用する文字コード
		/// </summary>
		protected System.Text.Encoding Encoding
		{
			get
			{
				return this._encoding;
			}
			set
			{
				_encoding = value;
			}
		}

		private Socket _socket;
		/// <summary>
		/// 基になるSocket
		/// </summary>
		protected Socket Client
		{
			get
			{
				return this._socket;
			}
		}

		private IPEndPoint _localEndPoint;
		/// <summary>
		/// ローカルエンドポイント
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get
			{
				return this._localEndPoint;
			}
		}

		private IPEndPoint _remoteEndPoint;
		/// <summary>
		/// ローカルエンドポイント
		/// </summary>
		public IPEndPoint RemoteEndPoint
		{
			get
			{
				return this._remoteEndPoint;
			}
		}

		/// <summary>
		/// 閉じているか
		/// </summary>
		public bool IsClosed
		{
			get
			{
				return (this._socket == null);
			}
		}

		private int _maxReceiveLength;
		/// <summary>
		/// 一回で受信できる最大バイト
		/// </summary>
		protected int MaxReceiveLenght
		{
			get
			{
				return _maxReceiveLength;
			}
			set
			{
				_maxReceiveLength = value;
			}
		}
		#endregion

		#region フィールド
		/// <summary>
		/// 受信したデータ
		/// </summary>
		protected System.IO.MemoryStream receivedBytes;
		private bool startedReceiving = false;
		#endregion

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public TcpChatClient()
		{
			this.Initialize();
			
			this._socket = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);
		}
		public TcpChatClient(Socket soc)
		{
			this.Initialize();
			
			this._socket = soc;
			this._localEndPoint = (IPEndPoint) soc.LocalEndPoint;
			this._remoteEndPoint = (IPEndPoint) soc.RemoteEndPoint;
		}

		private void Initialize()
		{
			this.Encoding = System.Text.Encoding.UTF8;
			this.MaxReceiveLenght = int.MaxValue;
		}

		/// <summary>
		/// サーバーに接続する
		/// </summary>
		/// <param name="host">ホスト名</param>
		/// <param name="port">ポート番号</param>
		public void Connect(string host, int port)
		{
			if (this.IsClosed)
				throw new ApplicationException("閉じています。");
			if (this._socket.Connected)
				throw new ApplicationException("すでに接続されています。");

            //接続する

            //System.Net.IPAddress ipAdd = System.Net.Dns.GetHostEntry(host).AddressList[0];
            System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(host);

            //IPEndPoint ipEnd = new IPEndPoint(Dns.Resolve(host).AddressList[0], port);
            IPEndPoint ipEnd = new IPEndPoint(ipAdd, port);

            this._socket.Connect(ipEnd);

			this._localEndPoint = (IPEndPoint) this._socket.LocalEndPoint;
			this._remoteEndPoint = (IPEndPoint) this._socket.RemoteEndPoint;

			//イベントを発生
			this.OnConnected(new EventArgs());

			//非同期データ受信を開始する
			this.StartReceive();
		}

		/// <summary>
		/// 切断する
		/// </summary>
		public void Close()
		{
			lock (this)
			{
				if (this.IsClosed)
					return;

				//閉じる
				this._socket.Shutdown(SocketShutdown.Both);
				this._socket.Close();
				this._socket = null;
				if (this.receivedBytes != null)
				{
					this.receivedBytes.Close();
					this.receivedBytes = null;
				}
			}
			//イベントを発生
			this.OnDisconnected(new EventArgs());
		}

		/// <summary>
		/// 文字列を送信する
		/// </summary>
		/// <param name="str">送信する文字列</param>
		public void Send(string str)
		{
			if (this.IsClosed)
				throw new ApplicationException("閉じています。");

			//文字列をByte型配列に変換
			byte[] sendBytes = this.Encoding.GetBytes(str + "\r\n");

			lock (this)
			{
				//データを送信する
				this._socket.Send(sendBytes);
			}
		}

		/// <summary>
		/// メッセージを送信する
		/// </summary>
		/// <param name="msg">送信するメッセージ</param>
		public virtual void SendMessage(string msg)
		{
			//CRLFを削除
			msg = msg.Replace("\r\n", "");

			this.Send(msg);
		}

		/// <summary>
		/// データの非同期受信を開始する
		/// </summary>
		public void StartReceive()
		{
			if (this.IsClosed)
				throw new ApplicationException("閉じています。");
			if (this.startedReceiving)
				throw new ApplicationException("StartReceiveがすでに呼び出されています。");

			//初期化
			byte[] receiveBuffer = new byte[1024];
			this.receivedBytes = new System.IO.MemoryStream();
			this.startedReceiving = true;

			//非同期受信を開始
			this._socket.BeginReceive(receiveBuffer,
				0, receiveBuffer.Length,
				SocketFlags.None, new AsyncCallback(ReceiveDataCallback),
				receiveBuffer);
		}

		//BeginReceiveのコールバック
		private void ReceiveDataCallback(IAsyncResult ar)
		{
			if (this._socket == null) return;
			int len = -1;
			//読み込んだ長さを取得
			try
			{
				lock (this)
				{
					if(this._socket != null)
					len = this._socket.EndReceive(ar);
				}
			}
			catch
			{
			}
			//切断されたか調べる
			if (len <= 0)
			{
				this.Close();
				return;
			}

			//受信したデータを取得する
			byte[] receiveBuffer = (byte[]) ar.AsyncState;

			//受信したデータを蓄積する
			this.receivedBytes.Write(receiveBuffer, 0, len);
			//最大値を超えた時は、接続を閉じる
			if (this.receivedBytes.Length > this.MaxReceiveLenght)
			{
				this.Close();
				return;
			}
			//最後まで受信したか調べる
			if (this.receivedBytes.Length >= 2)
			{
				this.receivedBytes.Seek(-2, System.IO.SeekOrigin.End);
				if (this.receivedBytes.ReadByte() == (int) '\r' &&
					this.receivedBytes.ReadByte() == (int) '\n')
				{
					//最後まで受信した時
					//受信したデータを文字列に変換
					string str = this.Encoding.GetString(
						this.receivedBytes.ToArray());
					this.receivedBytes.Close();
					//一行ずつに分解する
					int startPos = 0, endPos;
					while ((endPos = str.IndexOf("\r\n", startPos)) >=0 )
					{
						string line = str.Substring(startPos, endPos - startPos);
						startPos = endPos + 2;
						//イベントを発生
						this.OnReceivedData(new ReceivedDataEventArgs(this, line));
					}
					this.receivedBytes = new System.IO.MemoryStream();
				}
				else
				{
					this.receivedBytes.Seek(0, System.IO.SeekOrigin.End);
				}
			}

			lock (this)
			{
				//再び受信開始
				this._socket.BeginReceive(receiveBuffer,
					0, receiveBuffer.Length,
					SocketFlags.None, new AsyncCallback(ReceiveDataCallback)
					, receiveBuffer);
			}
		}
	}
}
