#Socket Helpers Plugin for Xamarin and Windows (PCL)

This library aims to provide useful functionality around the base [sockets-for-pcl](https://github.com/rdavisau/sockets-for-pcl/) classes, including hub-style communications, custom protocol helpers and support for typed messaging, and error handling/life cycle and reliability options. 

#### Service Discovery
Alpha code for service discovery is now included in the project. 
Please see [here](http://ryandavis.io/service-discovery-in-mobile-apps/) for an overview of the design and usage.

#### Typed Message Transmission
Alpha code for strongly typed object transimssion is included in the project via `JsonProtocolMessenger`. 
`JsonProtocolMessenger` wraps a `TcpSocketClient` and facilitates sending and receiving strongly typed objects. It exposes an `IObservable<TMessage>` of messages received, and a `Send(TMessage message)` method for sending to the other party. Serialisation is handled by JSON.NET. 

**Connecting**

Here, `client` is a connected `TcpSocketClient`. `TMessage` is a type from which all your messages derive, `object` is fine if you have no overarching base class. 
```
// wrap a connected socket in a JsonProtocolMessenger, start running the send/receive functions
var messenger = new JsonProtocolMessenger<TMessage>(newClient);
messenger.StartExecuting();
```

**Receiving Messages**

To handle received messages subscribe to the `Messages` property directly, or perform whatever Rx operations or you need.
Messages are not replayed or cached so any subscriptions must be in place before they are received. If you are expecting to receive messages immediately on connect, you should set up those subscriptions before calling `StartExecuting`.
```
// e.g. log any messages we receive to the console
messenger.Messages
         .Subscribe(msg=> Debug.WriteLine(msg));
    
// e.g. print the names of people who send us HelloMessages to the console
messenger.Messages
         .OfType<HelloMessage>()
         .Subscribe(msg => Debug.WriteLine("{0} says hello.", msg.Name));

// e.g. don't proceed until we get a ReadyMessage from the other end
// this kind of subscription has to be made *after* StartExecuting has been called. 
await messenger
         .Messages
         .OfType<ReadyMessage>
         .FirstAsync()
         .ToTask();
```

**Sending Messages**

Call `Send` on `JsonProtocolMessenger`.
```
var msg = new Message { Content = "Hi" };
messenger.Send(msg);
```
