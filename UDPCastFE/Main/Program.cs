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
namespace UDPcastSFE
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
            bool help = false;
            string file = string.Empty;
            if (args.Length != 0)
            {
                int i = 0;
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "-f":
                        case "--file":
                            file = args[i + 1];
                            file = file.TrimEnd('"');
                            break;
                        case "/?":
                        case "--help":
                        case "-h":
                            help = true;
                            break;
                    }
                    i++;
                }
                if (System.IO.File.Exists(file) && System.IO.File.GetAttributes(file) != System.IO.FileAttributes.Directory)
                {
                    Application.Run(new ServerGUI(file, false));
                }
                if (System.IO.Directory.Exists(file) && System.IO.File.GetAttributes(file) == System.IO.FileAttributes.Directory)
                {
                    Application.Run(new ServerGUI(file, true));
                }
                else if (help == true)
                {
                    Application.Run(new AboutBox());
                }
            }
            else
            {
                Application.Run(new ServerGUI());
            }
        }
    }
}
