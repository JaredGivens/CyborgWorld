using Godot;
using System;

public partial class Loading : ColorRect {
  private ProgressBar _progressBar;
  private String _nextScenePath;
  private Node _nextNode;
  private Func<Single> GetProgress;
  public Boolean _finished;

  public override void _Ready() {
    _progressBar = GetNode<ProgressBar>("CenterContainer/ProgressBar");
    Visible = false;
    SetProcess(false);
  }
  public void StartLoading(Func<Single> getStatus) {
    GetProgress = getStatus;
    Visible = true;
    SetProcess(true);
  }
  public Boolean IsFinished() {
    if (!_finished) {
      return false;
    }
    _progressBar.Value = 0.0f;
    _finished = false;
    Visible = false;
    SetProcess(false);
    return true;
  }
  public override void _Process(Double delta) {
    var progress = GetProgress();
    _finished = 1.0f == progress;
    _progressBar.Value = progress * 100;
  }
}
