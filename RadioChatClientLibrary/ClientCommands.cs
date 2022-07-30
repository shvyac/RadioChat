using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// Dobon Chat Clientが送信するコマンド
	/// </summary>
	public class ClientCommands
	{
		/// <summary>
		/// メッセージを送る
		/// </summary>
		public static string Message
		{
			get
			{
				return "MSG";
			}
		}
		/// <summary>
		/// プライベートメッセージを送る
		/// </summary>
		public static string PrivateMessage
		{
			get
			{
				return "PRIVMSG";
			}
		}
		/// <summary>
		/// メンバーリストの送信を要求する
		/// </summary>
		public static string MembersList
		{
			get
			{
				return "NAMES";
			}
		}
		/// <summary>
		/// 参加を要求する
		/// </summary>
		public static string Login
		{
			get
			{
				return "JOIN";
			}
		}
		/// <summary>
		/// 退室する
		/// </summary>
		public static string Logout
		{
			get
			{
				return "PART";
			}
		}
	}
}
