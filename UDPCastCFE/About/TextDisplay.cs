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
using System.Windows.Forms;
using System.IO;
using System.Reflection;
namespace UDPcastCFE.About
{
    public partial class TextDisplay : Form
    {
        Assembly _assembly;
        StreamReader _textStreamReader;
        public TextDisplay(string file)
        {
            InitializeComponent();
            _assembly = Assembly.GetExecutingAssembly();
            _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream(file));
            txtText.Text = _textStreamReader.ReadToEnd();
            txtText.Select(0,0);
        }
    }
}
