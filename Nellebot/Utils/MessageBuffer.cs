using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Nellebot.Utils;

public class MessageBuffer
{
    private readonly ConcurrentQueue<string> _messageQueue;
    private readonly int _delayMillis;
    private readonly Action<IEnumerable<string>> _callback;
    private readonly Timer _timer;
    private readonly object _lockObject;

    public MessageBuffer(int delayMillis, Action<IEnumerable<string>> callback)
    {
        _messageQueue = new ConcurrentQueue<string>();
        _delayMillis = delayMillis;
        _callback = callback;
        _lockObject = new object();
        _timer = new Timer(InvokeCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void AddMessage(string message)
    {
        _messageQueue.Enqueue(message);
        _timer.Change(_delayMillis, Timeout.Infinite);
    }

    private void InvokeCallback(object? state)
    {
        lock (_lockObject)
        {
            var allMessages = new List<string>();

            while (_messageQueue.TryDequeue(out var message))
            {
                allMessages.Add(message);
            }

            _callback.Invoke(allMessages);
        }
    }
}
