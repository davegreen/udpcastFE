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
namespace UDPcastCFE
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string serverIP = string.Empty;
            bool help = false;
            int port = 0;
            string destination = string.Empty;
            if (args.Length != 0)
            {
                int i = 0;
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "--dest":
                        case "-d":
                            destination = args[i + 1];
                            destination = destination.TrimEnd('"');
                            break;
                        case "--server":
                        case "-s":
                            serverIP = args[i + 1];
                            break;
                        case "--port":
                        case "-p":
                            port = Convert.ToInt16(args[i + 1]);
                            break;
                        case "/?":
                        case "--help":
                        case "-h":
                            help = true;
                            break;
                    }
                    i++;
                }
                if (help == true)
                {
                    Application.Run(new AboutBox());
                }
                if ((port < 9000) || (port > 9100))
                {
                    port = 9000;
                }
                if (destination != String.Empty && !System.IO.Directory.Exists(destination))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(destination);
                    }
                    catch
                    {
                        Application.Exit();
                    }
                }
                if (destination != String.Empty && serverIP == String.Empty)
                {
                    Application.Run(new ClientGUI(destination));
                }
                if (serverIP != String.Empty && System.IO.Directory.Exists(destination))
                {
                    Application.Run(new ClientGUI(serverIP, destination, port));
                }
            }
            else
            {
                Application.Run(new ClientGUI());
            }
        }
    }
}
