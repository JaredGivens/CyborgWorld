using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Godot;

public class TaskManager {
  private volatile Int32 _runningTaskCount = 0;

  public void RunTask(Action act) {
    Interlocked.Increment(ref _runningTaskCount);

    Task.Run(() => {
      try {
        act();
      } catch (Exception e) {
        GD.PrintErr(e);
      } finally {
        Interlocked.Decrement(ref _runningTaskCount);
        GD.Print("done");
      }
    });
  }

  async public Task Stop() {
    while (Interlocked.CompareExchange(ref _runningTaskCount, 0, 0) != 0) {
      await Task.Delay(256);
    }
  }
}

