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
				return this._members;
			}
		}

		public MembersListEventArgs(string[] mems)
		{
			this._members = mems;
		}
	}
}
