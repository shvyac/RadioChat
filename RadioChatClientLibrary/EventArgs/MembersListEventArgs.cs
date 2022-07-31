using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// MembersListEventArgs �̊T�v�̐����ł��B
	/// </summary>
	public class MembersListEventArgs : EventArgs
	{
		private string[] _members;
		public string[] Members
		{
			get
			{
				return _members;
			}
		}

		public MembersListEventArgs(string[] mems)
		{
			_members = mems;
		}
	}
}
