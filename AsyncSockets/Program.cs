using System;
using AsyncSockets.Client;
using AsyncSockets.Core;
using AsyncSockets.Server;

namespace AsyncSockets
{
    /// <summary>
    ///     This is a console app to test the client and server.
    ///     It does minimal error handling.
    ///     To see the valid commands, start the app and type "help" at the command prompt
    /// </summary>
    internal class Program
    {
        // We use util, and one server, and one client in this app
        private static OsUtil _osUtil;

        private static OsServer _osServer;
        private static OsClient _osClient;


        private static void Main(string[] args)
        {
            //application state trackers
            var shutdown = false;
            var serverstarted = false;
            var clientconnected = false;

            _osUtil = new OsUtil();

            // This is a loop to get commands from the user and execute them
            while (!shutdown)
            {
                Console.Write("> ");
                var userinput = Console.ReadLine();

                if (!string.IsNullOrEmpty(userinput))
                    switch (_osUtil.ParseCommand(userinput))
                    {
                        case OsUtil.OsCmd.OsExit:
                        {
                            if (serverstarted)
                                _osServer.Stop();
                            shutdown = true;
                            break;
                        }
                        case OsUtil.OsCmd.OsStartserver:
                        {
                            if (!serverstarted)
                            {
                                _osServer = new OsServer();
                                var started = _osServer.Start(userinput);
                                if (!started)
                                {
                                    Console.WriteLine("Failed to Start Server.");
                                    Console.WriteLine(_osServer.GetLastError());
                                }
                                else
                                {
                                    Console.WriteLine("Server started successfully.");
                                    serverstarted = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Server is already running.");
                            }
                            break;
                        }
                        case OsUtil.OsCmd.OsConnect:
                        {
                            if (!clientconnected)
                            {
                                _osClient = new OsClient();
                                var connected = _osClient.Connect(userinput);
                                if (!connected)
                                {
                                    Console.WriteLine("Failed to connect Client.");
                                    Console.WriteLine(_osClient.GetLastError());
                                }
                                else
                                {
                                    Console.WriteLine("Client might be connected.  It's hard to say.");
                                    clientconnected = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Client is already connected");
                            }
                            break;
                        }
                        case OsUtil.OsCmd.OsDisconnect:
                            if (clientconnected)
                            {
                                _osClient.DisConnect();
                                clientconnected = false;
                                Console.WriteLine("Client dis-connected from server successfully.");
                            }
                            break;
                        case OsUtil.OsCmd.OsSend:
                        {
                            if (clientconnected)
                            {
                                _osClient.Send(userinput);
                                Console.WriteLine("Message sent from client...");
                            }
                            else
                            {
                                Console.WriteLine("Send Failed with message:");
                                Console.WriteLine(_osClient.GetLastError());
                            }
                            break;
                        }
                        case OsUtil.OsCmd.OsHelp:
                        {
                            Console.WriteLine("Available Commands:");
                            Console.WriteLine("startserver <port> = Start the OS Server (Limit 1 per box)");
                            Console.WriteLine("connect <server> <port> = Connect the client to the OS Server");
                            Console.WriteLine("disconnect = Disconnect from the OS Server");
                            Console.WriteLine("send <message> = Send a message to the OS Server");
                            Console.WriteLine("exit = Stop the server and quit the program");
                            break;
                        }
                    }
            }
        }
    }
}