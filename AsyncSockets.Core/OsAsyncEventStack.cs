using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace AsyncSockets.Core
{
    /// <summary>
    ///     class OSAsyncEventStack
    ///     This is a very standard stack implementation.
    ///     This one is set up to stack asynchronous socket connections.
    ///     It has only two operations: a push onto the stack, and a pop off of it.
    /// </summary>
    public sealed class OsAsyncEventStack
    {
        private readonly Stack<SocketAsyncEventArgs> _socketstack;

        // This constructor needs to know how many items it will be storing max
        public OsAsyncEventStack(int maxCapacity)
        {
            _socketstack = new Stack<SocketAsyncEventArgs>(maxCapacity);
        }

        // Pop an item off of the top of the stack
        public SocketAsyncEventArgs Pop()
        {
            //We are locking the stack, but we could probably use a ConcurrentStack if
            // we wanted to be fancy
            lock (_socketstack)
                return _socketstack.Count > 0 ? _socketstack.Pop() : null;
        }

        // Push an item onto the top of the stack
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
                throw new ArgumentNullException($"Cannot add null object to socket stack");

            lock (_socketstack)
                _socketstack.Push(item);
        }
    }
}
