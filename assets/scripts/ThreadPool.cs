using System;
using System.Collections.Concurrent;
using System.Threading;
using Godot;

public class ThreadPool {
  private const int _threadCount = 8;
  private readonly Thread[] _threads = new Thread[_threadCount];
  private readonly ConcurrentQueue<Action>[] _queues
    = new ConcurrentQueue<Action>[_threadCount];

  private bool _working = false;

  public ThreadPool() {

    // Initialize the job queues
    for (int i = 0; i < _threadCount; ++i) {
      _queues[i] = new();
    }
    for (int i = 0; i < _threadCount; ++i) {
      int threadId = i;
      _threads[i] = new Thread(() => ThreadWork(threadId));
    }
  }
  public void Start() {
    _working = true;
    for (int i = 0; i < _threadCount; i++) {
      _threads[i].Start();
    }
  }

  public void RunTask(int threadIndex, Action action) {
    if (threadIndex < 0 || threadIndex >= _threadCount) {
      GD.PrintErr("Invalid thread index");
      return;
    }

    // Enqueue the task with the specified thread index
    _queues[threadIndex].Enqueue(action);
  }

  // Wait for all tasks to be completed
  public void Stop() {
    _working = false;

    for (int i = 0; i < _threadCount; i++) {
      _threads[i].Join(); // Block until there is more work to do or all queues are empty
    }
  }

  private void ThreadWork(int threadIndex) {
    while (_working || _queues[threadIndex].Count != 0) {
      // Process tasks assigned to this thread
      if (_queues[threadIndex].TryDequeue(out Action act)) {
        try {
          act();
        } catch (Exception e) {
          GD.PrintErr(e);
        }
      } else {
        Thread.Sleep(32);
      }
    }
  }
}

