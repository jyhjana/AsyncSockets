using System;
using System.Text;
using AsyncSockets.Core;

namespace AsyncSockets.Client
{
    /// <summary>
    ///     class OSClient : OSCore
    ///     This is a naive client class that I added into this project just to test the server.
    ///     It does very little error checking and is not suitable for anything but testing.
    /// </summary>
    public class OsClient : OsCore
    {
        // This method is used to send a message to the server
        public bool Send(string cmdstring)
        {
            Exceptionthrown = false;

            var parameters = OsUtil.ParseParams(cmdstring);
            if (parameters.Count > 0)
                try
                {
                    // We need a connection to the server to send a message
                    if (!Connectionsocket.Connected) return false;
                    var byData = Encoding.ASCII.GetBytes(parameters[0]);
                    Connectionsocket.Send(byData);
                    return true;
                }
                catch (Exception ex)
                {
                    Lasterror = ex.ToString();
                    return false;
                }
            Lasterror = "No message provided for Send.";
            return false;
        }


        // This method disconnects us from the server
        public void DisConnect()
        {
            try
            {
                Connectionsocket.Dispose(); // Connectionsocket.Close();
            }
            catch
            {
                //nothing to do since connection is already closed
            }
        }


        // This method connects us to the server.
        // Winsock is very optimistic about connecting to the server.
        // It will not tell you, for instance, if the server actually accepted the connection.  It assumes that it did.
        public bool Connect(string cmdstring)
        {
            Exceptionthrown = false;

            if (!CreateSocket(cmdstring)) return false;
            try
            {
                var parameters = OsUtil.ParseParams(cmdstring);
                if (parameters.Count > 1)
                {
                    // This will succeed as long as some server is listening on this IP and port
                    var connectendpoint = CreateIpEndPoint(parameters[0], Convert.ToInt32(parameters[1]));
                    Connectionsocket.Connect(Connectionendpoint);
                    return true;
                }
                Lasterror = "Server and Port not specified on client connection.";
                return false;
            }
            catch (Exception ex)
            {
                Exceptionthrown = true;
                Lasterror = ex.ToString();
                return false;
            }
        }
    }
}
