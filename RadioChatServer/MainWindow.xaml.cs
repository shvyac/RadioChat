using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Radio.Net.Chat;

namespace RadioChatServerApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private delegate void PrintStringInvoker(string str, Color col);
        private RadioChatServer? server;

        #region メソッド
        /// <summary>
        /// Listenを開始する
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        public void Listen(string hostName, int port)
        {
            if (server != null)
            {
                MessageBox.Show(this, "すでにListen中です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            server = new RadioChatServer();
            try
            {
                server.Listen(hostName, port);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Listenに失敗しました。\n(" + ex.Message + ")", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //イベントハンドラを追加
            server.AcceptedClient += new ServerEventHandler(server_AcceptedClient);
            server.DisconnectedClient += new ServerEventHandler(server_DisconnectClient);
            server.ReceivedData += new ReceivedDataEventHandler(server_ReceivedData);
            server.LoggedinMember += new ServerEventHandler(server_LoggedinMember);
            server.LoggedoutMember += new ServerEventHandler(server_LoggedoutMember);

            //ステータスバーに表示
            ShowMessage(server.LocalEndPoint.ToString() + "をListen中...");
            AddLog(server.LocalEndPoint.ToString() + "のListenを開始しました。", Colors.Gray);

            Title = Application.Current.MainWindow.GetType().Assembly.ToString() + " - Listen中(" + server.LocalEndPoint.ToString() + ")";

            menuListen.IsEnabled = false;
            menuDisconnectAllClient.IsEnabled = true;
            menuDisconnectClient.IsEnabled = true;
            menuStopListen.IsEnabled = true;
        }
        public void Listen()
        {
            Listen("0.0.0.0", 23);
        }

        /// <summary>
        /// Listenを中止する
        /// </summary>
        public void StopListen()
        {
            if (server == null)
            {
                MessageBox.Show(this,
                    "Listenしていません。",
                    "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            server.Close();
            server = null;

            //ステータスバーに表示
            ShowMessage("Listenしていません");
            AddLog("Listenを中止しました。", Colors.Gray);

            menuListen.IsEnabled = true;
            menuDisconnectAllClient.IsEnabled = false;
            menuDisconnectClient.IsEnabled = false;
            menuStopListen.IsEnabled = false;
        }

        /// <summary>
        /// ステータスバーに文字列を表示する
        /// </summary>
        /// <param name="str"></param>
        public void ShowMessage(string str)
        {
            Dispatcher.Invoke(new Action<String, Color>(PrivateShowMessage), str, Colors.White);
        }
        private void PrivateShowMessage(string str, Color col)
        {
            TextBoxStatusBar.Text = str;
        }

        /// <summary>
        /// ログに文字列を一行追加する
        /// </summary>
        /// <param name="str"></param>
        /// <param name="col"></param>
        public void AddLog(string str, Color col)
        {
            Dispatcher.Invoke(new Action<String, Color>(PrivateAddLog), str, col);
        }
        private void PrivateAddLog(string str, Color col)
        {
            string addText = DateTime.Now.ToLongTimeString() + " : " + str + "\n";
            TextBoxMSG.AppendText(addText);
            TextBoxMSG.ScrollToEnd();
        }

        /// <summary>
        /// メンバーリストを更新する
        /// </summary>
        public void UpdateClientList()
        {
            Dispatcher.Invoke(new Action(PrivateUpdateClientList));
        }
        private void PrivateUpdateClientList()
        {
            TcpChatClient[] clients = server.AcceptedClients;
            //リストをクリア
            clientViewList.Items.Clear();
            //クライアントをリストに追加
            foreach (AcceptedChatClient c in clients)
            {
                ListViewItem item = new ListViewItem();
                if (c.LoginState == LoginState.Joined)
                {
                    item.Content = c.Name;
                    // item.Text = c.Name;
                }
                else
                {
                    item.Content = "(参加していません)";
                }
                item.Tag = c;

                //item.SubItems.Add(c.RemoteEndPoint.Address.ToString());

                clientViewList.Items.Add(item);
            }
        }
        #endregion

        #region フォームとコントロールのイベントハンドラ
        //フォームのロード
        private void ServerForm_Load(object sender, System.EventArgs e)
        {
            Listen();
        }

        //フォームを閉じた時
        private void ServerForm_Closed(object sender, System.EventArgs e)
        {
            if (server != null)
            {
                server.Close();
            }
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void menuListen_Click(object sender, RoutedEventArgs e)
        {
            Listen();
        }

        //Listen終了
        private void menuStopListen_Click(object sender, RoutedEventArgs e)
        {
            StopListen();
        }

        //クライアント切断
        private void menuDisconnectClient_Click(object sender, RoutedEventArgs e)
        {
            if (clientViewList.SelectedItems.Count == 0)
            {
                MessageBox.Show(this,
                    "クライアントが選択されていません。",
                    "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ListViewItem item in clientViewList.SelectedItems)
            {
                ((AcceptedChatClient)item.Tag).Close();
            }
        }

        //すべてのクライアントを切断
        private void menuDisconnectAllClient_Click(object sender, RoutedEventArgs e)
        {
            server.CloseAllClients();
        }
        #endregion

        #region RadioChatServerのイベントハンドラ
        //クライアントを受け入れた時
        private void server_AcceptedClient(object sender, ServerEventArgs e)
        {
            UpdateClientList();
            AddLog(string.Format("({0})が接続しました。",
                e.Client.RemoteEndPoint.Address.ToString()),
                Colors.Black);
        }

        //クライアントが切断した時
        private void server_DisconnectClient(object sender, ServerEventArgs e)
        {
            UpdateClientList();
            AddLog(string.Format("[{0}]({1})が切断しました。",
                ((AcceptedChatClient)e.Client).Name,
                e.Client.RemoteEndPoint.Address.ToString()),
                Colors.Black);
        }

        //クライアントからデータを受信した時
        private void server_ReceivedData(object sender, ReceivedDataEventArgs e)
        {
            string str =
                e.Client.RemoteEndPoint.Address.ToString() +
                " > " + e.ReceivedString;
            AddLog(str, Colors.LightGray);
        }

        //メンバーがログインしたとき
        private void server_LoggedinMember(object sender, ServerEventArgs e)
        {
            UpdateClientList();
            AddLog(string.Format("{0}が参加しました。",
                ((AcceptedChatClient)e.Client).Name),
                Colors.Black);
        }

        //メンバーがログアウトした時
        private void server_LoggedoutMember(object sender, ServerEventArgs e)
        {
            UpdateClientList();
            AddLog(string.Format("{0}が退室しました。",
                ((AcceptedChatClient)e.Client).Name),
                Colors.Black);
        }

        #endregion
    }
}
