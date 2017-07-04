using System;
using System.Net;
using System.Net.Sockets;

namespace AsyncSockets.Core
{
    /// <summary>
    ///     class OSCore
    ///     This is a base class that is used by both clients and servers.
    ///     It contains the plumbing to set up a socket connection.
    /// </summary>
    public class OsCore
    {
        // This is just some utilities that we use all over
        protected OsUtil OsUtil;

        // these are the defaults if the user does not provide any parameters
        protected const string DefaultServer = "127.0.0.1";

        protected const int DefaultPort = 804;

        //  We default to a 256 Byte buffer size
        protected const int DefaultBufferSize = 256;

        // This is the connection socket and endpoint information
        protected Socket Connectionsocket;

        protected IPEndPoint Connectionendpoint;

        // This is some error handling stuff that is not well implemented
        protected string Lasterror;

        protected bool Exceptionthrown;

        // This is the current buffer size for receive and send
        protected int Buffersize;


        // We only instantiate the utility class here.
        // We could probably make it static and avoid this.
        public OsCore()
        {
            OsUtil = new OsUtil();
        }

        // An IPEndPoint contains all of the information about a server or client
        // machine that a socket needs.  Here we create one from information
        // that we send in as parameters
        public IPEndPoint CreateIpEndPoint(string ipAddress, int portnumber)
        {
            try
            {
                // TODO: DNS for IPV6.
                // We get the IP address and stuff from DNS (Domain Name Services)
                // I think you can also pass in an IP address, but I would not because
                // that would not be extensible to IPV6 later
                //
                //var hostInfo = Dns.GetHostEntry(servername);
                //var serverAddr = hostInfo.AddressList[0];
                // return new IPEndPoint(serverAddr, portnumber);

                return new IPEndPoint(IPAddress.Parse(ipAddress), portnumber);
            }
            catch (Exception ex)
            {
                Exceptionthrown = true;
                Lasterror = ex.ToString();
                return null;
            }
        }


        // This method peels apart the command string to create either a client or server socket,
        // which is not great because it means the method has to know the semantics of the command
        // that is passed to it.  So this needs to be fixed.
        protected bool CreateSocket(string cmdstring)
        {
            Exceptionthrown = false;

            if (!string.IsNullOrEmpty(cmdstring))
            {
                // Here is the utility function that actually parses the command string.
                var parameters = OsUtil.ParseParams(cmdstring);

                // Based on the number of parameters in the command string, we create an IPEndPoint
                // with the appropriate values for server and port number.
                // Implicit in here is the fact that a server always creates on localhost, so the
                // startserver command will contain only one parameter (port number), or none.
                // Like I said, this needs to be refactored to be more generic
                if (parameters.Count < 1)
                    Connectionendpoint = CreateIpEndPoint(DefaultServer, DefaultPort);
                else if (parameters.Count == 1)
                    Connectionendpoint = CreateIpEndPoint(DefaultServer, Convert.ToInt32(parameters[0]));
                else
                    Connectionendpoint = CreateIpEndPoint(parameters[0], Convert.ToInt32(parameters[1]));
            }
            else
            {
                Connectionendpoint = CreateIpEndPoint(DefaultServer, DefaultPort);
            }

            // If we get here, we try to create the socket using the IPEndpoint information.
            // We are defaulting here to TCP Stream sockets, but you could change this with more parameters.
            if (Exceptionthrown) return true;
            try
            {
                Connectionsocket = new Socket(Connectionendpoint.AddressFamily, SocketType.Stream,
                    ProtocolType.Tcp);
            }
            catch (Exception ex)
            {
                Exceptionthrown = true;
                Lasterror = ex.ToString();
                return false;
            }
            return true;
        }

        // This method is a lame way for external classes to get the last error message that was posted
        // from this class.  It is a poor man's exception handler.  Don't do this in production code.
        // Use proper exception handling.
        public string GetLastError() => Lasterror;
    }
}
