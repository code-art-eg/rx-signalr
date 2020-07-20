import { signalRObservable } from '@code-art-eg/rx-signalr';
import { Injectable } from '@angular/core';

export interface TimerEvent {
  group: string;
  timer: number;
}

@Injectable({
  providedIn: 'root',
})
export class TimerService {
  public readonly timer12 = signalRObservable<TimerEvent>({
    connection: {
      url: 'http://localhost:62559/',
      logging: true,
      retryTimeout: 5000,
    },
    hubName: 'timerHub',
    groups: ['g1', 'g2'],
    eventName: 'notifyTimer',
  });

  public readonly timer1 = signalRObservable<TimerEvent>({
    connection: {
      url: 'http://localhost:62559/',
      logging: true,
      retryTimeout: 5000,
    },
    hubName: 'timerHub',
    groups: ['g1'],
    eventName: 'notifyTimer',
  });

  public readonly timer2 = signalRObservable<TimerEvent>({
    connection: {
      url: 'http://localhost:62559/',
      logging: true,
      retryTimeout: 5000,
    },
    hubName: 'timerHub',
    groups: ['g2'],
    eventName: 'notifyTimer',
  });
}
