using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SocketHelpers.Messaging
{
    public class MergableSubject<T> : IDisposable
    {
        readonly Subject<T> _backingSub = new Subject<T>();
        public IObservable<T> SubscriptionLine { get { return _backingSub.AsObservable(); } }

        readonly Dictionary<object, IDisposable> _subscriptions = new Dictionary<object, IDisposable>();

        public void Merge(IObservable<T> source, object key = null)
        {
            var sub = source.Subscribe(_backingSub.OnNext, _backingSub.OnError);
            _subscriptions.Add(key ?? source, sub);
        }

        public void Unmerge(object key)
        {
            IDisposable sub;
            if (!_subscriptions.TryGetValue(key, out sub)) return;
            
            _subscriptions.Remove(key);
            sub.Dispose();
        }
        
        public void Dispose()
        {
            foreach (var sub in _subscriptions.Values)
                sub.Dispose();
        }
    }
}