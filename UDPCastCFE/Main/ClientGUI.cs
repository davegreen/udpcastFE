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
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Management;
using System.Collections;
using System.Text.RegularExpressions;
using Ionic.Zip;
using System.Diagnostics;

namespace UDPcastCFE
{
    public partial class ClientGUI : Form
    {
        byte[] m_dataBuffer = new byte[10];
        IAsyncResult m_result;
        public AsyncCallback m_pfnCallBack;
        private ArrayList m_interfaces = ArrayList.Synchronized(new ArrayList());
        UDPcastCFE.Properties.Settings settings = new UDPcastCFE.Properties.Settings();
        int iface = 0;
        public delegate void UpdateLogCallback(string data);
        public delegate void UpdateGUICallback(bool status);
        public delegate void OpenBrowseCallback(bool launch);
        public Socket m_clientSocket;
        private bool folder;
        public ClientGUI()
        {
            InitializeComponent();
            UpdateLogControl("Loading Settings");
            settings.Reload();
            UpdateGUIControl(false);
            txtIP.Text = settings.Server;
            nudPort.Value = settings.Port;
            txtDestination.Text = settings.Destination;
            PopulateInterfaces();
            UpdateLogControl("OK.");
        }
        public ClientGUI(string dest)
        {
            InitializeComponent();
            settings.Destination = dest;
            txtDestination.Text = dest;
            PopulateInterfaces();
            UpdateLogControl("OK.");
            UpdateGUIControl(false);
        }
        public ClientGUI(string serv, string dest, int port)
        {
            InitializeComponent();
            settings.Destination = dest;
            settings.Server = serv;
            txtIP.Text = serv;
            txtDestination.Text = dest;
            settings.Port = port;
            UpdateGUIControl(false);
            PopulateInterfaces();
            UpdateLogControl("OK.");
            ServConnect();
        }
        private void SendMessage(string msg)
        {
            try
            {
                NetworkStream networkStream = new NetworkStream(m_clientSocket);
                System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(networkStream);
                streamWriter.WriteLine(msg);
                streamWriter.Flush();
            }
            catch (SocketException se)
            {
                UpdateLogControl(se.Message);
                CloseSocket();
            }
            catch (ArgumentNullException ane)
            {

            }
        }
        public void WaitForData()
        {
            try
            {
                if (m_pfnCallBack == null)
                {
                    m_pfnCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.thisSocket = m_clientSocket;
                // Start listening to the data asynchronously
                m_result = m_clientSocket.BeginReceive(theSocPkt.dataBuffer, 0, theSocPkt.dataBuffer.Length, SocketFlags.None, m_pfnCallBack, theSocPkt);
            }
            catch (SocketException se)
            {
                UpdateLogControl(se.Message);
                CloseSocket();
            }
        }
        public void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket theSockId = (SocketPacket)asyn.AsyncState;
                int iRx = theSockId.thisSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(theSockId.dataBuffer, 0, iRx, chars, 0);
                System.String szData = new System.String(chars);
                //UpdateLogControl(szData);
                if (szData != string.Empty)
                {
                    if (szData.Contains("Folder Send"))
                    {
                        settings.Filename = szData.Substring(13);
                        UpdateLogControl("The Folder is: " + settings.Filename);
                        folder = true;
                        LaunchReciever();
                    }
                    if (szData.Contains("Launch"))
                    {
                        settings.Filename = szData.Substring(8);
                        UpdateLogControl("The file is: " + settings.Filename);
                        folder = false;
                        LaunchReciever();
                    }
                    if (szData == "Server Down")
                    {
                        UpdateLogControl("Server Down, Disconnecting");
                        CloseSocket();
                    }
                    else
                    {
                       UpdateLogControl(szData);
                    }
                }
                WaitForData();
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debugger.Log(0,"OnDataReceived","Socket closed " + ex.Message);
            }
            catch (SocketException se)
            {
                UpdateLogControl(se.Message);
                CloseSocket();
            }
        }
        private void UpdateGUI(bool connected)
        {
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;
            lblStatus.Text = connected ? "Connected" : "Disconnected";
            if (!connected)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                lblStatus.ForeColor = System.Drawing.Color.Black;
            }
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenBrowseControl(false);
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            // See if we have text on the IP text field
            settings.Server = txtIP.Text;
            if (settings.Server == "")
            {
                UpdateLogControl("Server IP required to connect");
                return;
            }
            ServConnect();
        }
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (m_clientSocket.Connected)
            {
                SendMessage("Disconnect by User");
            }
            CloseSocket();
        }
        private void CloseSocket()
        { 
            if (m_clientSocket != null)
            {
                m_clientSocket.Close();
                m_clientSocket = null;
            }
            UpdateGUIControl(false);
            UpdateLogControl("Disconnected");
        }
        private void UpdateLogControl(string data)
        {
            if (InvokeRequired) // Is this called from a thread other than the one created the control
            {
                // We cannot update the GUI on this thread.
                // All GUI controls are to be updated by the main (GUI) thread.
                // Hence we will use the invoke method on the control which will
                // be called when the Main thread is free
                // Do UI update on UI thread
                lbLog.BeginInvoke(new UpdateLogCallback(UpdateLog), data);
            }
            else
            {
                // This is the main thread which created this control, hence update it
                // directly 
                UpdateLog(data);
            }
        }
        private void UpdateGUIControl(bool status)
        {
            if (InvokeRequired)
            {
                lbLog.BeginInvoke(new UpdateGUICallback(UpdateGUI), status);
            }
            else
            {
                UpdateGUI(status);
            }
        }
        private void OpenBrowseControl(bool launch)
        {
            if (InvokeRequired)
            {
                lbLog.BeginInvoke(new OpenBrowseCallback(OpenBrowseDialog), launch);
            }
            else
            {
                OpenBrowseDialog(launch);
            }
        }
        private void OpenBrowseDialog(bool launch)
        {
            FolderBrowserDialog destDialog = new FolderBrowserDialog();
            destDialog.ShowNewFolderButton = true;
            if (destDialog.ShowDialog() == DialogResult.OK)
            {
                settings.Destination = destDialog.SelectedPath;
                txtDestination.Text = destDialog.SelectedPath;
                if (launch)
                {
                    LaunchReciever();
                }
            }
            else
            {
                UserCancel();
            }
        }
        private void UpdateLog(string data)
        {
            lbLog.Items.Add(data);
            lbLog.SelectedIndex = lbLog.Items.Count - 1;
            lbLog.SelectedIndex = -1;
        }     
        static public bool validPath(string path)
        {
            Regex expr = new Regex( @"^(([a-zA-Z]\:)|(\\))(\\{1}|((\\{1})[^\\]([^/:*?<>""|]*))+)$" );
            return expr.IsMatch(path);
        }
        private void LaunchReciever()
        {
            Process proc = new Process();
            string extractdest = settings.Destination;
            string dir = settings.Filename;
            if (folder)
            {
                settings.Destination = System.IO.Path.GetTempPath();
                settings.Filename = "udprecieve.zip";
            }
            if (System.IO.Directory.Exists(settings.Destination) && (validPath(settings.Destination) == true))
            {
                string args = " --interface " + m_interfaces[iface] + " --portbase " + (settings.Port + 100) + " --nokbd -f \"" + settings.Destination + "\\" + settings.Filename + "\"";
                string app = Application.StartupPath + "\\Resources\\udp-receiver.exe";
                proc = Process.Start(app, args);
            }
            else
            {
                OpenBrowseControl(true);
            }
            if (folder)
            {
                try
                {
                    proc.WaitForExit();
                    using (ZipFile extr = ZipFile.Read(System.IO.Path.GetTempPath() + "\\udprecieve.zip"))
                    {
                        extr.ExtractAll(extractdest, ExtractExistingFileAction.DoNotOverwrite);
                    }
                    System.IO.File.Delete(System.IO.Path.GetTempPath() + "\\udprecieve.zip");
                    settings.Destination = extractdest;
                    settings.Filename = dir;
                }
                catch
                {
                }
            }
        }
        private void UserCancel()
        {
            UpdateLogControl("User Cancelled");
            SendMessage("User Cancelled");
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }
        private void ServConnect()
        {
            try
            {
                // Create the socket instance
                m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Get the remote IP address
                IPAddress ip = IPAddress.Parse(txtIP.Text);
                // Create the end point 
                IPEndPoint ipEnd = new IPEndPoint(ip, (int)nudPort.Value);
                // Connect to the remote host
                m_clientSocket.Connect(ipEnd);
                if (m_clientSocket.Connected)
                {
                    UpdateGUIControl(true);
                    UpdateLogControl("Connected to Server at: " + txtIP.Text);
                    settings.Server = txtIP.Text;
                    settings.Interface = cmbInterface.SelectedIndex;
                    settings.Port = (int)nudPort.Value;
                    //Wait for data asynchronously 
                    WaitForData();
                }
            }
            catch (SocketException)
            {
                UpdateLogControl("Connection failed, is the server running?");
                CloseSocket();
            }
            catch (FormatException)
            {
                UpdateLogControl("Invalid IP Address");
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UpdateLogControl("Saving Settings");
            settings.Save();
            if (m_clientSocket != null)
            {
                if (MessageBox.Show(this, "Are you sure you wish to exit?", "Exit?", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
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
                    cmbInterface.SelectedIndex = cmbInterface.Items.Count - 1;
                }
            }
            catch
            {
            }
        }
        private void clearSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.Reset();
            UpdateLogControl("Settings Cleared");
            txtIP.Text = settings.Server;
            nudPort.Value = settings.Port;
            txtDestination.Text = settings.Destination;
            UpdateLogControl("OK.");
        }
        private void cmbInterface_SelectedIndexChanged(object sender, EventArgs e)
        {
            iface = cmbInterface.SelectedIndex;
            UpdateLogControl("Interface: " + cmbInterface.SelectedItem);
        }
        private void txtDestination_TextChanged(object sender, EventArgs e)
        {
            settings.Destination = txtDestination.Text;
        }
    }
}