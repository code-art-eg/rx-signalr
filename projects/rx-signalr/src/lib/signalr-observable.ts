
import { RxSignalRConnectionOptions, TransportName, RxSignalROptions } from './models';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { shareReplay, filter, map, switchMap, takeUntil, take, delay } from 'rxjs/operators';

/**
 * Hub initialization options
 */
interface HubOptions {
  /**
   * connection options
   */
  connection: RxSignalRConnectionOptions;

  /**
   * hub name
   */
  name: string;
}

/**
 * symbo for property to keep track of unique ids
 */
const hubIdCallbackIdSymbol = Symbol('hubCallbackId');

/**
 * hub call back function (with unique id tracking)
 */
interface HubCallbackFunction {
  (...args: any[]): any;
  [hubIdCallbackIdSymbol]?: number;
}

/**
 * hub call back info
 */
interface HubCallback {
  /**
   * event name
   */
  eventName: string;
  /**
   * call back function
   */
  callback: HubCallbackFunction;
}

/**
 * returns whether the signalR connection options has no transports specified
 * @param options signalR connection options
 */
function emptyTransport(options: RxSignalRConnectionOptions): boolean {
  return !options.transport || Array.isArray(options.transport) && options.transport.length === 0;
}

/**
 * if the signalR connections options has one and only one transport, returns it, otherwise returns undefined.
 * @param options signalR connection options
 */
function singleTransport(options: RxSignalRConnectionOptions): TransportName | undefined {
  return typeof options.transport === 'string' ? options.transport as TransportName :
    Array.isArray(options.transport) && options.transport.length === 1 ? options.transport[0] :
      undefined;
}

/**
 * checkes whether both connection options have the same transport
 * @param options1 first signalR connection options
 * @param options2 second signalR connection options
 */
function sameTransports(options1: RxSignalRConnectionOptions, options2: RxSignalRConnectionOptions) {
  if (emptyTransport(options1)) {
    return emptyTransport(options2);
  }
  const st1 = singleTransport(options1);
  if (st1) {
    return st1 === singleTransport(options2);
  }
  const a2 = options2.transport;
  if (!Array.isArray(a2)) {
    return false;
  }
  const a1 = options1.transport as TransportName[];
  if (a1.length !== a2.length) {
    return false;
  }
  for (let i = 0; i < a1.length; i++) {
    if (a1[i] !== a2[i]) {
      return false;
    }
  }
  return true;
}

/**
 * compares two connection options and returns true if they are similar
 * @param options1 first signalR connection options
 * @param options2 second signalR connection options
 */
function sameConnectionOptions(options1: RxSignalRConnectionOptions, options2: RxSignalRConnectionOptions): boolean {
  if (options1 === options2) {
    return true;
  }
  return options1.url === options2.url
    && options1.logging === options2.logging
    && options1.pingInterval === options2.pingInterval
    && options1.queryString === options2.queryString
    && sameTransports(options1, options2)
    && options1.useDefaultPath === options2.useDefaultPath
    && options1.withCredentials === options2.withCredentials
    && options1.retryTimeout === options2.retryTimeout;
}


/**
 * compares two hub options. They are the same if the connection options is similar and hub name (case insensitive) is similar
 * @param options1 first hub options
 * @param options2 second hub options
 */
function sameHubOptions(options1: HubOptions, options2: HubOptions): boolean {
  return sameConnectionOptions(options1.connection, options2.connection)
    && options1.name.toLocaleLowerCase() === options2.name.toLocaleLowerCase();
}

/**
 * get connection state name (for logging purposes)
 * @param state the connection state
 */
function getConnectionStateName(state: number) {
  switch (state) {
    case SignalR.ConnectionState.Connecting:
      return 'Connecting';
    case SignalR.ConnectionState.Connected:
      return 'Connected';
    case SignalR.ConnectionState.Disconnected:
      return 'Disconnected';
    case SignalR.ConnectionState.Reconnecting:
      return 'Reconnecting';
  }
  return state.toString();
}


/**
 * An object that maintains reference count to consumers and trigger complete event when the last consumer stops
 */
abstract class RefCountedObject {
  /**
   * complete event. Triggers when the last consumer stops. Always emits true. Should never error.
   * Will share replay and will complete once it emits.
   */
  public readonly complete$: Observable<boolean>;

  /**
   * Reference count
   */
  private readonly _refCount$ = new BehaviorSubject<number>(0);

  /** Error if any */
  private _error: any;

  /**
   * whether it was started
   */
  private _started = false;

  /**
   * constructor. Will trigger onStart and initialize reference count to 1
   */
  constructor() {
    this.complete$ = this._refCount$.pipe(
      filter((v) => v === 0),
      map(() => true),
      shareReplay(1),
    );
  }

  public get started(): boolean {
    return this._started;
  }

  /**
   * Whether the object is complete (i.e. all consumers are stopped)
   */
  public get complete(): boolean {
    return this._started && this._refCount$.value === 0;
  }

  /**
   * Error (if any)
   */
  public get error(): any {
    return this._error;
  }

  /**
   * Track another consumer
   */
  public addRef(): void {
    if (this._error) {
      throw new Error(`Cannot start object of type ${this.constructor.name} while it is error state.`);
    }
    if (this.complete) {
      return;
    }
    this._refCount$.next(this._refCount$.value + 1);
    if (!this._started) {
      this._started = true;
      this.onStart();
    }
  }

  /**
   * Stop tracking a cosnumer. If this is the last consumer, trigger complete and call onStop.
   */
  public async stop(): Promise<void> {
    if (!this.error && !this.complete) {
      this._refCount$.next(this._refCount$.value - 1);
      if (this.complete) {
        this._refCount$.complete();
        await this.onStop();
      }
    }
  }

  /**
   * Called initialized
   */
  protected onStart(): void {
  }

  /**
   * Called when last consumer is stops
   */
  protected async onStop(): Promise<void> {
  }

  /**
   * Trigger error. The complete event will complete and all reference counts will stop. OnStop will not be called.
   * complete$ will not tigger an error
   * @param error Error event
   */
  protected onError(error: any): void {
    this._error = error;
    this._refCount$.next(0);
    this._refCount$.complete();
  }
}

/**
 * A keyed RefCountedObject
 */
abstract class KeyedRefCountedObject<TKey> extends RefCountedObject {

  /**
   * constructor
   * @param name object name
   */
  constructor(public readonly key: TKey) {
    super();
  }
}

/**
 * A collection to keep track of ref counted objects.
 */
class RefCountedObjectCollection<TItem extends KeyedRefCountedObject<TKey>, TKey> {
  /**
   * internal collection that keeps track of objects by name
   */
  private readonly _collection: TItem[] = [];

  /**
   * constructor. initializes the collection to empty values.
   * @param _factory factory to create a ref counted object
   */
  constructor(
    private readonly _factory: (key: TKey) => TItem,
    private readonly _compareKeys: (k1: TKey, k2: TKey) => boolean) {
  }

  /**
   * Get object by key.
   * @param key key of the object to retrieve. If it doesn't exists, factory will be used to create one.
   */
  public getByKey(key: TKey): TItem {
    const item = this.findItemByKey(key);
    if (item) {
      // item found add reference
      item.addRef();
      return item;
    }
    const newItem = this._factory(key);
    newItem.addRef();
    this._collection.push(newItem);
    // register removal when complete
    // no need to keey track of subscription
    // since complete$ observable will always complete when it emits
    newItem.complete$.subscribe(() => {
      const index = this._collection.indexOf(newItem);
      if (index >= 0) {
        this._collection.splice(index, 1);
      }
    });
    return newItem;
  }

  /**
   * call stop on all items in the collection
   */
  public async stopAll(): Promise<void> {
    while (this._collection.length > 0) {
      const item = this._collection[this._collection.length - 1];
      // completion will take care of removing the item from the collection
      // completion should not trigger an error
      await this.stopItem(item, true);
    }
  }

  /**
   * call stop for a single item
   * @param key key of item to stop
   */
  public async stopItemByKey(key: TKey): Promise<void> {
    const item = this.findItemByKey(key);
    if (item) {
      // The item complete event should take care of removal from collection
      await this.stopItem(item, false);
    }
  }

  /**
   * find an item by key
   * @param key key for item to look for
   */
  private findItemByKey(key: TKey): TItem | undefined {
    for (const item of this._collection) {
      if (this._compareKeys(item.key, key)) {
        return item;
      }
    }
    return undefined;
  }

  /**
   * call stop on an item
   * @param item item to stop
   * @param untilCompleted whether to continue calling stop until complete is triggered
   */
  private async stopItem(item: TItem, untilCompleted: boolean): Promise<void> {
    do {
      await item.stop();
    } while (untilCompleted && !item.complete);
  }
}

/**
 * Tracks subscribers to a signalR group.
 * Will invoke signalR project method joinGroup on start and will recall it whenever the connection is reconnected.
 */
class GroupInfo extends KeyedRefCountedObject<string> {
  /**
   * constructor
   * @param _hub signalR hub info
   * @param name group name
   */
  constructor(private readonly _hub: HubInfo, name: string) {
    super(name);
  }

  /**
   * Called on initialize. Will invoke join group when connection is started and whenever it is reconnected.
   */
  public onStart(): void {
    this._hub.connected$
      .pipe(
        takeUntil(this.complete$),
        filter((s) => s),
      ).subscribe(() => {
        // Call later as a workaround because sometimes calling join group immediately after reconnect hangs.
        window.setTimeout(() => {
          this.invokeGroupAction('joinGroup');
        }, 100);
      });
  }

  /**
   * Called when last subscriber unsubscribes. Will call leaveGroup if connection is still alive.
   */
  public async onStop(): Promise<void> {
    return this.invokeGroupAction('leaveGroup');
  }

  /**
   * call leave group or leave group
   * @param action action to call join group or leave group
   */
  private async invokeGroupAction(action: 'joinGroup' | 'leaveGroup'): Promise<void> {

    if (this._hub.connected) {
      try {
        this.log(action, false);
        await this._hub.invoke(action, this.key);
        this.log(action, true);
      } catch (e) {
        this.handlePromiseError(action, e);
      }
    }
  }

  /**
   * handle invokation error
   * @param action action to log
   * @param error error to log
   */
  private handlePromiseError(action: 'joinGroup' | 'leaveGroup', error: any): void {
    this.onError(new Error(`Failed to ${action} hub group ${name} on connection ${
      this._hub.connectionName
      }. Failed with error ${error}`));
    if (this._hub.key.connection.logging) {
      console.error(error);
    }
  }

  /**
   * Log joinGroup/leaveGroup events if logging is enabled
   * @param action action to log
   */
  private log(action: 'joinGroup' | 'leaveGroup', done: boolean): void {
    if (this._hub.key.connection.logging) {
      console.log(`${this._hub.key.name}.${
        action
        }(${
        this.key
        }) on connection ${
        this._hub.key.connection.url || '(default)'
        } ${
          done ? 'successful' : 'about to be called'
        }.`);
    }
  }
}

/**
 * SignalR hub connection wrapper
 */
class SignalRConnection extends KeyedRefCountedObject<RxSignalRConnectionOptions> {
  /**
   * Tracks all connections
   */
  private static _connections = new RefCountedObjectCollection<SignalRConnection, RxSignalRConnectionOptions>(
    (options) => new SignalRConnection(options),
    sameConnectionOptions,
  );

  /**
   * internal connection object (will be initalized in onStart when super constructor is called)
   */
  private _hubConnection!: SignalR.Hub.Connection;

  /**
   * subject tracking connection state
   */
  private readonly _state$ = new BehaviorSubject<SignalR.ConnectionState>(SignalR.ConnectionState.Disconnected);

  /**
   * observer for current connection state
   */
  public readonly state$ = this._state$
    .pipe(shareReplay(1));

  /**
   * observer for connected state
   */
  public readonly connected$ = this.state$
    .pipe(
      map((v) => v === SignalR.ConnectionState.Connected),
    );


  /**
   * consturctor.
   * @param key connection options
   */
  constructor(key: RxSignalRConnectionOptions) {
    super(key);
  }

  /**
   *
   * @param options connection options
   */
  public static getConnection(options: RxSignalRConnectionOptions): SignalRConnection {
    return this._connections.getByKey(options);
  }

  /**
   * get connection name (url) for logging purposes
   */
  public get name(): string {
    return this.key.url || '(default)';
  }

  /**
   * handle connection on start (start the connection)
   */
  public onStart(): void {
    // create the connection
    this._hubConnection = $.hubConnection(this.key.url, {
      qs: this.key.queryString || undefined,
      logging: this.key.logging,
      useDefaultPath: this.key.useDefaultPath
    });
    // register for state changes
    this._hubConnection.stateChanged((change) => {
      // if logging is enabled log state change
      if (this.key.logging) {
        console.log(`Connection state changed from ${
          getConnectionStateName(change.oldState)
          } to ${
          getConnectionStateName(change.newState)
          }`);
      }
      // emit new state
      this._state$.next(change.newState);
      if (change.newState === SignalR.ConnectionState.Disconnected) {
        // disconnected can be triggered by 3 events
        // 1. A call to on stop (in this case complete will be true)
        // 2. Connection loss to server.
        // 3. Failure to connect to the server.
        // In cases 2 & 3 complete will be false. An error will be triggered and complete will be emitted
        if (!this.complete) {
          this.onError(new Error(`SignalR Connection ${
            this.name
            } ${
            change.oldState === SignalR.ConnectionState.Connecting ?
              'failed' : 'was lost'
            }.`));
        }
      }
    });

    /** start the actual connection */
    this._hubConnection.start({
      pingInterval: this.key.pingInterval,
      transport: this.key.transport,
      withCredentials: this.key.withCredentials,
    });
  }

  public createProxy(name: string): SignalR.Hub.Proxy {
    return this._hubConnection.createHubProxy(name);
  }

  /**
   * stop the connection
   */
  public async onStop(): Promise<void> {
    if (!this.error) {
      this._hubConnection.stop(true, true);
    }
  }

  /**
   * get current connection state
   */
  public get currentState(): SignalR.ConnectionState {
    return this._state$.value;
  }

  /**
   * get whether the connection is in connected state
   */
  public get connected(): boolean {
    return this._state$.value === SignalR.ConnectionState.Connected;
  }
}

class HubConnectionInfo {
  /**
   * undelying connection wrapper
   */
  public readonly connection: SignalRConnection;

  /**
   * underlying proxy
   */
  public proxy: SignalR.Hub.Proxy;

  constructor(private _key: HubOptions) {
    this.connection = SignalRConnection.getConnection(this._key.connection);
    // create proxy
    this.proxy = this.connection.createProxy(this._key.name);
  }

  public close(): Promise<void> {
    return this.connection.stop();
  }
}

/**
 * Tracks hub information
 */
class HubInfo extends KeyedRefCountedObject<HubOptions> {

  private static readonly _hubs = new RefCountedObjectCollection<HubInfo, HubOptions>(
    (key) => new HubInfo(key),
    sameHubOptions,
  );

  /**
   * Connected state observable
   */
  public connected$!: Observable<boolean>;

  /**
   * Tracks groups
   */
  private readonly _groups = new RefCountedObjectCollection<GroupInfo, string>(
    (n: string) => new GroupInfo(this, n), (k1, k2) => k1 === k2);

  /**
   * timer handle to handle reconnection
   */
  private _timeoutHandle?: number;

  /**
   * list of hub call backs
   */
  private readonly _callBacks: HubCallback[] = [];
  /**
   * next callback unique id
   */
  private _nextId = 0;

  private _connectionInfo$!: BehaviorSubject<HubConnectionInfo | undefined>;


  /**
   * Initialize a hub wrapper
   * @param key hub options
   */
  constructor(key: HubOptions) {
    super(key);
  }

  public static getByKey(options: RxSignalROptions) {
    const connectionOptions = (typeof options.connection === 'string' ? { url: options.connection } : options.connection) || {};
    const key = {
      name: options.hubName,
      connection: connectionOptions,
    };
    return this._hubs.getByKey(key);
  }

  /**
   * Whether the underlying connection is closed
   */
  public get connected(): boolean {
    return !!(this._connectionInfo$.value && this._connectionInfo$.value.connection.connected);
  }

  /**
   * connection name
   */
  public get connectionName(): string {
    return this._connection.name;
  }

  /**
   * wait for hub connection to be in a given status (connected or disconnected)
   * @param status status to wait for
   */
  public async waitForStatus(status: boolean): Promise<void> {
    if (this.connected === status) {
      return;
    }
    await this.connected$
      .pipe(
        filter((f) => f === status),
        take(1),
      ).toPromise();
  }

  /**
   * invoke a method on the hub
   * @param methodName method name
   * @param args argument list
   */
  public async invoke<T>(methodName: string, ...args: any[]): Promise<T | undefined> {
    return await this._proxy.invoke(methodName, ...args);
  }

  /**
   * Subscribe to an event
   * @param eventName event name
   * @param fn event callback
   */
  public on(eventName: string, fn: HubCallbackFunction): number {
    if (fn[hubIdCallbackIdSymbol]) {
      return fn[hubIdCallbackIdSymbol] as number;
    }
    this._proxy.on(eventName, fn);
    fn[hubIdCallbackIdSymbol] = ++this._nextId;
    this._callBacks.push({
      eventName,
      callback: fn,
    });
    return fn[hubIdCallbackIdSymbol] as number;
  }

  /**
   * Unsubscribe from an event
   * @param id id of subscription to remove
   */
  public off(id: number): void {
    const index = this._callBacks.findIndex((c) => c.callback[hubIdCallbackIdSymbol] === id);
    if (index < 0) {
      return;
    }
    const cb = this._callBacks[index];
    this._proxy.off(cb.eventName, cb.callback);
    this._callBacks.splice(index, 1);
  }

  /**
   * Join groups an return a function that can be used to leave all the groups joined.
   * @param groups groups to join
   */
  public joinGroups(groups: string | Array<string> | undefined): () => Promise<void> {
    if (groups) {
      if (typeof groups === 'string') {
        this.joinGroup(groups);
      } else {
        for (const group of groups) {
          this.joinGroup(group);
        }
      }
    }
    return () => this.stopGroups(groups);
  }

  /**
   * initialize the hub
   */
  public onStart(): void {
    this._connectionInfo$ = new BehaviorSubject<HubConnectionInfo | undefined>(undefined);
    this.connected$ = this._connectionInfo$
      .pipe(
        switchMap((v) => v ? v.connection.connected$ : of(false)),
      );
    this.startConnection();
  }

  /**
   * call stop for hub
   */
  public async onStop(): Promise<void> {
    this.releaseConnection();
    // If there is a reconnect attempt, abort it.
    if (this._timeoutHandle) {
      window.clearTimeout(this._timeoutHandle);
      this._timeoutHandle = undefined;
    }
    // Call leave group for all groups (if connected)
    await this._groups.stopAll();
    // call connection stop
    await this._connection.stop();
    if (this._callBacks.length) {
      this._callBacks.splice(0, this._callBacks.length);
    }
  }

  private releaseConnection(): void {
    // since we are switching proxies unsubscribe events from old proxy (will sub them again on new one)
    if (this._connectionInfo$.value && this._proxy) {
      for (const cb of this._callBacks) {
        this._proxy.off(cb.eventName, cb.callback);
      }
    }
  }

  /**
   * start underlying connection
   */
  private startConnection(): void {
    this.releaseConnection();
    // clear handle (since this is called from timeout callback)
    this._timeoutHandle = undefined;
    // create connection
    this._connectionInfo$.next(new HubConnectionInfo(this.key));

    // recreate subs on new proxy
    for (const cb of this._callBacks) {
      this._proxy.on(cb.eventName, cb.callback);
    }

    // if retry is enabled would discard connection and create a new one if connection is lost
    // otherwise go to error state
    const retry = typeof this.key.connection.retryTimeout === 'number' ?
      this.key.connection.retryTimeout : 0;
    this._connection.complete$.subscribe(() => {
      if (this.complete) {
        return;
      }
      if (retry > 0) {
        this._timeoutHandle = window.setTimeout(() => this.startConnection(), retry);
        if (this.key.connection.logging) {
          console.log(`Retrying hub ${this.key.name} connection for connection ${this._connection.name} after ${retry} ms.`);
        }
      } else {
        this.error(`Connection lost for hub ${this.key.name} for connection ${this._connection.name}.`);
      }
    });
  }

  /**
   * stops a list of groups
   * @param groups groups to stop
   */
  private async stopGroups(groups: string | Array<string> | undefined): Promise<void> {
    if (!groups) {
      return;
    }
    if (typeof groups === 'string') {
      await this._groups.stopItemByKey(groups);
    } else {
      for (const group of groups) {
        await this._groups.stopItemByKey(group);
      }
    }
  }

  /**
   * join a group
   * @param name group to join
   */
  private joinGroup(name: string): void {
    this._groups.getByKey(name);
  }

  private get _proxy(): SignalR.Hub.Proxy {
    return (this._connectionInfo$.value as HubConnectionInfo).proxy;
  }

  private get _connection(): SignalRConnection {
    return (this._connectionInfo$.value as HubConnectionInfo).connection;
  }
}

/**
 * An observable that emits events when a signalR event occurs
 */
export interface SignalRObservable<T> extends Observable<T> {
  /**
   * observer for server connection status (true for connected, false otherwise)
   */
  readonly connected$: Observable<boolean>;

  /**
   * Current server connection status
   */
  readonly connected: boolean;

  /**
   * wait for server connection status to be connected
   */
  waitForConnected(): Promise<void>;

  /**
   * wait for server connection status to be disconnected
   */
  waitForDisconnected(): Promise<void>;

  /**
   * wait for server connection status to be as the desired statys
   * @param status desired status
   */
  waitForStatus(status: boolean): Promise<void>;

  /**
   * invoke a server side method
   * @param methodName method name
   * @param args argument list
   */
  invoke(methodName: string, ...args: any[]): Promise<any>;
}

/**
 * implementation for SignalRObservable
 */
class SignalRObservableImpl<T> extends Observable<T> implements SignalRObservable<T> {

  /**
   * internal hub
   */
  private readonly _hub$: BehaviorSubject<HubInfo | undefined>;

  /**
   * options
   */
  private readonly _options: RxSignalROptions;

  /**
   * connected status observable
   */
  public readonly connected$: Observable<boolean>;

  /**
   * create a signalR observable
   * @param options options
   */
  constructor(
    options: RxSignalROptions,
  ) {
    super((observer) => {
      // init hub
      let hub = this._hub$.value as HubInfo;
      if (!hub) {
        hub = HubInfo.getByKey(this._options);
        this._hub$.next(hub);
        hub.complete$.subscribe(() => {
          this._hub$.next(undefined);
        });
      } else {
        hub.addRef();
      }

      // subscribe to complete event
      const sub = hub.complete$.subscribe(
        () => {
          if (hub.error) {
            // A hub with retry will not throw errors
            observer.error(hub.error);
          } else {
            // Errored or complete
            observer.complete();
          }
        }
      );

      // Join groups
      const stopper = hub.joinGroups(options.groups);

      // subscribe to event
      const id = hub.on(this._options.eventName, (d: T) => {
        observer.next(d);
      });

      return {
        unsubscribe: async () => {
          // undo what we did in revere order
          hub.off(id);
          await stopper();
          sub.unsubscribe();
          hub.stop();
        }
      };
    });
    // init hub subect
    // A subject is used becaue connected$ observer can switchMap on it
    this._hub$ = new BehaviorSubject<HubInfo | undefined>(undefined);

    // copy options
    this._options = { ...options };
    if (typeof options.connection === 'object') {
      this._options.connection = { ...options.connection };
    }
    // init connected status
    this.connected$ = this._hub$.pipe(
      switchMap((v) => v ? v.connected$ : of(false)),
    );
  }

  /**
   * observer for server connection status (true for connected, false otherwise)
   */
  public get connected(): boolean {
    return this._hub$.value ? this._hub$.value.connected : false;
  }

  /**
   * wait for server connection status to be connected
   */
  public waitForConnected(): Promise<void> {
    return this.waitForStatus(true);
  }

  /**
   * wait for server connection status to be disconnected
   */
  public waitForDisconnected(): Promise<void> {
    return this.waitForStatus(false);
  }

  /**
   * wait for server connection status to be as the desired statys
   * @param status desired status
   */
  public async waitForStatus(status: boolean): Promise<void> {
    if (this.connected === status) {
      return;
    }
    await this.connected$.pipe(
      filter((f) => f === status),
      take(1),
    ).toPromise();
  }

  /**
   * invoke a server side method
   * @param methodName method name
   * @param args argument list
   */
  public async invoke(methodName: string, ...args: any[]): Promise<any> {
    if (this._hub$.value) {
      return await this._hub$.value.invoke(methodName, ...args);
    }
  }
}

/**
 * creates an observable that emits events when signalR events occur
 * @param options signalR options
 */
export function signalRObservable<T>(options: RxSignalROptions): SignalRObservable<T> {
  return new SignalRObservableImpl<T>(options);
}
