using System;
using System.Net;
using System.Net.Sockets;

namespace Radio.Net.Chat
{
	/// <summary>
	/// RadioChatServerの機能を提供する
	/// </summary>
	public class RadioChatServer : TcpChatServer
	{
		#region イベント
		/// <summary>
		/// メンバがログインした
		/// </summary>
		public event ServerEventHandler LoggedinMember;
		private void OnLoggedinMember(ServerEventArgs e)
		{
			if (LoggedinMember != null)
			{
				LoggedinMember(this, e);
			}
		}

		/// <summary>
		/// メンバがログアウトした
		/// </summary>
		public event ServerEventHandler LoggedoutMember;
		private void OnLoggedoutMember(ServerEventArgs e)
		{
			if (LoggedoutMember != null)
			{
				LoggedoutMember(this, e);
			}
		}
		#endregion

		/// <summary>
		/// RadioChatServerのコンストラクタ
		/// </summary>
		public RadioChatServer() : base()
		{
		}

		public override void SendMessageToAllClients(string msg)
		{
			//CRLFを削除
			msg = msg.Replace("\r\n", "");

			SendToAllClients(ServerCommands.Message + " _HOST " + msg);
		}

		/// <summary>
		/// 名前からクライアントを探す
		/// </summary>
		/// <param name="nickName">探す名前</param>
		/// <returns>見つかった時はAcceptedChatClientオブジェクト</returns>
		public AcceptedChatClient FindMember(string nickName)
		{
			lock (_acceptedClients.SyncRoot)
			{
				foreach (AcceptedChatClient c in _acceptedClients)
				{
					if (c.Name == nickName)
						return c;
				}
			}

			return null;
		}

		//クライアントからのデータを受信した時
		protected override void OnReceivedData(ReceivedDataEventArgs e)
		{
			base.OnReceivedData(e);

			AcceptedChatClient client = (AcceptedChatClient) e.Client;

			//受信した文字列を分解する
			string[] cmds = e.ReceivedString.Split(new char[] {' '}, 2);

			//コマンドを調べる
			if (ClientCommands.Login == cmds[0])
			{
				//チャット参加コマンド
				if (client.LoginState != LoginState.Parted)
				{
					SendErrorMessage(client, "すでに参加しています。");
					return;
				}

				//名前が適当か
				string nickName = cmds[1];
				if (nickName.Length == 0 || nickName.IndexOf(' ') >= 0 ||
					nickName.StartsWith("_"))
				{
					SendErrorMessage(client, "名前が不正です。");
					return;
				}

				//同じ名前がないか調べる
				lock (_acceptedClients.SyncRoot)
				{
					foreach (AcceptedChatClient c in _acceptedClients)
					{
						if (nickName == c.Name)
						{
							SendErrorMessage(client, "同じ名前のメンバーがすでにログインしています。");
							return;
						}
					}

					//名前、状態の更新
					client.Name = nickName;
					client.LoginState = LoginState.Joined;
				}

				//イベント発生
				OnLoggedinMember(new ServerEventArgs(client));
				//クライアントに通知
				SendToAllClients(ServerCommands.JoinMember + " _HOST " + client.Name);
				//メンバリストを送る
				SendMembersList(client);
			}
			else if (ClientCommands.Logout == cmds[0])
			{
				//退室コマンド
				if (client.LoginState != LoginState.Joined)
				{
					SendErrorMessage(client, "チャットに参加していません。");
					return;
				}
				
				//状態の更新
				client.LoginState = LoginState.Parted;
				//イベント発生
				OnLoggedoutMember(new ServerEventArgs(client));
				//クライアントに通知
				SendToAllClients(ServerCommands.PartMember + " _HOST " + client.Name);
			}
			else if (ClientCommands.MembersList == cmds[0])
			{
				//メンバリスト要求コマンド
				if (client.LoginState != LoginState.Joined)
				{
					SendErrorMessage(client, "チャットに参加していません。");
					return;
				}

				//メンバリストを送る
				SendMembersList(client);
			}
			else if (ClientCommands.Message == cmds[0])
			{
				//メッセージ送信コマンド
				if (client.LoginState != LoginState.Joined)
				{
					SendErrorMessage(client, "チャットに参加していません。");
					return;
				}

				//クライアントにメッセージを送信
				SendToAllClients(ServerCommands.Message + " " + client.Name + " " + cmds[1]);
			}
			else if (ClientCommands.PrivateMessage == cmds[0])
			{
				//プライベートメッセージ送信コマンド
				if (client.LoginState != LoginState.Joined)
				{
					SendErrorMessage(client, "チャットに参加していません。");
					return;
				}

				string[] msgs = cmds[1].Split(new char[] {' '}, 2);

				//名前からクライアントを探す
				AcceptedChatClient toClient = FindMember(msgs[0]);
				if (toClient == null)
				{
					SendErrorMessage(client, "送信先の参加者が見つかりません。");
					return;
				}

				//クライアントにメッセージを送信
				toClient.Send(ServerCommands.PrivateMessage + " " + client.Name + " " + msgs[1]);
			}
		}

		//クライアントが切断した時
		protected override void OnDisconnectedClient(ServerEventArgs e)
		{
			AcceptedChatClient ac = (AcceptedChatClient) e.Client;
			//ログインしているときは、名前を覚えておく
			string clientName = "";
			if (ac.LoginState == LoginState.Joined)
			{
				clientName = ac.Name;
			}
			
			//基本クラスのOnDisconnectedClientを処理
			base.OnDisconnectedClient(e);

			//メンバのログアウトを通知する
			if (clientName.Length > 0)
				SendToAllClients(ServerCommands.PartMember + " _HOST " + clientName);
		}

		protected override TcpChatClient CreateChatClient(Socket soc)
		{
			return new AcceptedChatClient(soc);
		}

		/// <summary>
		/// エラーメッセージをクライアントに送信
		/// </summary>
		/// <param name="client">送信先のクライアント</param>
		/// <param name="msg">メッセージ</param>
		protected override void SendErrorMessage(TcpChatClient client, string msg)
		{
			client.Send(ServerCommands.Error + " _HOST " + msg);
		}

		/// <summary>
		/// メンバリストを送信する
		/// </summary>
		private void SendMembersList(TcpChatClient client)
		{
			string msg = "";
			lock (_acceptedClients.SyncRoot)
			{
				foreach (AcceptedChatClient c in _acceptedClients)
				{
					msg += c.Name + " ";
				}
			}
			msg = msg.TrimEnd(' ');

			client.Send(ServerCommands.MembersList + " _HOST " + msg);
		}
	}
}
