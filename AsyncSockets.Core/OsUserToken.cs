using System;
using System.Net.Sockets;
using System.Text;

namespace AsyncSockets.Core
{
    /// <summary>
    ///     class OSUserToken : IDisposable
    ///     This class represents the instantiated read socket on the server side.
    ///     It is instantiated when a server listener socket accepts a connection.
    /// </summary>
    public sealed class OsUserToken : IDisposable
    {
        // This is a ref copy of the socket that owns this token

        // this stringbuilder is used to accumulate data off of the readsocket
        private readonly StringBuilder _stringbuilder;

        // This stores the total bytes accumulated so far in the stringbuilder
        private int _totalbytecount;

        // We are holding an exception string in here, but not doing anything with it right now.
        public string LastError;

        // The read socket that creates this object sends a copy of its "parent" accept socket in as a reference
        // We also take in a max buffer size for the data to be read off of the read socket
        public OsUserToken(Socket readSocket, int bufferSize)
        {
            OwnerSocket = readSocket;
            _stringbuilder = new StringBuilder(bufferSize);
        }

        // This allows us to refer to the socket that created this token's read socket
        public Socket OwnerSocket { get; }


        // Do something with the received data, then reset the token for use by another connection.
        // This is called when all of the data have been received for a read socket.
        public void ProcessData(SocketAsyncEventArgs args)
        {
            // Get the last message received from the client, which has been stored in the stringbuilder.
            var received = _stringbuilder.ToString();

            //TODO Use message received to perform a specific operation.
            Console.WriteLine("Received: \"{0}\". The server has read {1} bytes.", received, received.Length);

            //TODO: Load up a send buffer to send an ack back to the calling client
            //Byte[] sendBuffer = Encoding.ASCII.GetBytes(received);
            //args.SetBuffer(sendBuffer, 0, sendBuffer.Length);

            // Clear StringBuffer, so it can receive more data from the client.
            _stringbuilder.Length = 0;
            _totalbytecount = 0;
        }


        // This method gets the data out of the read socket and adds it to the accumulator string builder
        public bool ReadSocketData(SocketAsyncEventArgs readSocket)
        {
            var bytecount = readSocket.BytesTransferred;

            if (_totalbytecount + bytecount > _stringbuilder.Capacity)
            {
                LastError = "Receive Buffer cannot hold the entire message for this connection.";
                return false;
            }
            _stringbuilder.Append(Encoding.ASCII.GetString(readSocket.Buffer, readSocket.Offset, bytecount));
            _totalbytecount += bytecount;
            return true;
        }

        // This is a standard IDisposable method
        // In this case, disposing of this token closes the accept socket
        public void Dispose()
        {
            try
            {
                OwnerSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                //Nothing to do here, connection is closed already
            }
            finally
            {
                OwnerSocket.Dispose(); // OwnerSocket.Close();
            }
        }
    }
}
