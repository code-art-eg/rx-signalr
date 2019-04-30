import { Component } from '@angular/core';
import { TimerService } from './timer.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'rx-signalr-app';
  public sub12?: Subscription;
  public sub1?: Subscription;
  public sub2?: Subscription;
  constructor(private readonly timerService: TimerService) {

  }

  public changeSub12(): void {
    const s = this.sub12;
    if (!s) {
      this.sub12 = this.timerService.timer12.subscribe((e) => {
        console.log(`sub12: ${JSON.stringify(e)}`);
      });
    } else {
      s.unsubscribe();
      this.sub12 = undefined;
    }
  }

  public changeSub1(): void {
    const s = this.sub1;
    if (!s) {
      this.sub1 = this.timerService.timer1.subscribe((e) => {
        console.log(`sub1: ${JSON.stringify(e)}`);
      });
    } else {
      s.unsubscribe();
      this.sub1 = undefined;
    }
  }

  public changeSub2(): void {
    const s = this.sub2;
    if (!s) {
      this.sub2 = this.timerService.timer2.subscribe((e) => {
        console.log(`sub2: ${JSON.stringify(e)}`);
      });
    } else {
      s.unsubscribe();
      this.sub2 = undefined;
    }
  }
}
