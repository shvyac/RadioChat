using System;

namespace Radio.Net.Chat
{
    /// <summary>
    /// MemberEventArgs の概要の説明です。
    /// </summary>
    public class MemberEventArgs : EventArgs
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memName"></param>
        public MemberEventArgs(string memName)
        {
            _name = memName;
        }
    }
}
