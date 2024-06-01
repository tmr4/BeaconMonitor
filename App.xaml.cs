namespace BeaconMonitor;

public partial class App : Application
{
  public App()
  {
    InitializeComponent();

    MainPage = new AppShell();
  }

  protected override Window CreateWindow(IActivationState? activationState) {
    Window window = base.CreateWindow(activationState);

    window.Width = 1920;
    window.Height = 1200;
    window.X = 100;
    window.Y = 100;

    return window;
  }
}
