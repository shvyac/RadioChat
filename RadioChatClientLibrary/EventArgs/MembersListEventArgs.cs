using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// MembersListEventArgs の概要の説明です。
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
