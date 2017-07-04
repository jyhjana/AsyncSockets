using System;
using System.Net.Sockets;
using System.Threading;
using AsyncSockets.Core;

namespace AsyncSockets.Server
{
    /// <summary>
    ///     class OSServer : OSCore
    ///     This is the server class that is derived from OSCore.
    ///     It creates a server that listens for client connections, then receives
    ///     text data from those clients and writes it to the console screen
    /// </summary>
    public class OsServer : OsCore
    {
        // We limit this server client connections for test purposes
        protected const int DefaultMaxConnections = 4;

        // We use a Mutex to block the listener thread so that limited client connections are active
        // on the server.  If you stop the server, the mutex is released. 
        private static Mutex _mutex;

        // Here is where we track the number of client connections
        protected int Numconnections;

        // Here is where we track the totalbytes read by the server
        protected int Totalbytesread;

        // Here is our stack of available accept sockets
        protected OsAsyncEventStack Socketpool;


        // Default constructor
        public OsServer()
        {
            Exceptionthrown = false;

            // First we set up our mutex and semaphore
            _mutex = new Mutex();
            Numconnections = 0;

            // Then we create our stack of read sockets
            Socketpool = new OsAsyncEventStack(DefaultMaxConnections);

            // Now we create enough read sockets to service the maximum number of clients
            // that we will allow on the server
            // We also assign the event handler for IO Completed to each socket as we create it
            // and set up its buffer to the right size.
            // Then we push it onto our stack to wait for a client connection
            for (var i = 0; i < DefaultMaxConnections; i++)
            {
                var item = new SocketAsyncEventArgs();
                item.Completed += OnIoCompleted;
                item.SetBuffer(new byte[DefaultBufferSize], 0, DefaultBufferSize);
                Socketpool.Push(item);
            }
        }


        // This method is called when there is no more data to read from a connected client
        private void OnIoCompleted(object sender, SocketAsyncEventArgs e)
        {
            // Determine which type of operation just completed and call the associated handler.
            // We are only processing receives right now on this server.
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive");
            }
        }


        // We call this method once to start the server if it is not started
        public bool Start(string cmdstring)
        {
            Exceptionthrown = false;

            // First create a generic socket
            if (CreateSocket(cmdstring))
                try
                {
                    // Now make it a listener socket at the IP address and port that we specified
                    Connectionsocket.Bind(Connectionendpoint);

                    // Now start listening on the listener socket and wait for asynchronous client connections
                    Connectionsocket.Listen(DefaultMaxConnections);
                    StartAcceptAsync(null);
                    _mutex.WaitOne();
                    return true;
                }
                catch (Exception ex)
                {
                    Exceptionthrown = true;
                    Lasterror = ex.ToString();
                    return false;
                }
            Lasterror = "Unknown Error in Server Start.";
            return false;
        }

        // This method is called once to stop the server if it is started.
        // We could check for the open socket here
        // to stop some exception noise.
        public void Stop()
        {
            Connectionsocket.Dispose(); // Connectionsocket.Close();
            _mutex.ReleaseMutex();
        }


        // This method implements the asynchronous loop of events
        // that accepts incoming client connections
        public void StartAcceptAsync(SocketAsyncEventArgs acceptEventArg)
        {
            // If there is not an accept socket, create it
            // If there is, reuse it
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += OnAcceptCompleted;
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            // this will return true if there is a connection
            // waiting to be processed (IO Pending) 
            var acceptpending = Connectionsocket.AcceptAsync(acceptEventArg);

            // If not, we can go ahead and process the accept.
            // Otherwise, the Completed event we tacked onto the accept socket will do it when it completes
            if (!acceptpending)
                ProcessAccept(acceptEventArg);
        }


        // This method is triggered when the accept socket completes an operation async
        // In the case of our accept socket, we are looking for a client connection to complete
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs asyncEventArgs) => ProcessAccept(asyncEventArgs);


        // This method is used to process the accept socket connection
        private void ProcessAccept(SocketAsyncEventArgs asyncEventArgs)
        {
            // First we get the accept socket from the passed in arguments
            var acceptsocket = asyncEventArgs.AcceptSocket;

            // If the accept socket is connected to a client we will process it
            // otherwise nothing happens
            if (!acceptsocket.Connected) return;
            try
            {
                // Go get a read socket out of the read socket stack
                var readsocket = Socketpool.Pop();

                // If we get a socket, use it, otherwise all the sockets in the stack are used up
                // and we can't accept anymore connections until one frees up
                if (readsocket != null)
                {
                    // Create our user object and put the accept socket into it to use later
                    readsocket.UserToken = new OsUserToken(acceptsocket, DefaultBufferSize);

                    // We are not using this right now, but it is useful for counting connections
                    Interlocked.Increment(ref Numconnections);

                    // Start a receive request and immediately check to see if the receive is already complete
                    // Otherwise OnIOCompleted will get called when the receive is complete
                    var ioPending = acceptsocket.ReceiveAsync(readsocket);
                    if (!ioPending)
                        ProcessReceive(readsocket);
                }
                else
                {
                    acceptsocket.Dispose(); // acceptsocket.Close();
                    Console.WriteLine(
                        "Client connection refused because the maximum number of client connections allowed on the server has been reached.");
                    var ex = new Exception("No more connections can be accepted on the server.");
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                Exceptionthrown = true;
                Lasterror = ex.ToString();
            }

            // Start the process again to wait for the next connection
            StartAcceptAsync(asyncEventArgs);
        }


        // This method processes the read socket once it has a transaction
        private void ProcessReceive(SocketAsyncEventArgs readSocket)
        {
            // if BytesTransferred is 0, then the remote end closed the connection
            if (readSocket.BytesTransferred > 0)
                if (readSocket.SocketError == SocketError.Success)
                {
                    var token = readSocket.UserToken as OsUserToken;
                    if (token != null && token.ReadSocketData(readSocket))
                    {
                        var readsocket = token.OwnerSocket;

                        // If the read socket is empty, we can do something with the data that we accumulated
                        // from all of the previous read requests on this socket
                        if (readsocket.Available == 0)
                            token.ProcessData(readSocket);

                        // Start another receive request and immediately check to see if the receive is already complete
                        // Otherwise OnIOCompleted will get called when the receive is complete
                        // We are basically calling this same method recursively until there is no more data
                        // on the read socket
                        var ioPending = readsocket.ReceiveAsync(readSocket);
                        if (!ioPending)
                            ProcessReceive(readSocket);
                    }
                    else
                    {
                        if (token != null) Console.WriteLine(token.LastError);
                        CloseReadSocket(readSocket);
                    }
                }
                else
                {
                    ProcessError(readSocket);
                }
            else
                CloseReadSocket(readSocket);
        }


        private void ProcessError(SocketAsyncEventArgs readSocket)
        {
            Console.WriteLine(readSocket.SocketError.ToString());
            CloseReadSocket(readSocket);
        }


        // This overload of the close method doesn't require a token
        private void CloseReadSocket(SocketAsyncEventArgs readSocket)
        {
            var token = readSocket.UserToken as OsUserToken;
            CloseReadSocket(token, readSocket);
        }


        // This method closes the read socket and gets rid of our user token associated with it
        private void CloseReadSocket(OsUserToken token, SocketAsyncEventArgs readSocket)
        {
            token.Dispose();

            // Decrement the counter keeping track of the total number of clients connected to the server.
            Interlocked.Decrement(ref Numconnections);

            // Put the read socket back in the stack to be used again
            Socketpool.Push(readSocket);
        }
    }
}
