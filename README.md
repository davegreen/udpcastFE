Readme - udpcastFE
====================

Description
---------------------

A flexible Microsoft Windows frontend to the udpcast project for multicasting files or folders to client machines.

## Purpose

	A GUI Frontend for the UDPcast project udp-sender program.
	
	This program uses a simple server methodology. Clients connect to the server,
	a file is chosen, then when send is clicked, the server notifies the clients 
	to start the recieving program and then excecutes udp-sender.exe with the 
	required arguments to actually send the file to the clients.
	
Usage
---------------------

## Server

These programs can be run as a GUI, or from the command line, defined arguments are listed below.

Command Line Arguments:

> '-h', '/?' or '--help'	-	Open the About dialog.
> '-f' or '--file'		-	Specify a file or folder to send.

## Client

Command Line Arguments:
	
> '-d' or '--dest'		-	Specify destination folder.
> '-s' or '--server'		-	Specify server IP address to connect to.
> '-p' or '--port'		-	Specify Port number to connect to.
> '-h', '/?' or '--help'	-	Open the About dialog.
			
Example:
	
UDPcastFE.exe -s "192.168.0.1" -p "9010" -d "D:\Documents\udpcastdata"

Note 1:	Quotes are mandatory if a space exists in the file path, as above. I recommended them even if they aren't required.
				
Note 2:	Port 9000 is default. udpcast-reciever.exe will be launched using the specified port + 100,  so using the example above, udpcast-reciever will be launched using port 9110.

Contact
---------------------

For help, feedback, suggestions or bugfixes please check out [http://tookitaway.co.uk/](http://tookitaway.co.uk/) or contact david.green@tookitaway.co.uk.
