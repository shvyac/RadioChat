using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// RadioChatServer�����M����R�}���h
	/// </summary>
	public class ServerCommands
	{
		/// <summary>
		/// ���b�Z�[�W�𑗂�
		/// </summary>
		public static string Message
		{
			get
			{
				return "MSG";
			}
		}
		/// <summary>
		/// �v���C�x�[�g���b�Z�[�W�𑗂�
		/// </summary>
		public static string PrivateMessage
		{
			get
			{
				return "PRIVMSG";
			}
		}
		/// <summary>
		/// �����o�[���X�g�𑗂�
		/// </summary>
		public static string MembersList
		{
			get
			{
				return "NAMES";
			}
		}
		/// <summary>
		/// �Q���҂����邱�Ƃ�ʒm����
		/// </summary>
		public static string JoinMember
		{
			get
			{
				return "JOIN";
			}
		}
		/// <summary>
		/// �ގ��҂����邱�Ƃ�ʒm����
		/// </summary>
		public static string PartMember
		{
			get
			{
				return "PART";
			}
		}
		/// <summary>
		/// �G���[���b�Z�[�W�𑗂�
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
