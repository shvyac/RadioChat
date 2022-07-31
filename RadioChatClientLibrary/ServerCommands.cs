using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// RadioChatServerが送信するコマンド
	/// </summary>
	public class ServerCommands
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
		/// メンバーリストを送る
		/// </summary>
		public static string MembersList
		{
			get
			{
				return "NAMES";
			}
		}
		/// <summary>
		/// 参加者がいることを通知する
		/// </summary>
		public static string JoinMember
		{
			get
			{
				return "JOIN";
			}
		}
		/// <summary>
		/// 退室者がいることを通知する
		/// </summary>
		public static string PartMember
		{
			get
			{
				return "PART";
			}
		}
		/// <summary>
		/// エラーメッセージを送る
		/// </summary>
		public static string Error
		{
			get
			{
				return "ERR";
			}
		}
	}
}
