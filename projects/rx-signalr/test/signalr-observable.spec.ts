import { launchServer, stopServer, wait } from './signalr-server-launcher';
import { RxSignalROptions } from '../src/lib/models';
import { signalRObservable, SignalRObservable } from '../src/public-api';

jasmine.DEFAULT_TIMEOUT_INTERVAL = 60000;
interface EchoEvent {
  group: string;
  message: string;
}

async function expectDisonnected(t$: SignalRObservable<EchoEvent>): Promise<void> {
  await t$.waitForDisconnected();
  expect(t$.connected).toBe(false);
}

async function expectConnected(t$: SignalRObservable<EchoEvent>): Promise<void> {
  await t$.waitForConnected();
  expect(t$.connected).toBe(true);
}
async function expectMessage(t$: SignalRObservable<EchoEvent>, group = 'g1', secs = 1): Promise<void> {
  await expectConnected(t$);

  let evt: EchoEvent | undefined;
  const tempsub = t$.subscribe((e) => {
    evt = e;
  });
  try {
    const msg = Math.random().toString();
    await t$.invoke('send', group, secs, msg);
    await wait(secs * 1000 + 100);
    expect(evt).toBeTruthy();
    expect(evt && evt.message).toBe(msg);
    expect(evt && evt.group).toBe(group);
  } finally {
    tempsub.unsubscribe();
  }
}

describe('signalRObservable Retry', () => {

  let port = 0;

  beforeEach(async () => {
    port = await launchServer();
  });

  afterEach(async () => {
    await stopServer(port);
  });

  function createOptions(groups?: string | Array<string>): RxSignalROptions {
    return {
      connection: {
        url: `http://localhost:${port}`,
        logging: true,
        retryTimeout: 500,
      },
      eventName: 'notifyMessage',
      hubName: 'EchoHub',
      groups,
    };
  }

  it('connects if server initially down', async () => {
    await stopServer(port);
    const t$ = signalRObservable<EchoEvent>(createOptions('g1'));
    const sub = t$.subscribe((e) => {
    });
    try {
      await expectDisonnected(t$);
      await launchServer(port);
      await expectMessage(t$);
    } finally {
      sub.unsubscribe();
    }
    await expectDisonnected(t$);
  });

  it('reconnects after disconnect', async () => {
    const t$ = signalRObservable<EchoEvent>(createOptions('g1'));
    const sub = t$.subscribe(() => {
    });
    try {
      await expectMessage(t$);

      await wait(2000);
      await stopServer(port);
      await expectDisonnected(t$);

      await launchServer(port);
      await wait(2000);
      await expectMessage(t$);
    } finally {
      sub.unsubscribe();
    }
    expectDisonnected(t$);
    await wait(1000);
  });
});


describe('signalRObservable', () => {
  let port = 0;
  const baseOtions: RxSignalROptions = {
    eventName: 'notifyMessage',
    hubName: 'EchoHub',
  };
  beforeAll(async () => {
    port = await launchServer();
    baseOtions.connection = {
      url: `http://localhost:${port}`,
      logging: false,
    };
  });

  function createOptions(groups?: string | Array<string>): RxSignalROptions {
    return { ...baseOtions, groups };
  }

  afterAll(async () => {
    stopServer(port);
  });

  it('connects and disconnects', async () => {
    const opt = createOptions('g1');
    const t$ = signalRObservable<EchoEvent>(opt);
    expect(t$.connected).toBe(false);
    const sub = t$.subscribe((e) => {
    });
    try {
      await expectConnected(t$);
    } finally {
      sub.unsubscribe();
    }
    await expectDisonnected(t$);
  });

  it('send and receives messages', async () => {
    const opt = createOptions('g1');
    const t$ = signalRObservable<EchoEvent>(opt);
    const sub = t$.subscribe((e) => {
    });
    try {
      await expectMessage(t$);
    } finally {
      sub.unsubscribe();
    }
  });
});
