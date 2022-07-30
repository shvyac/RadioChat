using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Radio.Net.Chat;

namespace RadioChatServerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;  

        private delegate void PrintStringInvoker(string str, Color col);

        private RadioChatServer server;

        #region メソッド
        /// <summary>
        /// Listenを開始する
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        public void Listen(string hostName, int port)
        {
            if (this.server != null)
            {
                MessageBox.Show(this,
                    "すでにListen中です。",
                    "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.server = new RadioChatServer();
            try
            {
                this.server.Listen(hostName, port);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Listenに失敗しました。\n(" + ex.Message + ")",
                    "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //イベントハンドラを追加
            this.server.AcceptedClient += new ServerEventHandler(server_AcceptedClient);
            this.server.DisconnectedClient += new ServerEventHandler(server_DisconnectClient);
            this.server.ReceivedData += new ReceivedDataEventHandler(server_ReceivedData);
            this.server.LoggedinMember += new ServerEventHandler(server_LoggedinMember);
            this.server.LoggedoutMember += new ServerEventHandler(server_LoggedoutMember);

            //ステータスバーに表示
            this.ShowMessage(this.server.LocalEndPoint.ToString() + "をListen中...");
            this.AddLog(this.server.LocalEndPoint.ToString() +
                "のListenを開始しました。", Colors.Gray);

            this.menuListen.IsEnabled = false;
            this.menuDisconnectAllClient.IsEnabled = true;
            this.menuDisconnectClient.IsEnabled = true;
            this.menuStopListen.IsEnabled = true;
        }
        public void Listen()
        {
            this.Listen("0.0.0.0", 2345);
        }

        /// <summary>
        /// Listenを中止する
        /// </summary>
        public void StopListen()
        {
            if (this.server == null)
            {
                MessageBox.Show(this,
                    "Listenしていません。",
                    "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.server.Close();
            this.server = null;

            //ステータスバーに表示
            this.ShowMessage("Listenしていません");
            this.AddLog("Listenを中止しました。", Colors.Gray);

            this.menuListen.IsEnabled = true;
            this.menuDisconnectAllClient.IsEnabled = false;
            this.menuDisconnectClient.IsEnabled = false;
            this.menuStopListen.IsEnabled = false;
        }

        /// <summary>
        /// ステータスバーに文字列を表示する
        /// </summary>
        /// <param name="str"></param>
        public void ShowMessage(string str)
        {
            //if (this.InvokeRequired)
            //    this.Invoke(new PrintStringInvoker(PrivateShowMessage),
            //        new object[] { str, Colors.Empty });
            //else
            //    this.PrivateShowMessage(str, Colors.Empty);

            if (!Dispatcher.CheckAccess()) PrivateShowMessage(str, Colors.White);
            //else  Dispatcher.Invoke(new Action(PrivateShowMessage),new object[] { str, Colors.White });
        }
        private void PrivateShowMessage(string str, Color col)
        {
            this.TextBoxStatusBar.Text = str;
        }

        /// <summary>
        /// ログに文字列を一行追加する
        /// </summary>
        /// <param name="str"></param>
        /// <param name="col"></param>
        public void AddLog(string str, Color col)
        {
            //if (this.InvokeRequired)
            //{
            //    this.Invoke(new PrintStringInvoker(PrivateAddLog),
            //        new object[] { str, col });
            //}
            //else
            //{
            //    this.PrivateAddLog(str, col);
            //}

            if (!Dispatcher.CheckAccess()) PrivateAddLog(str, col);
            //else  Dispatcher.Invoke(new Action(PrivateAddLog),new object[] { str, col });
        }
        private void PrivateAddLog(string str, Color col)
        {
            string addText = DateTime.Now.ToLongTimeString() + " : " + str + "\n";

            //MaxLengthを超えて表示されるとき
            //if (logTextBox.TextLength + addText.Length > logTextBox.MaxLength)
            //{
            //    int delLen = logTextBox.TextLength + addText.Length - logTextBox.MaxLength;
            //    delLen = logTextBox.Text.IndexOf('\n', delLen) + 1;
            //    logTextBox.Select(0, delLen);
            //    logTextBox.SelectedText = "";
            //}

            //logTextBox.SelectionStart = logTextBox.TextLength;
            //logTextBox.SelectionLength = 0;
            //logTextBox.SelectionColor = col;
            //logTextBox.AppendText(addText);
            //logTextBox.SelectionStart = logTextBox.TextLength;
            //logTextBox.Focus();
            //logTextBox.ScrollToCaret();
        }

        /// <summary>
        /// メンバーリストを更新する
        /// </summary>
        public void UpdateClientList()
        {
            //if (this.InvokeRequired)
            //    this.Invoke(new MethodInvoker(PrivateUpdateClientList),
            //        new object[] { });
            //else
            //    this.PrivateUpdateClientList();

            if (!Dispatcher.CheckAccess()) PrivateUpdateClientList();
            else  Dispatcher.Invoke(new Action(PrivateUpdateClientList));
        }
        private void PrivateUpdateClientList()
        {
            TcpChatClient[] clients = this.server.AcceptedClients;
            //リストをクリア
            this.clientViewList.Items.Clear();
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
             
                this.clientViewList.Items.Add(item);
            }
        }
        #endregion

        #region フォームとコントロールのイベントハンドラ
        //フォームのロード
        private void ServerForm_Load(object sender, System.EventArgs e)
        {
            this.Listen();
        }

        //フォームを閉じた時
        private void ServerForm_Closed(object sender, System.EventArgs e)
        {
            if (this.server != null)
            {
                this.server.Close();
            }
        }

        private void menuExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        //Listen開始
        private void menuListen_Click(object sender, System.EventArgs e)
        {
            this.Listen();
        }

        //Listen終了
        private void menuStopListen_Click(object sender, System.EventArgs e)
        {
            this.StopListen();
        }

        //クライアント切断
        private void menuDisconnectClient_Click(object sender, System.EventArgs e)
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
        private void menuDisconnectAllClient_Click(object sender, System.EventArgs e)
        {
            this.server.CloseAllClients();
        }
        #endregion

        #region DobonChatServerのイベントハンドラ
        //クライアントを受け入れた時
        private void server_AcceptedClient(object sender, ServerEventArgs e)
        {
            this.UpdateClientList();
            this.AddLog(string.Format("({0})が接続しました。",
                e.Client.RemoteEndPoint.Address.ToString()),
                Colors.Black);
        }

        //クライアントが切断した時
        private void server_DisconnectClient(object sender, ServerEventArgs e)
        {
            this.UpdateClientList();
            this.AddLog(string.Format("[{0}]({1})が切断しました。",
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
            this.AddLog(str, Colors.LightGray);
        }

        //メンバーがログインしたとき
        private void server_LoggedinMember(object sender, ServerEventArgs e)
        {
            this.UpdateClientList();
            this.AddLog(string.Format("{0}が参加しました。",
                ((AcceptedChatClient)e.Client).Name),
                Colors.Black);
        }

        //メンバーがログアウトした時
        private void server_LoggedoutMember(object sender, ServerEventArgs e)
        {
            this.UpdateClientList();
            this.AddLog(string.Format("{0}が退室しました。",
                ((AcceptedChatClient)e.Client).Name),
                Colors.Black);
        }
        #endregion  
    }
}
