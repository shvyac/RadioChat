using System;
using System.Collections.ObjectModel;
using System.Net;
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

        //private delegate void PrintStringInvoker(string str, Color col);

        private Radio.Net.Chat.RadioChatClient? client;

        private System.Collections.Specialized.StringCollection membersList;
        public void ShowConnectDialog()
        {
            ConnectForm? dlg = new ConnectForm();
            Nullable<bool> dialogResult = dlg.ShowDialog();
            if (dialogResult == true)
            {
                Connect(dlg.HostName, dlg.Port, dlg.NickName);
            }

            TextBoxMSG.AppendText(dlg.HostName + " " + dlg.Port + " " + dlg.NickName);

        }
        public void Connect(string host, int port, string nick)
        {
            if (client != null)
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
                MessageBox.Show(this, "接続に失敗しました。\n(" + ex.Message + ")", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        public void Send()
        {
            Send(false);
        }

        public void Send(bool privMsg)
        {
            if (client == null)
            {
                MessageBox.Show(this, "接続していません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (client.LoginState != LoginState.Joined)
            {
                MessageBox.Show(this, "チャットに参加していません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (sendTextBox.Text.Length <= 0)
            {
                MessageBox.Show(this, "送信する文字列を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //プライベートメッセージの時
            string to = "";
            if (privMsg)
            {
                if (memberListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(this, "送信先のメンバーが選択されていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int toIndex = memberListView.SelectedIndex;
                to = members[toIndex].Name;

                if (client.Name == to)
                {
                    MessageBox.Show(this, "自分自身にプライベートメッセージは送れません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                //メッセージを送信
                if (!privMsg)
                    client.SendMessage(sendTextBox.Text);
                else
                    client.SendPrivateMessage(sendTextBox.Text, to);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "送信に失敗しました。\n(" + ex.Message + ")", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            sendTextBox.Text = "";
            sendTextBox.Focus();
        }

        public void AddLog(string str)
        {
            Dispatcher.Invoke(new Action<String>(PrivateAddLog), str);
        }
        public void PrivateAddLog(string str)
        {
            string addText = DateTime.Now.ToLongTimeString() + " : " + str + "\n";
            TextBoxMSG.AppendText(addText);
            TextBoxMSG.ScrollToEnd();
        }

        /// <summary>
        /// メンバーリストを更新する
        /// </summary>
        public void UpdateMembersList()
        {
            Dispatcher.Invoke(new Action(PrivateUpdateMembersList));
        }

        ObservableCollection<Member> members = new ObservableCollection<Member>();
        private void PrivateUpdateMembersList()
        {
            //memberListView.Items.Clear();
            members.Clear();
            foreach (string nick in membersList)
            {
                ListViewItem? item = new ListViewItem();
                if (client.Name == nick)
                {
                    item.Background = Brushes.Red;
                    item.Name = nick;

                    members.Add(new Member() { Name = nick });
                }
                memberListView.ItemsSource = members;

                //memberListView.Items.Add(item);
            }
        }

        /// <summary>
        /// 接続状態にする
        /// </summary>
        public void SetToConnected()
        {
            Dispatcher.Invoke(new Action(PrivateSetToConnected));
        }
        private void PrivateSetToConnected()
        {
            sendButton.IsEnabled = true;
            MenuItemConnect.IsEnabled = false;
            MenuItemDisconnect.IsEnabled = true;
            MenuItemSendMessage.IsEnabled = true;
            MenuItemSendPrivateMessage.IsEnabled = true;
            Title = Application.Current.MainWindow.GetType().Assembly.ToString() + " - 接続中(" + client.RemoteEndPoint.ToString() + ")";
        }

        /// <summary>
        /// 切断状態にする
        /// </summary>
        public void SetToDisconnected()
        {
            Dispatcher.Invoke(new Action(PrivateSetToDisconnected));
        }
        private void PrivateSetToDisconnected()
        {
            sendButton.IsEnabled = false;
            MenuItemConnect.IsEnabled = true;
            MenuItemDisconnect.IsEnabled = false;
            MenuItemSendMessage.IsEnabled = false;
            MenuItemSendPrivateMessage.IsEnabled = false;
            Title = Application.Current.MainWindow.GetType().Assembly.ToString();
            client = null;
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientForm_Closed(object sender, System.EventArgs e)
        {
            if (client != null)
                client.Close();
        }
        /// <summary>
        /// 終了する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        /// <summary>
        /// 接続する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemConnect_Click(object sender, RoutedEventArgs e)
        {
            ShowConnectDialog();
        }
        /// <summary>
        /// 切断する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemDisconnect_Click(object sender, RoutedEventArgs e)
        {
            client.Close();
        }
        /// <summary>
        /// メッセージを送信する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemSendMessage_Click(object sender, RoutedEventArgs e)
        {
            Send();
        }
        /// <summary>
        /// プライベートメッセージを送信する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemSendPrivateMessage_Click(object sender, RoutedEventArgs e)
        {
            Send(true);
        }

        /// <summary>
        /// データを受信した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_ReceivedData(object sender, ReceivedDataEventArgs e)
        {
            AddLog(e.ReceivedString);
        }

        /// <summary>
        /// サーバーに接続した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_Connected(object sender, EventArgs e)
        {
            client = (Radio.Net.Chat.RadioChatClient)sender;
            Dispatcher.Invoke(new Action(SetToConnected));
            AddLog("サーバーに接続しました。");
        }

        /// <summary>
        /// 切断した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_Disconnected(object sender, EventArgs e)
        {
            client = null;
            Dispatcher.Invoke(new Action(SetToDisconnected));
            AddLog("サーバーから切断しました。");
            if (membersList != null)
            {
                membersList.Clear();
                UpdateMembersList();
            }
        }

        /// <summary>
        /// メンバーが参加した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_JoinedMember(object sender, MemberEventArgs e)
        {
            if (client.Name == e.Name)
            {
                //自分がログインしたとき
                membersList = new System.Collections.Specialized.StringCollection();
                AddLog("チャットへの参加に成功しました。");
            }
            else
            {
                //誰かがログインしたとき
                AddLog(string.Format("[{0}]さんが参加しました。", e.Name));
            }

            //メンバーリストを更新
            membersList.Add(e.Name);
            UpdateMembersList();
        }

        /// <summary>
        /// メンバーが退室した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_PartedMember(object sender, MemberEventArgs e)
        {
            if (client.Name == e.Name)
            {
                //自分がログアウトしたとき
                membersList.Clear();
                AddLog("退室しました。");
            }
            else
            {
                //誰かがログアウトしたとき
                membersList.Remove(e.Name);
                AddLog(string.Format("[{0}]さんが退室しました。", e.Name));
            }

            //メンバーリストを更新
            UpdateMembersList();
        }

        /// <summary>
        /// メッセージを受信した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_ReceivedMessage(object sender, ReceivedMessageEventArgs e)
        {
            if (!e.PrivateMessage)
                AddLog(e.From + " > " + e.Message);
            else
                AddLog(e.From + " > " + e.Message);
        }

        /// <summary>
        /// メンバーリストを受信した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_UpdatedMembers(object sender, MembersListEventArgs e)
        {
            //メンバーリストを更新
            membersList.Clear();
            membersList.AddRange(e.Members);
            UpdateMembersList();
        }

        /// <summary>
        /// エラーを受信した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_ReceivedError(object sender, ReceivedErrorEventArgs e)
        {
            AddLog("エラー : " + e.ErrorMessage);

            if (client.LoginState != LoginState.Joined)
            {
                //エラーで閉じる
                client.Close();
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            Send();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            Radio.Net.Chat.RadioChatClient rc = new();
            IPAddress addr = rc.GetLocalIPAddress();
            Title = Title + " " + addr.ToString();
            TextBoxStatusBar.Text = addr.ToString();
        }
    }
}
