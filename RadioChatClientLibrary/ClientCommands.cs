using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// Dobon Chat Client�����M����R�}���h
	/// </summary>
	public class ClientCommands
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
		/// �����o�[���X�g�̑��M��v������
		/// </summary>
		public static string MembersList
		{
			get
			{
				return "NAMES";
			}
		}
		/// <summary>
		/// �Q����v������
		/// </summary>
		public static string Login
		{
			get
			{
				return "JOIN";
			}
		}
		/// <summary>
		/// �ގ�����
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
