//A Frontend to the UDPcast sender and reciever.
//Copyright (C) 2010 David Green

//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or (at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using System;
using System.Collections;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zip;
using System.Diagnostics;
namespace UDPcastSFE
{
    public partial class ServerGUI : Form
    {
        private bool started = false;
        private Socket m_mainSocket;
        int m_clients = 0;
        public delegate void UpdateClientListCallback();
        public delegate void UpdateLogCallback(string data);
        public delegate void OpenBrowseCallback(bool launch, bool dir);
        public AsyncCallback pfnWorkerCallBack;
        private string source;
        private string oldsource;
        private string filename;
        
        //private ArrayList m_interfaces = ArrayList.Synchronized(new ArrayList());
        UDPcastSFE.Properties.Settings settings = new UDPcastSFE.Properties.Settings();
        // An ArrayList is used to keep track of worker sockets that are designed
        // to communicate with each connected client. Make it a synchronized ArrayList
        // For thread safety
        private ArrayList m_interfaces = ArrayList.Synchronized(new ArrayList());
        private ArrayList m_workerSocketList = ArrayList.Synchronized(new ArrayList());
        // The following variable will keep track of the cumulative 
        // total number of clients connected at any time. Since multiple threads
        // can access this variable, modifying this variable should be done
        // in a thread safe manner
        private int m_clientCount = 0;
        public ServerGUI()
        {
            InitializeComponent();
            UpdateLogControl("Loading Settings");
            settings.Reload();
            cbFolder.Checked = settings.Folder;
            autoStartToolStripMenuItem.Checked = settings.AutoStart;
            fullDuplexToolStripMenuItem.Checked = settings.Duplex;
            nudPort.Value = settings.Port;
            PopulateInterfaces();
            UpdateLogControl("OK.");
            txtIP.Text = GetIP();
            UpdateGUI(false);
        }
        public ServerGUI(string file, bool dir)
        {
            InitializeComponent();
            UpdateLogControl("Loading Settings");
            settings.Reload();
            source = file;
            txtFile.Text = file;
            if (!dir)
            {
                settings.Folder = false;
            }
            else
            {
                settings.Folder = true;
            }
            filename = System.IO.Path.GetFileName(file);
            cbFolder.Checked = settings.Folder;
            autoStartToolStripMenuItem.Checked = settings.AutoStart;
            fullDuplexToolStripMenuItem.Checked = settings.Duplex;
            nudPort.Value = settings.Port;
            PopulateInterfaces();
            UpdateLogControl("OK.");
            txtIP.Text = GetIP();
            UpdateGUI(false);
            SrvStart();
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            SrvStart();
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            SrvStop();
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenBrowseControl(false, cbFolder.Checked);
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (!started || m_workerSocketList.Count == 0)
                {
                    UpdateLogControl("Start the server and connect clients first");
                    return;
                }
                if ((!System.IO.File.Exists(source)) && (!System.IO.Directory.Exists(System.IO.Path.GetFullPath(source))))
                {
                    OpenBrowseControl(true, cbFolder.Checked);
                }
                else
                {
                    LaunchSender();
                }
            }
            catch (ArgumentNullException nu)
            {
                OpenBrowseControl(true, cbFolder.Checked);
            }
        }
        private void UpdateGUI(bool listen)
        {
            btnStart.Enabled = !listen;
            started = listen;
            btnStop.Enabled = listen;
            if (lblConnected.Text.StartsWith("0") == true)
            {
                lblConnected.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                lblConnected.ForeColor = System.Drawing.Color.Black;
            }
        }
        private void SendMsgToAll(string msg)
        {
            byte[] byData = System.Text.Encoding.ASCII.GetBytes(msg);
            Socket workerSocket = null;
            for (int i = 0; i < m_workerSocketList.Count; i++)
            {
                workerSocket = (Socket)m_workerSocketList[i];
                if (workerSocket != null)
                {
                    if (workerSocket.Connected)
                    {
                        workerSocket.Send(byData);
                    }
                }
            }
        }
        private String GetIP()
        {
            String IPStr = "";
            // Find host by name
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList;
            for (int i = 0; i < addr.Length; i++)
            {
                if (addr[i].ToString().Contains(":")) //We dont want IPv6 right now, especially link local addresses
                {
                    IPStr = addr[i].ToString();
                }
                else //Return First IPv4 address
                {
                    UpdateLogControl("IPv4 Address: " + addr[i].ToString());
                    IPStr = addr[i].ToString();
                    return IPStr;
                }
            }
            return IPStr;
        }
        private void CloseSockets()
        {
            SendMsgToAll("Server Down");
            if (m_mainSocket != null)
            {
                m_mainSocket.Close();
            }
            Socket workerSocket = null;
            for (int i = 0; i < m_workerSocketList.Count; i++)
            {
                workerSocket = (Socket)m_workerSocketList[i];
                if (workerSocket != null)
                {
                    workerSocket.Close();
                    workerSocket = null;
                }
            }
            started = false;
            UpdateClientListControl();
        }
        private void UpdateClientListControl()
        {
            if (InvokeRequired) // Is this called from a thread other than the one created the control
            {
                // We cannot update the GUI on this thread.
                // All GUI controls are to be updated by the main (GUI) thread.
                // Hence we will use the invoke method on the control which will
                // be called when the Main thread is free
                // Do UI update on UI thread
                lblConnected.BeginInvoke(new UpdateClientListCallback(UpdateClientList), null);
            }
            else
            {
                // This is the main thread which created this control, hence update it
                // directly 
                UpdateClientList();
            }
        }
        private void UpdateLogControl(string data)
        {
            if (InvokeRequired)
            {
                lbLog.BeginInvoke(new UpdateLogCallback(UpdateLog), data);
            }
            else
            {
                UpdateLog(data);
            }
        }
        private void OpenBrowseControl(bool launch, bool dir)
        {
            if (InvokeRequired)
            {
                lbLog.BeginInvoke(new OpenBrowseCallback(OpenBrowseDialog), launch, dir);
            }
            else
            {
                OpenBrowseDialog(launch, dir);
            }
        }
        private void OpenBrowseDialog(bool launch, bool dir)
        {
            if (dir == false)
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.CheckFileExists = true;
                openDialog.CheckPathExists = true;
                openDialog.Filter = "All files (*.*)|*.*";
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    source = openDialog.FileName.ToString();
                    filename = System.IO.Path.GetFileName(openDialog.FileName.ToString());
                    txtFile.Text = filename;
                    UpdateLogControl("Filename: " + filename);
                    SendMsgToAll("Filename: " + filename);
                    if (launch)
                    {
                        LaunchSender();
                    }
                }
            }
            else
            {
                FolderBrowserDialog srcDialog = new FolderBrowserDialog();
                srcDialog.ShowNewFolderButton = false;
                if (srcDialog.ShowDialog() == DialogResult.OK)
                {
                    source = srcDialog.SelectedPath;
                    filename = System.IO.Path.GetFileName(source);
                    txtFile.Text = srcDialog.SelectedPath;
                    UpdateLogControl("Folder: " + filename);
                    SendMsgToAll("Folder: " + filename);
                    if (launch)
                    {
                        LaunchSender();
                    }
                }
            }
        }
        private void UpdateLog(string data)
        {
            lbLog.Items.Add(data);
            lbLog.SelectedIndex = lbLog.Items.Count - 1;
            lbLog.SelectedIndex = -1;
        }
        // Update the list of clients that is displayed
        private void UpdateClientList()
        {
            m_clients = 0;
            for (int i = 0; i < m_workerSocketList.Count; i++)
            {
                Socket workerSocket = (Socket)m_workerSocketList[i];
                if (workerSocket != null)
                {
                    if (workerSocket.Connected)
                    {
                        m_clients++;
                    }
                }
            }
            lblConnected.Text = m_clients + " Clients Connected";
            UpdateGUI(true);
        }
        private void SendMsgToClient(string msg, int clientNumber)
        {
            if (m_workerSocketList.Count != 0)
            {
                // Convert the reply to byte array
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(msg);
                Socket workerSocket = (Socket)m_workerSocketList[clientNumber - 1];
                workerSocket.Send(byData);
            }
        }
        public void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                // Here we complete/end the BeginAccept() asynchronous call
                // by calling EndAccept() - which returns the reference to
                // a new Socket object
                Socket workerSocket = m_mainSocket.EndAccept(asyn);
                // Now increment the client count for this client 
                // in a thread safe manner
                Interlocked.Increment(ref m_clientCount);
                // Add the workerSocket reference to our ArrayList
                m_workerSocketList.Add(workerSocket);
                // Send a welcome message to client
                SendMsgToClient("Connected as Client " + m_clientCount + ". ", m_clientCount);
                // Display current file to client, if any
                if (System.IO.File.Exists(source))
                {
                    SendMsgToClient("Filename: " + filename, m_clientCount);
                }
                if (System.IO.Directory.Exists(source))
                {
                    SendMsgToClient("Folder: " + filename, m_clientCount);
                }

                // Update running log
                UpdateLogControl("Client " + m_clientCount + " connected");
                // Update the list box showing the list of clients (thread safe call)
                UpdateClientListControl();
                // Let the worker Socket do the further processing for the 
                // just connected client
                WaitForData(workerSocket, m_clientCount);
                // Since the main Socket is now free, it can go back and wait for
                // other clients who are attempting to connect
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "OnClientConnection:", "Socket has been closed");
            }
            catch (SocketException se)
            {
                UpdateLogControl(se.Message);
            }
        }
        public void WaitForData(System.Net.Sockets.Socket soc, int clientNumber)
        {
            try
            {
                if (pfnWorkerCallBack == null)
                {
                    // Specify the call back function which is to be 
                    // invoked when there is any write activity by the 
                    // connected client
                    pfnWorkerCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket(soc, clientNumber);
                soc.BeginReceive(theSocPkt.dataBuffer, 0, theSocPkt.dataBuffer.Length, SocketFlags.None, pfnWorkerCallBack, theSocPkt);
            }
            catch (SocketException se)
            {
                CheckDisconnect(se, clientNumber);
            }
        }
        // This the call back function which will be invoked when the socket
        // detects any client writing of data on the stream
        public void OnDataReceived(IAsyncResult asyn)
        {
            SocketPacket socketData = (SocketPacket)asyn.AsyncState;
            try
            {
                // Complete the BeginReceive() asynchronous call by EndReceive() method
                // which will return the number of characters written to the stream 
                // by the client
                int iRx = socketData.m_currentSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                // Extract the characters as a buffer
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(socketData.dataBuffer, 0, iRx, chars, 0);
                System.String szData = new System.String(chars);
                UpdateLogControl("Client " + socketData.m_clientNumber + ": " + szData);
                //Send back a reply to the client
                //string replyMsg = "";
                //UpdateLogControl("Server Reply: " + replyMsg);
                //Convert the reply to byte array
                //byte[] byData = System.Text.Encoding.ASCII.GetBytes(replyMsg);
                //Socket workerSocket = (Socket)socketData.m_currentSocket;
                //workerSocket.Send(byData);
                WaitForData(socketData.m_currentSocket, socketData.m_clientNumber);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                CheckDisconnect(se, socketData.m_clientNumber);
            }
        }
        private void LaunchSender()
        {
            try
            {
                string autostart = string.Empty;
                string duplexstring = string.Empty;
                string msg = string.Empty;
                UpdateClientListControl();
                if (settings.Folder == true)
                {
                    SendMsgToAll("Sending folder to " + m_clients + " Clients");
                    msg = "Folder Send: udpfiles.zip";
                    UpdateLogControl("Sending Folder: " + source);
                    UpdateLogControl("Compressing Folder...");
                    using (ZipFile zipfolder = new ZipFile())
                    {
                        zipfolder.AddDirectory(source, filename + "\\");
                        zipfolder.Save(System.IO.Path.GetTempPath() + "\\udpfiles.zip");
                    }
                    oldsource = source;
                    source = System.IO.Path.GetTempPath() + "\\udpfiles.zip";
                }
                else
                {
                    SendMsgToAll("Sending file to " + m_clients + " Clients");
                    msg = "Launch: " + filename;
                    UpdateLogControl("Sending file: " + filename);
                }
                if (settings.AutoStart == true)
                {
                    autostart = " --min-receivers " + m_clients;
                }
                if ((settings.Duplex == true) || (autoStartToolStripMenuItem.Checked == true))
                {
                    duplexstring = settings.DuplexTrue;
                }
                string args = " --portbase " + (System.Convert.ToInt16(nudPort.Value) + 100) + autostart + " --interface " + m_interfaces[cmbInterface.SelectedIndex] + " " + duplexstring + " -f \"" + source + "\"";
                string app = Application.StartupPath + "\\Resources\\udp-sender.exe";
                Process process = Process.Start(app, args);
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(msg);
                Socket workerSocket = null;
                for (int i = 0; i < m_workerSocketList.Count; i++)
                {
                    workerSocket = (Socket)m_workerSocketList[i];
                    if (workerSocket != null)
                    {
                        if (workerSocket.Connected)
                        {
                            workerSocket.Send(byData);
                        }
                    }
                }
                process.WaitForExit();
                if (settings.Folder == true)
                {
                    System.IO.File.Delete(System.IO.Path.GetTempPath() + "\\udpfiles.zip");
                    source = oldsource;
                }
            }
            catch (SocketException se)
            {
                UpdateLogControl(se.Message);
            }
            catch (System.IO.IOException)
            {
                OpenBrowseControl(true, cbFolder.Checked);
            }
        }
        private void fullDuplexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!fullDuplexToolStripMenuItem.Checked)
            {
                settings.Duplex = false;
            }
            else
            {
                settings.Duplex = true;
            }
        }
        private void autoStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!autoStartToolStripMenuItem.Checked)
            {
                settings.AutoStart = false;
            }
            else
            {
                settings.AutoStart = true;
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateLogControl("Server Closing");
            CloseSockets();
            this.Close();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }
        private void SrvStart()
        {
            try
            {
                // Create socket
                m_mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, System.Convert.ToInt16(nudPort.Value));
                // Bind Socket
                m_mainSocket.Bind(ipLocal);
                // Start listening
                m_mainSocket.Listen(4);
                // Create call back for connections
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
                UpdateGUI(true);
                UpdateLogControl("Server Started on port " + System.Convert.ToInt16(nudPort.Value));
                settings.Interface = cmbInterface.SelectedIndex;
            }
            catch (SocketException se)
            {
                UpdateLogControl(se.Message);
            }
        }
        private void SrvStop()
        {
            UpdateLogControl("Server Stopped");
            CloseSockets();
            UpdateGUI(false);
        }
        private void SrvRestart()
        {
            SrvStop();
            System.Threading.Thread.Sleep(20);
            SrvStart();
        }
        private void nudPort_ValueChanged(object sender, EventArgs e)
        {
            if (started == true)
            {
                SrvRestart();
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UpdateLogControl("Saving Settings");
            settings.Save();
            if (m_clients != 0)
            {
                if (MessageBox.Show(this, "Clients are connected, are you sure you wish to close?", "Exit with Clients connected?", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
        private void CheckDisconnect(SocketException se, int clientNumber)
        {
            if (se.ErrorCode == 10054) // Error code for Connection reset by peer
            {
                UpdateLogControl("Client " + clientNumber + " Disconnected");
                // Remove the reference to the worker socket of the closed client
                // so that this object will get garbage collected
                m_workerSocketList[clientNumber - 1] = null;
            }
            else
            {
                UpdateLogControl(se.Message);
            }
            UpdateClientListControl();
        }
        private void PopulateInterfaces()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            string MACAddress = string.Empty;
            string Description = string.Empty;
            string RealMACAddress = string.Empty;
            int i = 0;
            foreach (ManagementObject mo in moc)
            {
                if (((bool)mo["IPEnabled"]) && (mo["MACAddress"].ToString().Length > 15))
                {
                    MACAddress = mo["MACAddress"].ToString();
                    RealMACAddress = MACAddress.Replace(":", "-");
                    if (Description != mo["Description"].ToString())
                    {
                        Description = mo["Description"].ToString();
                        m_interfaces.Add(mo["MACAddress"].ToString());
                        cmbInterface.Items.Add(mo["Description"].ToString());
                        i++;
                    }
                }
                mo.Dispose();
            }
            try
            {
                if (settings.Interface > 0)
                {
                    cmbInterface.SelectedIndex = settings.Interface;
                }
                else
                {
                    cmbInterface.SelectedIndex = cmbInterface.Items.Count -1;
                }
            }
            catch
            {
            }
        }
        private void txtFile_Click(object sender, EventArgs e)
        {
            OpenBrowseDialog(false, cbFolder.Checked);
        }
        private void clearSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.Reset();
            UpdateLogControl("Settings Cleared");
            autoStartToolStripMenuItem.Checked = settings.AutoStart;
            fullDuplexToolStripMenuItem.Checked = settings.Duplex;
            nudPort.Value = settings.Port;
            UpdateLogControl("OK.");
        }
        private void cmbInterface_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLogControl("Interface: " + cmbInterface.SelectedItem);
        }
        private void cbFolder_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbFolder.Checked)
            {
                settings.Folder = false;
            }
            else
            {
                settings.Folder = true;
            }
        }
    }
}