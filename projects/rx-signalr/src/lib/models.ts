
/**
 * SignalR transport name
 */
export type TransportName = keyof SignalR.Transports;

/**
 * connection options
 */
export interface RxSignalRConnectionOptions {
  /**
   * URL of signalR server (if undefined, it will default to the current website)
   */
  url?: string;

  /**
   * query string
   */
  queryString?: string;

  /**
   * whether logging should be enabled
   */
  logging?: boolean;

  /**
   * use default path (/signalr)
   */
  useDefaultPath?: boolean;

  /**
   * transport(s) to use
   */
  transport?: TransportName | Array<TransportName>;

  /**
   * ping interval
   */
  pingInterval?: number;

  /**
   * credentials
   */
  withCredentials?: boolean;

  /**
   * milliseconds to wait before retrying to connect upon connection loss or failure to connect
   */
  retryTimeout?: number;
}

/**
 * SignalR observable options
 */
export interface RxSignalROptions {

  /**
   * connection options or url (defaults to connection to current website)
   */
  connection?: RxSignalRConnectionOptions | string;

  /**
   * hubname (case insensive)
   */
  hubName: string;

  /**
   * event name to listen to
   */
  eventName: string;

  /**
   * groups to join by invoking joinGroup on the hub
   */
  groups?: string | Array<string>;
}
