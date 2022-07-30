using System;
using System.Net;
using System.Net.Sockets;
using Radio.Net.Chat;

namespace Radio.Net.Chat
{
    #region デリゲート
    //イベントを処理するメソッドを表すデリゲート
    public delegate void ReceivedMessageEventHandler(object sender, ReceivedMessageEventArgs e);
	public delegate void MemberEventHandler(object sender, MemberEventArgs e);
	public delegate void MembersListEventHandler(object sender, MembersListEventArgs e);
	public delegate void ReceivedErrorEventHandler(object sender, ReceivedErrorEventArgs e);
	#endregion

	#region 列挙型
	/// <summary>
	/// チャットへの参加状態
	/// </summary>
	public enum LoginState
	{
		/// <summary>
		/// 参加を要求している
		/// </summary>
		WaitJoin,
		/// <summary>
		/// 参加している
		/// </summary>
		Joined,
		/// <summary>
		/// 退室を要求している
		/// </summary>
		WaitPart,
		/// <summary>
		/// 退室している
		/// </summary>
		Parted
	}
	#endregion

	/// <summary>
	/// Dobon Chat Clientの機能を提供する
	/// </summary>
	public class RadioChatClient : TcpChatClient
	{
		#region イベント
		/// <summary>
		/// メッセージを受け取った
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
		/// エラーを受け取った
		/// </summary>
		public event ReceivedErrorEventHandler ReceivedError;
		private void OnReceivedError(ReceivedErrorEventArgs e)
		{
			//参加要求が失敗した時
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
		/// メンバーが参加した
		/// </summary>
		public event MemberEventHandler JoinedMember;
		private void OnJoinedMember(MemberEventArgs e)
		{
			//自分の時は参加状態に
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
		/// メンバーが退室した
		/// </summary>
		public event MemberEventHandler PartedMember;
		private void OnPartedMember(MemberEventArgs e)
		{
			//自分の時は退室状態に
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
		/// メンバーリストが送られてきた
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
		
		#region プロパティ
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
		}
		#endregion

		/// <summary>
		/// コンストラクタ
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
		/// サーバーに接続する
		/// </summary>
		/// <param name="host">サーバーのホスト名</param>
		/// <param name="port">ポート番号</param>
		/// <param name="nickName">メンバー名</param>
		public void Connect(string host, int port, string nickName)
		{
			if (nickName.Length == 0)
				throw new ApplicationException("名前が指定されていません。");
			if (nickName.IndexOf(' ') >= 0)
				throw new ApplicationException("名前にスペース文字を入れることはできません。");
			if (nickName.StartsWith("_"))
				throw new ApplicationException("'_'で始まる名前は付けることができません。");

			//名前を保存する
			this._name = nickName;
			//接続する
			base.Connect(host, port);
		}

		/// <summary>
		/// メッセージを送信する
		/// </summary>
		/// <param name="msg">メッセージ</param>
		public override void SendMessage(string msg)
		{
			if (this.LoginState != LoginState.Joined)
				throw new ApplicationException("チャットに参加していません。");

			//CRLFを削除
			msg = msg.Replace("\r\n", "");

			this.Send(ClientCommands.Message + " " + msg);
		}

		/// <summary>
		/// プライベートメッセージを送信する
		/// </summary>
		/// <param name="msg">メッセージ</param>
		/// <param name="to">メッセージを送る相手の名前</param>
		public void SendPrivateMessage(string msg, string to)
		{
			if (this.LoginState != LoginState.Joined)
				throw new ApplicationException("チャットに参加していません。");

			//CRLFを削除
			msg = msg.Replace("\r\n", "");

			this.Send(ClientCommands.PrivateMessage + " " + to + " " + msg);
		}

		//データを受信した時
		protected override void OnReceivedData(ReceivedDataEventArgs e)
		{
			base.OnReceivedData(e);

			RadioChatClient client = (RadioChatClient) e.Client;

			//受信した文字列を分解する
			string[] cmds = e.ReceivedString.Split(new char[] {' '}, 3);

			//コマンドを解釈する
			if (ServerCommands.Error == cmds[0])
			{
				//エラーコマンド
				this.OnReceivedError(new ReceivedErrorEventArgs(cmds[2]));
			}
			else if (ServerCommands.Message == cmds[0])
			{
				//メッセージコマンド
				this.OnReceivedMessage(
					new ReceivedMessageEventArgs(cmds[1], cmds[2]));
			}
			else if (ServerCommands.PrivateMessage == cmds[0])
			{
				//プライベートメッセージコマンド
				this.OnReceivedMessage(
					new ReceivedMessageEventArgs(cmds[1], cmds[2], true));
			}
			else if (ServerCommands.JoinMember == cmds[0])
			{
				//メンバー参加コマンド
				this.OnJoinedMember(new MemberEventArgs(cmds[2]));
			}
			else if (ServerCommands.PartMember == cmds[0])
			{
				//メンバー退室コマンド
				this.OnPartedMember(new MemberEventArgs(cmds[2]));
			}
			else if (ServerCommands.MembersList == cmds[0])
			{
				//メンバーリストコマンド
				this.OnUpdatedMembers(new MembersListEventArgs(cmds[2].Split(' ')));
			}
		}

		protected override void OnConnected(EventArgs e)
		{
			base.OnConnected(e);

			//チャットへの参加を要求する
			this._loginState = LoginState.WaitJoin;
			string line = ClientCommands.Login + " " + this._name;
			this.Send(line);
		}

	}
}
