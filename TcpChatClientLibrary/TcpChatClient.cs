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
			Close();
		}
		#endregion

		#region イベント
		/// <summary>
		/// データを受信した
		/// </summary>
		public event ReceivedDataEventHandler? ReceivedData;
		protected virtual void OnReceivedData(ReceivedDataEventArgs e)
		{
			if (ReceivedData != null)
			{
				ReceivedData(this, e);
			}
		}

		/// <summary>
		/// サーバーに接続した
		/// </summary>
		public event EventHandler? Connected;
		protected virtual void OnConnected(EventArgs e)
		{
			if (Connected != null)
			{
				Connected(this, e);
			}
		}

		/// <summary>
		/// サーバーから切断された、あるいは切断した
		/// </summary>
		public event EventHandler? Disconnected;
		protected virtual void OnDisconnected(EventArgs e)
		{
			if (Disconnected != null)
			{
				Disconnected(this, e);
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
				return _encoding;
			}
			set
			{
				_encoding = value;
			}
		}

		private Socket? _socket;
		/// <summary>
		/// 基になるSocket
		/// </summary>
		protected Socket Client
		{
			get
			{
				return _socket;
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
				return _localEndPoint;
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
				return _remoteEndPoint;
			}
		}

		/// <summary>
		/// 閉じているか
		/// </summary>
		public bool IsClosed
		{
			get
			{
				return (_socket == null);
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
		protected System.IO.MemoryStream? receivedBytes;
		private bool startedReceiving = false;
		#endregion

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public TcpChatClient()
		{
			Initialize();
			
			_socket = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);
		}
		public TcpChatClient(Socket soc)
		{
			Initialize();
			
			_socket = soc;
			_localEndPoint = (IPEndPoint) soc.LocalEndPoint;
			_remoteEndPoint = (IPEndPoint) soc.RemoteEndPoint;
		}

		private void Initialize()
		{
			Encoding = System.Text.Encoding.UTF8;
			MaxReceiveLenght = int.MaxValue;
		}

		/// <summary>
		/// サーバーに接続する
		/// </summary>
		/// <param name="host">ホスト名</param>
		/// <param name="port">ポート番号</param>
		public void Connect(string host, int port)
		{
			if (IsClosed)
				throw new ApplicationException("閉じています。");
			if (_socket.Connected)	throw new ApplicationException("すでに接続されています。");

            //接続する

            //System.Net.IPAddress ipAdd = System.Net.Dns.GetHostEntry(host).AddressList[0];
            System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(host);

            //IPEndPoint ipEnd = new IPEndPoint(Dns.Resolve(host).AddressList[0], port);
            IPEndPoint? ipEnd = new IPEndPoint(ipAdd, port);

            _socket.Connect(ipEnd);

			_localEndPoint = (IPEndPoint) _socket.LocalEndPoint;
			_remoteEndPoint = (IPEndPoint) _socket.RemoteEndPoint;

			//イベントを発生
			OnConnected(new EventArgs());

			//非同期データ受信を開始する
			StartReceive();
		}

		/// <summary>
		/// 切断する
		/// </summary>
		public void Close()
		{
			lock (this)
			{
				if (IsClosed)
					return;

				//閉じる
				_socket.Shutdown(SocketShutdown.Both);
				_socket.Close();
				_socket = null;
				if (receivedBytes != null)
				{
					receivedBytes.Close();
					receivedBytes = null;
				}
			}
			//イベントを発生
			OnDisconnected(new EventArgs());
		}

		/// <summary>
		/// 文字列を送信する
		/// </summary>
		/// <param name="str">送信する文字列</param>
		public void Send(string str)
		{
			if (IsClosed)
				throw new ApplicationException("閉じています。");

			//文字列をByte型配列に変換
			byte[] sendBytes = Encoding.GetBytes(str + "\r\n");

			lock (this)
			{
				//データを送信する
				_socket.Send(sendBytes);
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

			Send(msg);
		}

		/// <summary>
		/// データの非同期受信を開始する
		/// </summary>
		public void StartReceive()
		{
			if (IsClosed)
				throw new ApplicationException("閉じています。");
			if (startedReceiving)
				throw new ApplicationException("StartReceiveがすでに呼び出されています。");

			//初期化
			byte[] receiveBuffer = new byte[1024];
			receivedBytes = new System.IO.MemoryStream();
			startedReceiving = true;

			//非同期受信を開始
			_socket.BeginReceive(receiveBuffer,
				0, receiveBuffer.Length,
				SocketFlags.None, new AsyncCallback(ReceiveDataCallback),
				receiveBuffer);
		}

		//BeginReceiveのコールバック
		private void ReceiveDataCallback(IAsyncResult ar)
		{
			if (_socket == null) return;
			int len = -1;
			//読み込んだ長さを取得
			try
			{
				lock (this)
				{
					if(_socket != null)
					len = _socket.EndReceive(ar);
				}
			}
			catch
			{
			}
			//切断されたか調べる
			if (len <= 0)
			{
				Close();
				return;
			}

			//受信したデータを取得する
			byte[] receiveBuffer = (byte[]) ar.AsyncState;

			//受信したデータを蓄積する
			receivedBytes.Write(receiveBuffer, 0, len);
			//最大値を超えた時は、接続を閉じる
			if (receivedBytes.Length > MaxReceiveLenght)
			{
				Close();
				return;
			}
			//最後まで受信したか調べる
			if (receivedBytes.Length >= 2)
			{
				receivedBytes.Seek(-2, System.IO.SeekOrigin.End);
				if (receivedBytes.ReadByte() == (int) '\r' &&
					receivedBytes.ReadByte() == (int) '\n')
				{
					//最後まで受信した時
					//受信したデータを文字列に変換
					string str = Encoding.GetString(
						receivedBytes.ToArray());
					receivedBytes.Close();
					//一行ずつに分解する
					int startPos = 0, endPos;
					while ((endPos = str.IndexOf("\r\n", startPos)) >=0 )
					{
						string line = str.Substring(startPos, endPos - startPos);
						startPos = endPos + 2;
						//イベントを発生
						OnReceivedData(new ReceivedDataEventArgs(this, line));
					}
					receivedBytes = new System.IO.MemoryStream();
				}
				else
				{
					receivedBytes.Seek(0, System.IO.SeekOrigin.End);
				}
			}

			lock (this)
			{
				//再び受信開始
				_ = _socket.BeginReceive(receiveBuffer,
					0, receiveBuffer.Length,
					SocketFlags.None, new AsyncCallback(ReceiveDataCallback)
					, receiveBuffer);
			}
		}
	}
}
