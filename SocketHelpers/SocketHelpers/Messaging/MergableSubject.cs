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

        readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public void Merge(IObservable<T> source)
        {
            var sub = source.Subscribe(_backingSub.OnNext, _backingSub.OnError);
            _subscriptions.Add(sub);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
        }
    }
}