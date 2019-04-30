# @code-art/rx-signalr

## About the library
This @code-art/rx-signalr library is a javascript library that implements a wrapper around [SignalR](https://github.com/SignalR/SignalR) to that events are emitted as stream of events using [RxJs](https://rxjs-dev.firebaseapp.com/). The observerable created by this library will automatically start connections when subscribed to, and stop when unsubsribed to. Also since my use case is an app that needs to run for long time unattended, the library has option to handle disconnected or retrying connection if connection initially fails.

## Installing the library
```bash
yarn add jquery signalr rxjs @code-art/rx-signalr
```

Note: Even though Angular CLI was used for creating and testing the library, it's not required for using the library. 

## Using the library

```typescript
import { signalRObservable } from '@code-art/rx-signalr';

interface MyEvent {
  msg: string;
  id: number;
  user: string;
}

// Creating an observable will not start connection
let myObservable$ = signalRObservable<MyEvent>({
    hubName: 'myhub', // hubname 
    eventName: 'notifyEvent', // event name
  });

// When first observer subscribes to event, the connection will start
let sub = myObservable$.subscribe((e) => {
  // Do something with event
});

// invoking a server side method while connection is open
myObservable$.invoke('methodName', arg1, arg2, arg3);

// when the last observer unsubscribes the connection fill stop
sub.unsubscribe();

```
