namespace BeaconMonitor;

using System.IO.Ports;

public partial class MainPage : ContentPage
{
  // serial port data
  private bool connectionStarted = false;
  private string selectedPort = "";
  private SerialPort? serialPort = null;

  public string[] Ports { get; set; }


  public MainPage()
  {
    InitializeComponent();

    Ports = SerialPort.GetPortNames();
    serialPortPicker.ItemsSource = Ports;
  }

  public bool Connect(string port) {
    bool result = false;

    try {
      if (!connectionStarted && port != selectedPort) {
        // try to connect to selected port
        serialPort = new SerialPort();
        serialPort.PortName = port;

        // we have a USB serial port communicating at native USB speeds
        // these aren't used
        serialPort.BaudRate = 19200;
        serialPort.Parity = Parity.None;
        serialPort.DataBits = 8;
        serialPort.DtrEnable = false;
        serialPort.RtsEnable = false;

        try {
          serialPort.Open();
        }

        //catch (Exception ex) {
        catch (Exception) {
          connectionStarted = false;
          selectedPort = "";
          return result;
        }

        Thread.Sleep(30);

        if (serialPort.IsOpen) {
          connectionStarted = true;
          selectedPort = port;
          result = true;

          // set T41 time
          SetTime();

        } else {
          connectionStarted = false;
          selectedPort = "";
        }
      }
      return result;
    }

    //catch (Exception ex) {
    catch (Exception) {
      connectionStarted = false;
      selectedPort = "";
      return result;
    }
  }


  public void SetTime() { //long time) {
    // *** TODO: the T41 time will lag from this by the serial processing time, add a second for now ***
    SendCmd("TM" + (DateTimeOffset.Now.ToUnixTimeSeconds() - 7 * 60 * 60 + 1).ToString("D11"));
  }

  private void SendCmd(string cmd) {
    if (serialPort != null && serialPort.IsOpen) {
      serialPort.Write(cmd + ";");
    }
  }

  private void serialPortPicker_SelectedIndexChanged(object sender, EventArgs e) {
    var picker = (Picker)sender;
    int selectedIndex = picker.SelectedIndex;

    if(selectedIndex != -1) {
      var port = picker.ItemsSource[selectedIndex];
      if(port != null) {
        if(Connect((string)port)) {
          SetTime();
        }
      }
    }
  }

  private void OnStartPauseClicked(object sender, EventArgs e) {
  }
}
