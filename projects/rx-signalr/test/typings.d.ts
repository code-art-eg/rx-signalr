
interface KarmaServerSideContext {
  processes?: { [key: number]: import('child_process').ChildProcess|undefined };
}

declare module 'karma-server-side' {
  export function run<T>(fn: (this: KarmaServerSideContext) => Promise<T>): Promise<T>;
  export function run<T>(fn: (this: KarmaServerSideContext) => T): Promise<T>;
  export function run(fn: (this: KarmaServerSideContext) => void): Promise<void>;
  export function run<T, U>(arg1: T, fn: (this: KarmaServerSideContext, a: T) => Promise<U>): Promise<U>;
  export function run<T, U>(arg1: T, fn: (this: KarmaServerSideContext, a: T) => U): Promise<U>;
  export function run<T, U>(arg1: T, fn: (this: KarmaServerSideContext, a: T) => void): Promise<void>;
}

