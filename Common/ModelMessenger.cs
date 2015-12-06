using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{

    public class ModelMessenger
    {
        private readonly Dictionary<object, Action<object>> _subscribers = 
            new Dictionary<object, Action<object>>();

        public void BroadCast(object self, object message)
        {
            foreach (var subscriber in _subscribers.Where(subscriber => subscriber.Key != self))
            {
                subscriber.Value(message);
            }
        }

        public void Subscribe(object self, Action<object> broadcast)
        {
            _subscribers.Add(self, broadcast);
        }

        public void Unsubscribe(object self)
        {
            _subscribers.Remove(self);
        }

    }
}
