using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Radio.Net.Chat;

namespace RadioChatClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private System.ComponentModel.Container components = null;

        private delegate void PrintStringInvoker(string str, Color col);

        private Radio.Net.Chat.RadioChatClient client;

        private System.Collections.Specialized.StringCollection membersList;
        public void ShowConnectDialog()
        {
            ConnectForm dlg = new ConnectForm();
            Nullable<bool> dialogResult = dlg.ShowDialog();
            if (dialogResult == true )
            {
                Connect(dlg.HostName, dlg.Port, dlg.NickName);
            }

            logTextBox.AppendText(dlg.HostName + " " + dlg.Port + " " + dlg.NickName);
            
        }
        public void Connect(string host, int port, string nick)
        {
            if (this.client != null)
            {
                MessageBox.Show("すでに接続しています。");
                return;
            }

            Radio.Net.Chat.RadioChatClient c = new Radio.Net.Chat.RadioChatClient();
            //イベントハンドラの追加
            c.Connected += new EventHandler(client_Connected);
            c.Disconnected += new EventHandler(client_Disconnected);
            c.ReceivedData += new ReceivedDataEventHandler(client_ReceivedData);
            c.JoinedMember += new MemberEventHandler(client_JoinedMember);
            c.PartedMember += new MemberEventHandler(client_PartedMember);
            c.ReceivedMessage += new ReceivedMessageEventHandler(client_ReceivedMessage);
            c.UpdatedMembers += new MembersListEventHandler(client_UpdatedMembers);
            c.ReceivedError += new ReceivedErrorEventHandler(client_ReceivedError);

            try
            {
                //接続する
                c.Connect(host, port, nick);
            }
            catch (Exception ex)
            {
                c.Close();
                MessageBox.Show(this, "接続に失敗しました。\n(" + ex.Message + ")","エラー",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        public void Send()
        {
            this.Send(false);
        }

        public void Send(bool privMsg)
        {
            if (this.client == null)
            {
                MessageBox.Show(this,"接続していません。","エラー",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.client.LoginState != LoginState.Joined)
            {
                MessageBox.Show(this,"チャットに参加していません。","エラー",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (sendTextBox.Text.Length <= 0)
            {
                MessageBox.Show(this,"送信する文字列を入力してください。","エラー",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //プライベートメッセージの時
            string to = "";
            if (privMsg)
            {
                if (memberListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(this,"送信先のメンバーが選択されていません。","エラー",MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int toIndex = memberListView.SelectedIndex;
                to = members[toIndex].Name;

                if (this.client.Name == to)
                {
                    MessageBox.Show(this,"自分自身にプライベートメッセージは送れません。","エラー",MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                //メッセージを送信
                if (!privMsg)
                    this.client.SendMessage(sendTextBox.Text);
                else
                    this.client.SendPrivateMessage(sendTextBox.Text, to);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,"送信に失敗しました。\n(" + ex.Message + ")","エラー",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.sendTextBox.Text = "";
            this.sendTextBox.Focus();
        }

        /// <summary>
        /// ログに文字列を一行追加する
        /// </summary>
        /// <param name="str"></param>
        /// <param name="col"></param>
        public void AddLog(string str, Color col)
        {
            Action<string, Color> action2;
            action2 = PrivateAddLog;

            //if (!Dispatcher.CheckAccess()) action2(str, col);
            /*else*/ Dispatcher.Invoke(new Action<String, Color>(PrivateAddLog), str, col);
        }
        public void PrivateAddLog(string str, Color col)
        {
            string addText = DateTime.Now.ToLongTimeString() + " : " + str + "\n";   
            logTextBox.AppendText(addText);

            TextPointer caretPos = logTextBox.CaretPosition;
            caretPos = caretPos.DocumentEnd;
            logTextBox.CaretPosition = caretPos;

            sendTextBox.Focus();
        }

        /// <summary>
        /// メンバーリストを更新する
        /// </summary>
        public void UpdateMembersList()
        {
            //if (this.InvokeRequired)
            //    this.Invoke(new MethodInvoker(PrivateUpdateMembersList),
            //        new object[] { });
            //else
            //    this.PrivateUpdateMembersList();


            //if (!Dispatcher.CheckAccess()) PrivateUpdateMembersList();
            /*else*/ Dispatcher.Invoke(new Action(PrivateUpdateMembersList));
        }

        ObservableCollection<Member> members = new ObservableCollection<Member>();
        private void PrivateUpdateMembersList()
        {
            //this.memberListView.Items.Clear();
            members.Clear();
            foreach (string nick in this.membersList)
            {
                ListViewItem item = new ListViewItem();
                if (this.client.Name == nick)
                {
                    item.Background = Brushes.Red;
                    item.Name = nick;

                    members.Add(new Member() { Name = nick });
                }                
                memberListView.ItemsSource = members;

                //this.memberListView.Items.Add(item);
            }
        }

        /// <summary>
        /// 接続状態にする
        /// </summary>
        public void SetToConnected()
        {
            //if (this.InvokeRequired)
            //    this.Invoke(new MethodInvoker(PrivateSetToConnected),
            //        new object[] { });
            //else
            //    this.PrivateSetToConnected();

            if (!Dispatcher.CheckAccess()) PrivateSetToConnected();
            else Dispatcher.Invoke(new Action(PrivateSetToConnected));
        }
        private void PrivateSetToConnected()
        {
            this.sendButton.IsEnabled = true;
            MenuItemConnect.IsEnabled = false;      //this.menuConnect.Enabled = false;
            MenuItemDisconnect.IsEnabled = true;    //this.menuDisconnect.Enabled = true;
            MenuItemSendMessage.IsEnabled = true;   //this.menuSendMessage.Enabled = true;
            MenuItemSendPrivateMessage.IsEnabled = true;    //this.menuSendPrivateMessage.Enabled = true;
            //this.Title = Application.ProductName + " - 接続中(" +   this.client.RemoteEndPoint.ToString() + ")";
            this.Title = Application.Current.MainWindow.GetType().Assembly.ToString() + " - 接続中(" + this.client.RemoteEndPoint.ToString() + ")";            
        }

        //切断状態にする
        public void SetToDisconnected()
        {
            //if (this.InvokeRequired)
            //    this.Invoke(new MethodInvoker(PrivateSetToDisconnected),new object[] { });
            //else
            //    this.PrivateSetToDisconnected();

            if (!Dispatcher.CheckAccess()) PrivateSetToDisconnected();
            else Dispatcher.Invoke(new Action(PrivateSetToDisconnected));
        }
        private void PrivateSetToDisconnected()
        {
            this.sendButton.IsEnabled = false;
            this.MenuItemConnect.IsEnabled = true;
            this.MenuItemDisconnect.IsEnabled = false;
            this.MenuItemSendMessage.IsEnabled = false;
            this.MenuItemSendPrivateMessage.IsEnabled = false;
            this.Title = Application.Current.MainWindow.GetType().Assembly.ToString();
            this.client = null;
        }

        //閉じる
        private void ClientForm_Closed(object sender, System.EventArgs e)
        {
            if (this.client != null)
                this.client.Close();
        }

        //接続する

        //切断する

        //メッセージを送信する

        //終了する
        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItemConnect_Click(object sender, RoutedEventArgs e)
        {
            this.ShowConnectDialog();
        }

        private void MenuItemDisconnect_Click(object sender, RoutedEventArgs e)
        {
            this.client.Close();
        }

        private void MenuItemSendMessage_Click(object sender, RoutedEventArgs e)
        {
            this.Send();
        }
        //プライベートメッセージを送信する
        private void MenuItemSendPrivateMessage_Click(object sender, RoutedEventArgs e)
        {
            this.Send(true);
        }

        //データを受信した
        private void client_ReceivedData(object sender, ReceivedDataEventArgs e)
        {
            this.AddLog(e.ReceivedString, Colors.LightGray);
        }

        //サーバーに接続した
        private void client_Connected(object sender, EventArgs e)
        {
            this.client = (Radio.Net.Chat.RadioChatClient)sender;
            //this.Invoke(new MethodInvoker(this.SetToConnected));
            if (Dispatcher.CheckAccess()) SetToConnected();
            else Dispatcher.Invoke(new Action(SetToConnected));

            this.AddLog("サーバーに接続しました。",
                Colors.Blue);
        }

        //切断した
        private void client_Disconnected(object sender, EventArgs e)
        {
            this.client = null;
            //this.Invoke(new MethodInvoker(this.SetToDisconnected));
            if (Dispatcher.CheckAccess())   SetToDisconnected();
            else    Dispatcher.Invoke(new Action(SetToDisconnected));

            this.AddLog("サーバーから切断しました。",
                Colors.Blue);
            if (this.membersList != null)
            {
                this.membersList.Clear();
                this.UpdateMembersList();
            }
        }

        //メンバーが参加した
        private void client_JoinedMember(object sender, MemberEventArgs e)
        {
            if (this.client.Name == e.Name)
            {
                //自分がログインしたとき
                this.membersList = new System.Collections.Specialized.StringCollection();
                this.AddLog("チャットへの参加に成功しました。",
                    Colors.Blue);
            }
            else
            {
                //誰かがログインしたとき
                this.AddLog(string.Format("[{0}]さんが参加しました。", e.Name),
                    Colors.Blue);
            }

            //メンバーリストを更新
            this.membersList.Add(e.Name);
            this.UpdateMembersList();
        }

        //メンバーが退室した
        private void client_PartedMember(object sender, MemberEventArgs e)
        {
            if (this.client.Name == e.Name)
            {
                //自分がログアウトしたとき
                this.membersList.Clear();
                this.AddLog("退室しました。",
                    Colors.Blue);
            }
            else
            {
                //誰かがログアウトしたとき
                this.membersList.Remove(e.Name);
                this.AddLog(string.Format("[{0}]さんが退室しました。", e.Name),
                    Colors.Blue);
            }

            //メンバーリストを更新
            this.UpdateMembersList();
        }

        //メッセージを受信した
        private void client_ReceivedMessage(object sender, ReceivedMessageEventArgs e)
        {
            if (!e.PrivateMessage)
                this.AddLog(e.From + " > " + e.Message, Colors.Black);
            else
                this.AddLog(e.From + " > " + e.Message, Colors.Brown);
        }

        //メンバーリストを受信した
        private void client_UpdatedMembers(object sender, MembersListEventArgs e)
        {
            //メンバーリストを更新
            this.membersList.Clear();
            this.membersList.AddRange(e.Members);
            this.UpdateMembersList();
        }

        //エラーを受信した
        private void client_ReceivedError(object sender, ReceivedErrorEventArgs e)
        {
            this.AddLog("エラー : " + e.ErrorMessage,  Colors.Red);

            if (this.client.LoginState != LoginState.Joined)
            {
                //エラーで閉じる
                this.client.Close();
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            this.Send();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Initialized(object sender, EventArgs e)
        {

        }
    }
}
