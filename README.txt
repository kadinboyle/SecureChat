
//-------------------------------------------------------------------------
Application: Async Chat Application Server & Client
Author: Kadin Boyle
Requirements: Built with .NET Framework 4.5
Version: 1.0.0
Status: Core functionality Implemented. Next rev -> Security & Cryptography
//-------------------------------------------------------------------------

Embedded DLL's:
	- Interop.NATUPNPlib.dll (UPNP Port Forwarding for Server)
	- protobuf-net.dll (Message Serialization/Deserialization)

NOTE: There may still be bugs and errors that cause the programs to crash!

NOTE: The server will atempt to forward your chosen port using UPNP. A
	message will notify you if this fails i.e your router doesnt have UPNP
	or doesnt have it enabled.


To run:

	- First host a server using the Server.exe Application
	- Run the client from any machine
	- Examine the Address and Public IP Address fields in the server
		> If you have the port open on your router OR have UPNP
		option checked / enabled in your router, then your server 
		should be visible from your public IP Address.

		> If not, you will have to use the local IP Address
	- Connect to the server by typing the IP Address the server is hosting on
	- Chat should be working