using System;

namespace Radio.Net.Chat
{
	/// <summary>
	/// MembersListEventArgs ‚ÌŠT—v‚Ìà–¾‚Å‚·B
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
