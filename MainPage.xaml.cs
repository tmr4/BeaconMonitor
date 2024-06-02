using Microsoft.UI.Dispatching;
using Microsoft.Maui.Graphics.Platform;
using System.IO.Ports;
using System.Reflection;

using IImage = Microsoft.Maui.Graphics.IImage;

using T41_Control;

namespace BeaconMonitor;


public partial class MainPage : ContentPage
{
  // serial port data
  private T41Control t41Ctl;
  //private bool dataFlag = false;
  public bool DataFlag { get; set; }= false;

  private bool connectionStarted = false;
  private string selectedPort = "";
  private SerialPort? serialPort = null;

  // world map control
  private GraphicsView graphicsView;

  public string[] Ports { get; set; }
  public BeaconData beaconData;

  public MainPage()
  {
    InitializeComponent();
    //this.BindingContext = this;

    t41Ctl = new T41Control(this, DispatcherQueue.GetForCurrentThread());

    graphicsView = new GraphicsView();
    graphicsView.Drawable = new BeaconMonitorMap(this);
    graphicsView.HeightRequest = 1000;
    graphicsView.WidthRequest = 1600;
    beaconVStackLayout.Add(graphicsView);

    Ports = SerialPort.GetPortNames();
    serialPortPicker.ItemsSource = Ports;
    //graphicsView.Invalidate();

    beaconData = new BeaconData();
  }

  public void SetBeaconData(BeaconData data) {
    beaconData = new BeaconData(data);
    graphicsView.Invalidate();
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
        if(t41Ctl.Connect((string)port)) {
          SetTime();
        }
      }
    }
  }

  // *** TODO: investigate .NET Maui property change events ***
  private void OnStartPauseClicked(object sender, EventArgs e) {
    if(DataFlag) {
      startPauseButton.Text = "Start";
      DataFlag = false;
    } else {
      startPauseButton.Text = "Pause";
      DataFlag = true;
    }
    t41Ctl.SetDataFlag();
  }
}

public class Beacon {
  public int order;
  public string region;
  public string callSign;
  public string site;
  public string grid;
  public string beaconPrefix; // *** TODO: check if this can be replaced with grid square ***
  public bool visible;
  public int x, y;
  public int status; // 0-active, >0 see status message at https://www.ncdxf.org/beacon/
  public bool active;
  public bool monitor; // true-monitor, false-don't monitor

  public Beacon() {
  }
}

public class BeaconMonitorMap : IDrawable {
  MainPage mainPage;

  private bool drawStaticMapFlag = true;
  private readonly string[] beaconBandName = ["20", "17", "15", "12", "10"];
  private readonly Beacon[] beacons = [
    // #   Region         Call sign    Site            Grid     prefix visible   x    y status,active,monitor
    new Beacon {  order = 1, region = "United Nations", callSign = " 4U1UN ",  site = "New York    ", grid = "FN30as", beaconPrefix = "W2N", visible = true,   x = 227, y = 170, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Canada        ", callSign = " VE8AT ",  site = "Inuvik, NT  ", grid = "CP38gh", beaconPrefix = "BC",  visible = true,   x =  68, y =  87, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "California    ", callSign = " W6WX ",   site = "Mt Umunhum  ", grid = "CM97bd", beaconPrefix = "W6F", visible = true,   x = 112, y = 184, status = 0, active = true, monitor = true }, // region listed as "United States"
    new Beacon {  order = 1, region = "Hawaii        ", callSign = " KH6RS ",  site = "Maui        ", grid = "BL10ts", beaconPrefix = "KH6", visible = true,   x =  36, y = 220, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "New Zealand   ", callSign = " ZL6B ",   site = "Masterton   ", grid = "RE78tw", beaconPrefix = "ZL",  visible = true,   x = 768, y = 379, status = 0, active = true, monitor = true }, // x was 768, put it off screen
    new Beacon {  order = 1, region = "Australia     ", callSign = " VK6RBP ", site = "Rolystone   ", grid = "OF87av", beaconPrefix = "VK6", visible = true,   x = 656, y = 354, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Japan         ", callSign = " JA2IGY ", site = "Mt Asama    ", grid = "PM84jk", beaconPrefix = "JA",  visible = true,   x = 695, y = 184, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Russia        ", callSign = " RR9O ",   site = "Novosibirsk ", grid = "NO14kx", beaconPrefix = "UA",  visible = true,   x = 577, y = 137, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Hong Kong     ", callSign = " VR2B ",   site = "Hong Kong   ", grid = "OL72bg", beaconPrefix = "VS6", visible = true,   x = 645, y = 217, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Sri Lanka     ", callSign = " 4S7B ",   site = "Colombo     ", grid = "MJ96wv", beaconPrefix = "4S",  visible = true,   x = 566, y = 260, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "South Africa  ", callSign = " ZS6DN ",  site = "Pretoria    ", grid = "KG33xi", beaconPrefix = "ZS",  visible = true,   x = 454, y = 339, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Kenya         ", callSign = " 5Z4B ",   site = "Kikuyu      ", grid = "KI88hr", beaconPrefix = "5Z",  visible = true,   x = 486, y = 289, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Israel        ", callSign = " 4X6TU ",  site = "Tel Aviv    ", grid = "KM72jb", beaconPrefix = "4X",  visible = true,   x = 468, y = 195, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Finland       ", callSign = " OH2B ",   site = "Lohja       ", grid = "KP20eh", beaconPrefix = "OH",  visible = true,   x = 443, y = 123, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Madeira       ", callSign = " CS3B ",   site = "Sao Jorge   ", grid = "IM12jt", beaconPrefix = "CT3", visible = true,   x = 350, y = 195, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Argentina     ", callSign = " LU4AA ",  site = "Buenos Aires", grid = "GF05tj", beaconPrefix = "LU",  visible = true,   x = 263, y = 365, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Peru          ", callSign = " OA4B ",   site = "Lima        ", grid = "FH17mw", beaconPrefix = "OA",  visible = true,   x = 209, y = 307, status = 0, active = true, monitor = true },
    new Beacon {  order = 1, region = "Venezuela     ", callSign = " YV5B ",   site = "Caracas     ", grid = "FJ69cc", beaconPrefix = "YV",  visible = true,   x = 238, y = 253, status = 0, active = true, monitor = true }
  ];

  private bool[] monitorFreq = [true, false, true, false, true];
  private double[,] beaconSNR = new double[18, 5];
  private readonly Color[] beaconSNRColor = [Colors.Black, Colors.LightGray, Colors.Purple, Colors.Blue, Colors.DarkCyan, Colors.Cyan, Colors.Green, Colors.Yellow, Colors.DarkOrange, Colors.Red];


  public BeaconMonitorMap(MainPage mp) {
    mainPage = mp;

    // initialize beacon SNR
    for(int j = 0; j < 18; j++) {
      for(int i = 0; i < 5; i++) {
        beaconSNR[j , i] = 0;
      }
    }
  }

  private Color GetSNRColor(double snr) {
    Color color = Colors.Black;
    int index;

    if(snr > 0) {
      index = (int)(snr / 6);
      if(index < 10) {
        color = beaconSNRColor[index];
      } else {
        color = Colors.Red;
      }
    }

    return color;
  }

  public void Draw(ICanvas canvas, RectF dirtyRect) {
    IImage image;
    Assembly assembly;

    // draw the static part of the world map if needed
    //if(drawStaticMapFlag) {
    if(true) {
      assembly = GetType().GetTypeInfo().Assembly;
      using (Stream stream = assembly.GetManifestResourceStream("BeaconMonitor.Resources.Images.world_map2.bmp")) {
        image = PlatformImage.FromStream(stream);
      }

      if(image != null) {
        canvas.DrawImage(image, 0, 0, image.Width, image.Height);
      }

      canvas.FillColor = Colors.Red;
      canvas.FontSize = 14;
      canvas.FontColor = Colors.Black;
      canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
      for(int i = 0; i < 18; i++) {
        int x, y;
        //x = (int)((double)beacons[i].x * 2.4 / 1.66667 * 1.6);
        x = beacons[i].x * 2;
        y = (int)((double)beacons[i].y * 2.083);
        canvas.FillRectangle(x,y,60,30);
        canvas.DrawString(beacons[i].callSign, x, y, 60, 30, HorizontalAlignment.Center, VerticalAlignment.Center);
      }

      drawStaticMapFlag = false;
    }

    // draw SNR patches
    // *** TODO: this could be streamlines to just update the latest SNR, but who cares ***
    canvas.FontSize = 10;
    canvas.Font = Microsoft.Maui.Graphics.Font.Default;
    for(int i = 0; i < 18; i++) {
      int x, y;

      x = beacons[i].x * 2;
      y = (int)((double)beacons[i].y * 2.083) + 30;

      // print SNR square for each monitored band
      for(int j = 0; j < 5; j++) {
        //Color color = GetSNRColor(beaconSNR[i, j]);
        //Color color = beaconSNRColor[i+j - ((i+j) >= 10 ? 10 + ((i+j) >= 20 ? 10 : 0) : 0)];
        Color color = beaconSNRColor[mainPage.beaconData.snrColors[i, j]];

        canvas.FillColor = color;
        if(monitorFreq[j]) {
          if(color == Colors.Black || color == Colors.Purple || color == Colors.Blue || color == Colors.DarkCyan || color == Colors.Green) {
            canvas.FontColor = Colors.White;
          } else {
            canvas.FontColor = Colors.Black;
          }

          canvas.FillRectangle(x + j * 10, y + 10, 15, 15);
          canvas.DrawString(beaconBandName[j], x + j * 10, y + 10, 15, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
      }
    }

    int band = mainPage.beaconData.band;
    int beacon =  mainPage.beaconData.beacon;
    int audioVolume =  mainPage.beaconData.volume;
    // *** TODO: move fixed items to init function and adjust what is erased ***
    canvas.FontSize = 14;
    canvas.FillColor = Colors.Black;
    canvas.FillRectangle(10, 890, 300, 75);
    canvas.FontColor = Colors.White;
    canvas.DrawString("Monitoring " + beaconBandName[band] + "m:", 20, 890, 150, 20, HorizontalAlignment.Left, VerticalAlignment.Center);
    canvas.DrawString(beacons[beacon].callSign + beacons[beacon].region, 20, 915, 150, 20, HorizontalAlignment.Left, VerticalAlignment.Center);
    canvas.DrawString("Volume: " + audioVolume.ToString(), 20, 940, 150, 20, HorizontalAlignment.Left, VerticalAlignment.Center);

    canvas.FontSize = 22;
    canvas.DrawString("PST: " + DateTime.Now.ToString("H:mm:ss"), 1450, 20, 150, 30, HorizontalAlignment.Left, VerticalAlignment.Center);

    /*
    canvas.StrokeLineCap = LineCap.Round;
    canvas.FillColor = Colors.Gray;

    //canvas.DrawImage(worldMap,0,0,800,480);
    // Translation and scaling
    canvas.Translate(dirtyRect.Center.X, dirtyRect.Center.Y);
    float scale = Math.Min(dirtyRect.Width / 200f, dirtyRect.Height / 200f);
    canvas.Scale(scale, scale);

    // Hour and minute marks
    for (int angle = 0; angle < 360; angle += 6)
    {
        canvas.FillCircle(0, -90, angle % 30 == 0 ? 4 : 2);
        canvas.Rotate(6);
    }

    DateTime now = DateTime.Now;

    // Hour hand
    canvas.StrokeSize = 20;
    canvas.SaveState();
    canvas.Rotate(30 * now.Hour + now.Minute / 2f);
    canvas.DrawLine(0, 0, 0, -50);
    canvas.RestoreState();

    // Minute hand
    canvas.StrokeSize = 10;
    canvas.SaveState();
    canvas.Rotate(6 * now.Minute + now.Second / 10f);
    canvas.DrawLine(0, 0, 0, -70);
    canvas.RestoreState();

    // Second hand
    canvas.StrokeSize = 2;
    canvas.SaveState();
    canvas.Rotate(6 * now.Second);
    canvas.DrawLine(0, 10, 0, -80);
    canvas.RestoreState();
    */
  }
}
