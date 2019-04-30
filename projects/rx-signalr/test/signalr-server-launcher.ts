
import * as server from 'karma-server-side';
import { NgZone } from '@angular/core';

interface ServerInfo {
  port: number;
}

function waitInternal(ms: number): Promise<void> {
  return new Promise<void>((resolve) => {
    window.setTimeout(() => {
      resolve();
    }, ms);
  });
}

async function launchServerInternal(usePort?: number): Promise<number> {
  const port = await server.run(usePort || 0, function(p) {
    const childProcess = serverRequire('child_process');
    const process = serverRequire('process');
    const path = serverRequire('path');
    const ps = childProcess.spawn(
      path.join(process.cwd(), 'sites', 'SignalRTestApp', 'bin', 'Debug', 'net471', 'SignalRTestApp.exe'),
      p ? [p.toString()] : undefined
    );
    return new Promise<number>((resolve, reject) => {
      ps.stdout.on('error', (err) => {
        reject(err);
      });
      let res = '';
      ps.stdout.on('data', (d) => {
        res += d;
        if (d.indexOf('}') >= 0) {
          const info = JSON.parse(res) as ServerInfo;
          this.processes = this.processes || {};
          this.processes[info.port] = ps;
          resolve(info.port);
        }
      });
    });
  });
  await waitInternal(200);

  return port;
}

function stopServerInternal(port: number): Promise<void> {
  return server.run(port, function(p) {
    const ps = this.processes && this.processes[p];
    if (ps) {
      ps.kill();
      if (this.processes) {
        delete this.processes[p];
      }
    }
  });
}

export function wait(ms: number): Promise<void> {
  return waitInternal(ms);
}

export function launchServer(port?: number): Promise<number> {
  NgZone.assertNotInAngularZone();
  return launchServerInternal(port);
}

export function stopServer(port: number): Promise<void> {
  NgZone.assertNotInAngularZone();
  return stopServerInternal(port);
}
