using ChatDesign.Model;
using Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using WinSound;

namespace ChatDesign.View
{
    /// <summary>
    /// Interaction logic for CallWindowServer.xaml
    /// </summary>
    public partial class CallWindowServer : Window
    {
        public SynchronizationContext uiContext;

        // UI GET USER INFORMATION 
        void GetConnectedUserName()
        {

        }
        void GetConnectedUserImage()
        {

        }

        void GetUserImage()
        {

        }

        void GetUserName()
        {

        }
        private ObservableCollection<CustomItem> contacts;

        public ObservableCollection<CustomItem> Contacts
        {
            get
            {
                if (contacts == null)
                {
                    contacts = new ObservableCollection<CustomItem>();
                }
                return contacts;
            }
        }

        void InitUi(string name, ImageSource image)
        {
            Contacts.Add(new CustomItem { ImagePath = image, Title = name });
        }
        // Image Handler
        #region Image Handler 
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {

            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private static readonly ImageConverter _imageConverter = new ImageConverter();
        public static Bitmap GetImageFromByteArray(byte[] byteArray)
        {

            Bitmap bm = (Bitmap)_imageConverter.ConvertFrom(byteArray);

            if (bm != null && (bm.HorizontalResolution != (int)bm.HorizontalResolution ||
                               bm.VerticalResolution != (int)bm.VerticalResolution))
            {
                // Correct a strange glitch that has been observed in the test program when converting 
                //  from a PNG file image created by CopyImageToByteArray() - the dpi value "drifts" 
                //  slightly away from the nominal integer value
                bm.SetResolution((int)(bm.HorizontalResolution + 0.5f),
                                 (int)(bm.VerticalResolution + 0.5f));
            }

            return bm;
        }
        #endregion
        private List<UserInfo> participants;

        public CallWindowServer(string name, ImageSource image, List<UserInfo> participants)
        {
            InitializeComponent();
            InitComboboxes();
            this.participants = participants;
            uiContext = SynchronizationContext.Current;
            DataContext = this;

            //InitUi(name, image);
            //m_Player.PlayFile("AbletonAudio.wav", Sound.SelectedItem.ToString());
            //m_Player.PlayFile("qqq.mp3", Sound.SelectedItem.ToString());
            InitJitterBufferClientRecording();
            InitJitterBufferClientPlaying();
            InitJitterBufferServerRecording();
            InitTimerShowProgressBarPlayingClient();
            InitProtocolClient();
        }
        public static Dictionary<Object, Queue<List<Byte>>> DictionaryMixed = new Dictionary<Object, Queue<List<byte>>>();
        private NF.TCPClient m_Client;
        private NF.TCPServer m_Server;
        private Configuration m_Config = new Configuration();
        private WinSound.Protocol m_PrototolClient = new WinSound.Protocol(WinSound.ProtocolTypes.LH, Encoding.Default);
        private Encoding m_Encoding = Encoding.GetEncoding(65001);
        private uint m_Milliseconds = 20;
        private Dictionary<NF.ServerThread, ServerThreadData> m_DictionaryServerDatas = new Dictionary<NF.ServerThread, ServerThreadData>();
        private int m_SoundBufferCount = 8;
        private long m_TimeStamp = 0;
        private Object LockerDictionary = new Object();
        private WinSound.EventTimer m_TimerMixed = null;
        private WinSound.Recorder m_Recorder_Client;
        private WinSound.Recorder m_Recorder_Server;
        static private WinSound.Player m_Player = new Player();
        private WinSound.Buffer m_JitterBufferServerRecording;
        private WinSound.Player m_PlayerClient;
        private string m_ConfigFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
        private long m_SequenceNumber = 4596;
        private uint m_RecorderFactor = 4;
        DispatcherTimer m_TimerProgressBarPlayingClient = new DispatcherTimer();
        private WinSound.Buffer m_JitterBufferClientRecording;
        private bool m_IsFormMain = true;
        private WinSound.Buffer m_JitterBufferClientPlaying;
        private const int RecordingJitterBufferCount = 8;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Daten holen
                FormToConfig();

                if (IsServerRunning)
                {
                    StopServer();
                    StopRecordingFromSounddevice_Server();
                    StopTimerMixed();
                }
                else
                {
                    StartServer();

                    //Wenn aktiv
                    if (m_Config.ServerNoSpeakAll == false)
                    {
                        StartRecordingFromSounddevice_Server();
                    }

                    StartTimerMixed();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadConfig()
        {
            try
            {
                //Wenn die Datei existiert
                if (File.Exists(m_ConfigFileName))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Configuration));
                    StreamReader sr = new StreamReader(m_ConfigFileName);
                    m_Config = (Configuration)ser.Deserialize(sr);
                    sr.Close();
                }

                //Daten anzeigen
                // IP HERE
                //ConfigToForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        private void InitJitterBufferServerRecording()
        {
            //Wenn vorhanden
            if (m_JitterBufferServerRecording != null)
            {
                m_JitterBufferServerRecording.DataAvailable -= new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferServerDataAvailable);
            }

            //Neu erstellen
            m_JitterBufferServerRecording = new WinSound.Buffer(null, RecordingJitterBufferCount, 20);
            m_JitterBufferServerRecording.DataAvailable += new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferServerDataAvailable);
        }
        private void InitProtocolClient()
        {
            if (m_PrototolClient != null)
            {
                m_PrototolClient.DataComplete += new WinSound.Protocol.DelegateDataComplete(OnProtocolClient_DataComplete);
            }
        }

        private void OnProtocolClient_DataComplete(Object sender, Byte[] data)
        {
            try
            {
                //Wenn der Player gestartet wurde
                if (m_PlayerClient != null)
                {
                    if (m_PlayerClient.Opened)
                    {
                        //RTP Header auslesen
                        WinSound.RTPPacket rtp = new WinSound.RTPPacket(data);

                        //Wenn Header korrekt
                        if (rtp.Data != null)
                        {
                            //In JitterBuffer hinzufügen
                            if (m_JitterBufferClientPlaying != null)
                            {
                                m_JitterBufferClientPlaying.AddData(rtp);
                            }
                        }
                    }
                }
                else
                {
                    //Konfigurationsdaten erhalten
                    OnClientConfigReceived(sender, data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void OnClientConfigReceived(Object sender, Byte[] data)
        {
            try
            {
                String msg = m_Encoding.GetString(data);
                if (msg.Length > 0)
                {
                    //Parsen
                    String[] values = msg.Split(':');
                    String cmd = values[0];

                    //Je nach Kommando
                    switch (cmd.ToUpper())
                    {
                        case "SAMPLESPERSECOND":
                            int samplePerSecond = Convert.ToInt32(values[1]);
                            m_Config.SamplesPerSecondClient = samplePerSecond;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Code to update the UI or perform other actions on the UI thread goes here
                                StartPlayingToSounddevice_Client();
                                StartRecordingFromSounddevice_Client();
                            });
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void StartRecordingFromSounddevice_Client()
        {
            try
            {
                if (IsRecorderFromSounddeviceStarted_Client == false)
                {
                    //Buffer Grösse berechnen
                    int bufferSize = 0;
                    if (UseJitterBufferClientRecording)
                    {
                        bufferSize = WinSound.Utils.GetBytesPerInterval((uint)m_Config.SamplesPerSecondClient, m_Config.BitsPerSampleClient, m_Config.ChannelsClient) * (int)m_RecorderFactor;
                    }
                    else
                    {
                        bufferSize = WinSound.Utils.GetBytesPerInterval((uint)m_Config.SamplesPerSecondClient, m_Config.BitsPerSampleClient, m_Config.ChannelsClient);
                    }

                    //Wenn Buffer korrekt
                    if (bufferSize > 0)
                    {
                        //Recorder erstellen
                        m_Recorder_Client = new WinSound.Recorder();

                        //Events hinzufügen
                        m_Recorder_Client.DataRecorded += new WinSound.Recorder.DelegateDataRecorded(OnDataReceivedFromSoundcard_Client);
                        m_Recorder_Client.RecordingStopped += new WinSound.Recorder.DelegateStopped(OnRecordingStopped_Client);

                        //Recorder starten
                        if (m_Recorder_Client.Start(m_Config.SoundInputDeviceNameClient, m_Config.SamplesPerSecondClient, m_Config.BitsPerSampleClient, m_Config.ChannelsClient, m_SoundBufferCount, bufferSize))
                        {
                            //Anzeigen
                            //ShowStreamingFromSounddeviceStarted_Client();
                            MessageBox.Show("Recording Start");
                            //Wenn JitterBuffer
                            if (UseJitterBufferClientRecording)
                            {
                                m_JitterBufferClientRecording.Start();
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void StartPlayingToSounddevice_Client()
        {
            //Wenn gewünscht
            if (IsPlayingToSoundDeviceWanted)
            {
                //JitterBuffer starten
                if (m_JitterBufferClientPlaying != null)
                {
                    InitJitterBufferClientPlaying();
                    m_JitterBufferClientPlaying.Start();
                }

                if (m_PlayerClient == null)
                {
                    m_PlayerClient = new WinSound.Player();
                    m_PlayerClient.Open(m_Config.SoundOutputDeviceNameClient, m_Config.SamplesPerSecondClient, m_Config.BitsPerSampleClient, m_Config.ChannelsClient, (int)m_Config.JitterBufferCountClient);
                }

                //Timer starten
                m_TimerProgressBarPlayingClient.Start();

            }

            //Anzeigen
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Code to update the UI goes here
                Sound.IsEnabled = false;
                //NumericUpDownJitterBufferClient.IsEnabled = false;
                //ProgressBarPlayingClient.Maximum = (int)m_JitterBufferClientPlaying.Maximum;
            });

        }
        private bool IsPlayingToSoundDeviceWanted
        {
            get
            {
                if (Sound.SelectedIndex >= 1)
                {
                    return true;
                }
                return false;
            }
        }
        private void OnJitterBufferServerDataAvailable(Object sender, WinSound.RTPPacket rtp)
        {
            try
            {
                if (IsServerRunning)
                {
                    if (m_IsFormMain)
                    {
                        //RTP Packet in Bytes umwandeln
                        Byte[] rtpBytes = rtp.ToBytes();

                        //Für alle Clients
                        List<NF.ServerThread> list = new List<NF.ServerThread>(m_Server.Clients);
                        foreach (NF.ServerThread client in list)
                        {
                            //Wenn nicht Mute
                            if (client.IsMute == false)
                            {
                                try
                                {
                                    //Absenden
                                    client.Send(m_PrototolClient.ToBytes(rtpBytes));
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                System.Diagnostics.StackFrame sf = new System.Diagnostics.StackFrame(true);
                //ShowError(LabelServer, String.Format("Exception: {0} StackTrace: {1}. FileName: {2} Method: {3} Line: {4}", ex.Message, ex.StackTrace, sf.GetFileName(), sf.GetMethod(), sf.GetFileLineNumber()));
            }
        }
        private void InitJitterBufferClientPlaying()
        {
            //Wenn vorhanden
            if (m_JitterBufferClientPlaying != null)
            {
                m_JitterBufferClientPlaying.DataAvailable -= new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferClientDataAvailablePlaying);
            }

            //Neu erstellen
            m_JitterBufferClientPlaying = new WinSound.Buffer(null, m_Config.JitterBufferCountClient, 20);
            m_JitterBufferClientPlaying.DataAvailable += new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferClientDataAvailablePlaying);
        }
        private void OnJitterBufferClientDataAvailablePlaying(Object sender, WinSound.RTPPacket rtp)
        {
            try
            {
                if (m_PlayerClient != null)
                {
                    if (m_PlayerClient.Opened)
                    {
                        if (m_IsFormMain)
                        {
                            //Wenn nicht stumm
                            if (m_Config.MuteClientPlaying == false)
                            {
                                //Nach Linear umwandeln
                                Byte[] linearBytes = WinSound.Utils.MuLawToLinear(rtp.Data, m_Config.BitsPerSampleClient, m_Config.ChannelsClient);
                                //Abspielen
                                m_PlayerClient.PlayData(linearBytes, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                System.Diagnostics.StackFrame sf = new System.Diagnostics.StackFrame(true);
                //ShowError(LabelClient, String.Format("Exception: {0} StackTrace: {1}. FileName: {2} Method: {3} Line: {4}", ex.Message, ex.StackTrace, sf.GetFileName(), sf.GetMethod(), sf.GetFileLineNumber()));
            }
        }
        private void InitJitterBufferClientRecording()
        {
            //Wenn vorhanden
            if (m_JitterBufferClientRecording != null)
            {
                m_JitterBufferClientRecording.DataAvailable -= new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferClientDataAvailableRecording);
            }

            //Neu erstellen
            m_JitterBufferClientRecording = new WinSound.Buffer(null, RecordingJitterBufferCount, 20);
            m_JitterBufferClientRecording.DataAvailable += new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferClientDataAvailableRecording);
        }

        private void OnJitterBufferClientDataAvailableRecording(Object sender, WinSound.RTPPacket rtp)
        {
            try
            {
                //Prüfen
                if (rtp != null && m_Client != null && rtp.Data != null && rtp.Data.Length > 0)
                {
                    if (IsClientConnected)
                    {
                        if (m_IsFormMain)
                        {
                            //RTP Packet in Bytes umwandeln
                            Byte[] rtpBytes = rtp.ToBytes();
                            //Absenden
                            m_Client.Send(m_PrototolClient.ToBytes(rtpBytes));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                System.Diagnostics.StackFrame sf = new System.Diagnostics.StackFrame(true);
                //ShowError(LabelClient, String.Format("Exception: {0} StackTrace: {1}. FileName: {2} Method: {3} Line: {4}", ex.Message, ex.StackTrace, sf.GetFileName(), sf.GetMethod(), sf.GetFileLineNumber()));
            }
        }

        private bool IsServerRunning
        {
            get
            {
                if (m_Server != null)
                {
                    return m_Server.State == NF.TCPServer.ListenerState.Started;
                }
                return false;
            }
        }
        private void StartServer()
        {
            try
            {
                if (IsServerRunning == false)
                {
                    if (m_Config.IPAddressServer.Length > 0 && m_Config.PortServer > 0)
                    {
                        m_Server = new NF.TCPServer();
                        m_Server.ClientConnected += new NF.TCPServer.DelegateClientConnected(OnServerClientConnected);
                        m_Server.ClientDisconnected += new NF.TCPServer.DelegateClientDisconnected(OnServerClientDisconnected);
                        m_Server.DataReceived += new NF.TCPServer.DelegateDataReceived(OnServerDataReceived);
                        m_Server.Start(m_Config.IPAddressServer, m_Config.PortServer);
                        MessageBox.Show("Start");
                        //Je nach Server Status
                        if (m_Server.State == NF.TCPServer.ListenerState.Started)
                        {

                        }
                        else
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnServerDataReceived(NF.ServerThread st, Byte[] data)
        {
            //Wenn vorhanden
            if (m_DictionaryServerDatas.ContainsKey(st))
            {
                //Wenn Protocol
                ServerThreadData stData = m_DictionaryServerDatas[st];
                if (stData.Protocol != null)
                {
                    stData.Protocol.Receive_LH(st, data);
                }
            }
        }
        private void OnServerClientDisconnected(NF.ServerThread st, string info)
        {
            try
            {
                //Wenn vorhanden
                if (m_DictionaryServerDatas.ContainsKey(st))
                {
                    //Alle Daten freigeben
                    ServerThreadData data = m_DictionaryServerDatas[st];
                    MessageBox.Show("Client Disconnect");
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        DeleteUpdateListBox(st);
                    });
                    data.Dispose();
                    lock (LockerDictionary)
                    {
                        //Entfernen
                        m_DictionaryServerDatas.Remove(st);
                    }
                }

                //Aus Mixdaten entfernen
                CallWindowServer.DictionaryMixed.Remove(st);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void AddUpdateListBox(string name, byte[] img)
        {
            
            Contacts.Add(new CustomItem { Title = name, ImagePath = ImageSourceFromBitmap(GetImageFromByteArray(img)) });
        }

        private void DeleteUpdateListBox(NF.ServerThread st)
        {
            var itemToRemove = Contacts.FirstOrDefault(item => item.Title == st.Client.Client.RemoteEndPoint.ToString());
            if (itemToRemove != null)
            {
                Contacts.Remove(itemToRemove);
            }
        }


        private void OnServerClientConnected(NF.ServerThread st)
        {
            try
            {
                MessageBox.Show("Client Connected to Audio " + st.Client.Client.RemoteEndPoint.ToString());

                //ServerThread Daten erstellen
                ServerThreadData data = new ServerThreadData();
                //Initialisieren
                data.Init(st, m_Config.SoundOutputDeviceNameServer, m_Config.SamplesPerSecondServer, m_Config.BitsPerSampleServer, m_Config.ChannelsServer, m_SoundBufferCount, m_Config.JitterBufferCountServer, m_Milliseconds);
                //Hinzufügen
                m_DictionaryServerDatas[st] = data;
                //Konfiguration senden

                byte[] nameLengthBytes = new byte[4];
                st.Client.Client.Receive(nameLengthBytes);
                int nameLength = BitConverter.ToInt32(nameLengthBytes, 0);

                // Read the length of the photo
                byte[] photoLengthBytes = new byte[4];
                st.Client.Client.Receive(photoLengthBytes);
                int photoLength = BitConverter.ToInt32(photoLengthBytes, 0);

                // Read the name
                byte[] nameBytes = new byte[nameLength];
                st.Client.Client.Receive(nameBytes);
                string clientName = Encoding.UTF8.GetString(nameBytes);

                // Read the photo
                byte[] clientPhoto = new byte[photoLength];
                st.Client.Client.Receive(clientPhoto);

                App.Current.Dispatcher.Invoke(() =>
                {
                    AddUpdateListBox(clientName, clientPhoto);
                });


                SendConfigurationToClient(data);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Take as Parametres Server Thread and Send To Client Configuration About Connection ( Protocol And Samples Format )
        private void SendConfigurationToClient(ServerThreadData data)
        {
            Byte[] bytesConfig = m_Encoding.GetBytes(String.Format("SamplesPerSecond:{0}", m_Config.SamplesPerSecondServer));
            data.ServerThread.Send(m_PrototolClient.ToBytes(bytesConfig));
        }
        private void ButtonServer_Click(object sender, EventArgs e)
        {
            try
            {
                // Daten holen
                FormToConfig();

                if (IsServerRunning)
                {
                    StopServer();
                    StopRecordingFromSounddevice_Server();
                    StopTimerMixed();
                }
                else
                {
                    StartServer();

                    //Wenn aktiv
                    if (m_Config.ServerNoSpeakAll == false)
                    {
                        StartRecordingFromSounddevice_Server();
                    }

                    StartTimerMixed();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void StopTimerMixed()
        {
            if (m_TimerMixed != null)
            {
                m_TimerMixed.Stop();
                m_TimerMixed.TimerTick -= new WinSound.EventTimer.DelegateTimerTick(OnTimerSendMixedDataToAllClients);
                m_TimerMixed = null;
            }
        }
        private void StopRecordingFromSounddevice_Server()
        {
            try
            {
                if (IsRecorderFromSounddeviceStarted_Server)
                {
                    //Stoppen
                    m_Recorder_Server.Stop();

                    //Events entfernen
                    m_Recorder_Server.DataRecorded -= new WinSound.Recorder.DelegateDataRecorded(OnDataReceivedFromSoundcard_Server);
                    m_Recorder_Server.RecordingStopped -= new WinSound.Recorder.DelegateStopped(OnRecordingStopped_Server);
                    m_Recorder_Server = null;

                    //JitterBuffer beenden
                    m_JitterBufferServerRecording.Stop();

                    //Anzeigen

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StopServer()
        {
            try
            {
                if (IsServerRunning == true)
                {

                    //Player beenden
                    DeleteAllServerThreadDatas();

                    //Server beenden
                    m_Server.Stop();
                    m_Server.ClientConnected -= new NF.TCPServer.DelegateClientConnected(OnServerClientConnected);
                    m_Server.ClientDisconnected -= new NF.TCPServer.DelegateClientDisconnected(OnServerClientDisconnected);
                    m_Server.DataReceived -= new NF.TCPServer.DelegateDataReceived(OnServerDataReceived);
                }

                //Je nach Server Status
                if (m_Server != null)
                {
                    if (m_Server.State == NF.TCPServer.ListenerState.Started)
                    {
                        //ShowServerStarted();
                    }
                    else
                    {
                        //ShowServerStopped();
                    }
                }

                //Fertig
                m_Server = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DeleteAllServerThreadDatas()
        {
            lock (LockerDictionary)
            {
                try
                {
                    foreach (ServerThreadData info in m_DictionaryServerDatas.Values)
                    {
                        info.Dispose();
                    }
                    m_DictionaryServerDatas.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private bool UseJitterBufferServerRecording
        {
            get
            {
                return m_Config.UseJitterBufferServerRecording;
            }
        }

        private bool IsRecorderFromSounddeviceStarted_Server
        {
            get
            {
                if (m_Recorder_Server != null)
                {
                    return m_Recorder_Server.Started;
                }
                return false;
            }
        }
        private void StartRecordingFromSounddevice_Server()
        {
            try
            {
                if (IsRecorderFromSounddeviceStarted_Server == false)
                {
                    //Buffer Grösse berechnen
                    int bufferSize = 0;
                    if (UseJitterBufferServerRecording)
                    {
                        bufferSize = WinSound.Utils.GetBytesPerInterval((uint)m_Config.SamplesPerSecondServer, m_Config.BitsPerSampleServer, m_Config.ChannelsServer) * (int)m_RecorderFactor;
                    }
                    else
                    {
                        bufferSize = WinSound.Utils.GetBytesPerInterval((uint)m_Config.SamplesPerSecondServer, m_Config.BitsPerSampleServer, m_Config.ChannelsServer);
                    }

                    //Wenn Buffer korrekt
                    if (bufferSize > 0)
                    {
                        //Recorder erstellen
                        m_Recorder_Server = new WinSound.Recorder();

                        //Events hinzufügen
                        m_Recorder_Server.DataRecorded += new WinSound.Recorder.DelegateDataRecorded(OnDataReceivedFromSoundcard_Server);
                        m_Recorder_Server.RecordingStopped += new WinSound.Recorder.DelegateStopped(OnRecordingStopped_Server);

                        //Recorder starten
                        if (m_Recorder_Server.Start(m_Config.SoundInputDeviceNameServer, m_Config.SamplesPerSecondServer, m_Config.BitsPerSampleServer, m_Config.ChannelsServer, m_SoundBufferCount, bufferSize))
                        {
                            //Anzeigen


                            //Zu Mixer hinzufügen
                            CallWindowServer.DictionaryMixed[this] = new Queue<List<byte>>();

                            //JitterBuffer starten
                            m_JitterBufferServerRecording.Start();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnDataReceivedFromSoundcard_Server(Byte[] data)
        {
            try
            {
                lock (this)
                {
                    if (IsServerRunning)
                    {
                        //Wenn Form noch aktiv
                        if (m_IsFormMain)
                        {
                            //Wenn gewünscht
                            if (m_Config.ServerNoSpeakAll == false)
                            {
                                //Sounddaten in kleinere Einzelteile zerlegen
                                int bytesPerInterval = WinSound.Utils.GetBytesPerInterval((uint)m_Config.SamplesPerSecondServer, m_Config.BitsPerSampleServer, m_Config.ChannelsServer);
                                int count = data.Length / bytesPerInterval;
                                int currentPos = 0;
                                for (int i = 0; i < count; i++)
                                {
                                    //Teilstück in RTP Packet umwandeln
                                    Byte[] partBytes = new Byte[bytesPerInterval];
                                    Array.Copy(data, currentPos, partBytes, 0, bytesPerInterval);
                                    currentPos += bytesPerInterval;

                                    //Wenn Buffer nicht zu gross
                                    Queue<List<Byte>> q = CallWindowServer.DictionaryMixed[this];
                                    if (q.Count < 10)
                                    {
                                        //Daten In Mixer legen
                                        q.Enqueue(new List<Byte>(partBytes));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void OnRecordingStopped_Server()
        {
            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Code to update the UI goes here

                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        // HERE IP 
        private bool FormToConfig()
        {
            try
            {
                m_Config.IpAddressClient = "26.245.118.136";
                m_Config.IPAddressServer = "26.114.170.202";
                m_Config.PortClient = 8888;
                m_Config.PortServer = 8888;
                m_Config.SoundInputDeviceNameClient = Mic.SelectedIndex >= 0 ? Mic.SelectedItem.ToString() : "";
                m_Config.SoundOutputDeviceNameClient = Sound.SelectedIndex >= 0 ? Sound.SelectedItem.ToString() : "";
                m_Config.SoundInputDeviceNameServer = Mic.SelectedIndex >= 0 ? Mic.SelectedItem.ToString() : "";
                m_Config.SoundOutputDeviceNameServer = Sound.SelectedIndex >= 0 ? Sound.SelectedItem.ToString() : "";
                m_Config.JitterBufferCountServer = 20;
                m_Config.JitterBufferCountClient = 20;
                m_Config.SamplesPerSecondServer = 22050;
                m_Config.BitsPerSampleServer = 16;
                m_Config.BitsPerSampleClient = 16;
                m_Config.ChannelsServer = 1;
                m_Config.ChannelsClient = 1;
                m_Config.UseJitterBufferClientRecording = true;
                m_Config.UseJitterBufferServerRecording = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //MessageBox.Show(ex.Message, "Fehler bei der Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void StartTimerMixed()
        {
            if (m_TimerMixed == null)
            {
                m_TimerMixed = new WinSound.EventTimer();
                m_TimerMixed.TimerTick += new WinSound.EventTimer.DelegateTimerTick(OnTimerSendMixedDataToAllClients);
                m_TimerMixed.Start(20, 0);
            }
        }

        private void OnTimerSendMixedDataToAllClients()
        {
            try
            {
                //Liste mit allen Sprachdaten (eigene + Clients)
                Dictionary<Object, List<Byte>> dic = new Dictionary<object, List<byte>>();
                List<List<byte>> listlist = new List<List<byte>>();
                Dictionary<Object, Queue<List<Byte>>> copy = new Dictionary<object, Queue<List<byte>>>(CallWindowServer.DictionaryMixed);
                {
                    Queue<List<byte>> q = null;
                    foreach (Object obj in copy.Keys)
                    {

                        q = copy[obj];

                        //Wenn Daten vorhanden
                        if (q.Count > 0)
                        {
                            dic[obj] = q.Dequeue();
                            listlist.Add(dic[obj]);
                        }
                    }
                }

                if (listlist.Count > 0)
                {
                    //Gemischte Sprachdaten
                    Byte[] mixedBytes = WinSound.Mixer.MixBytes(listlist, m_Config.BitsPerSampleServer).ToArray();
                    List<Byte> listMixed = new List<Byte>(mixedBytes);

                    //Für alle Clients
                    foreach (NF.ServerThread client in m_Server.Clients)
                    {
                        //Wenn nicht stumm
                        if (client.IsMute == false)
                        {
                            //Gemixte Sprache für Client
                            Byte[] mixedBytesClient = mixedBytes;

                            if (dic.ContainsKey(client))
                            {
                                //Sprache des Clients ermitteln
                                List<Byte> listClient = dic[client];

                                //Sprache des Clients aus Mix subtrahieren
                                mixedBytesClient = WinSound.Mixer.SubsctractBytes_16Bit(listMixed, listClient).ToArray();
                            }

                            //RTP Packet erstellen
                            WinSound.RTPPacket rtp = ToRTPPacket(mixedBytesClient, m_Config.BitsPerSampleServer, m_Config.ChannelsServer);
                            Byte[] rtpBytes = rtp.ToBytes();

                            //Absenden
                            client.Send(m_PrototolClient.ToBytes(rtpBytes));

                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                // Console.WriteLine(String.Format("FormMain.cs | OnTimerSendMixedDataToAllClients() | {0}", ex.Message));
                m_TimerProgressBarPlayingClient.Stop();
            }
        }

        private Byte[] ToRTPData(Byte[] data, int bitsPerSample, int channels)
        {
            //Neues RTP Packet erstellen
            WinSound.RTPPacket rtp = ToRTPPacket(data, bitsPerSample, channels);
            //RTPHeader in Bytes erstellen
            Byte[] rtpBytes = rtp.ToBytes();
            //Fertig
            return rtpBytes;
        }
        /// <summary>
        /// ToRTPPacket
        /// </summary>
        /// <param name="linearData"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        private WinSound.RTPPacket ToRTPPacket(Byte[] linearData, int bitsPerSample, int channels)
        {
            //Daten Nach MuLaw umwandeln
            Byte[] mulaws = WinSound.Utils.LinearToMulaw(linearData, bitsPerSample, channels);

            //Neues RTP Packet erstellen
            WinSound.RTPPacket rtp = new WinSound.RTPPacket();

            //Werte übernehmen
            rtp.Data = mulaws;
            rtp.CSRCCount = 0;
            rtp.Extension = false;
            rtp.HeaderLength = WinSound.RTPPacket.MinHeaderLength;
            rtp.Marker = false;
            rtp.Padding = false;
            rtp.PayloadType = 0;
            rtp.Version = 2;
            rtp.SourceId = 0;

            //RTP Header aktualisieren
            try
            {
                rtp.SequenceNumber = Convert.ToUInt16(m_SequenceNumber);
                m_SequenceNumber++;
            }
            catch (Exception)
            {
                m_SequenceNumber = 0;
            }
            try
            {
                rtp.Timestamp = Convert.ToUInt32(m_TimeStamp);
                m_TimeStamp += mulaws.Length;
            }
            catch (Exception)
            {
                m_TimeStamp = 0;
            }

            //Fertig
            return rtp;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                //Daten holen
                FormToConfig();

                if (IsClientConnected)
                {
                    DisconnectClient();
                    StopRecordingFromSounddevice_Client();
                }
                else
                {
                    ConnectClient();
                }

                //Kurz warten
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void OnClosing()
        {
            try
            {
                //Form ist geschlossen
                m_IsFormMain = false;

                //Aufnahme beenden
                StopRecordingFromSounddevice_Server();
                //Streamen von Sounddevice beenden
                StopRecordingFromSounddevice_Client();
                //Client beenden
                // HERE
                // DisconnectClient();
                //Server beenden
                StopServer();

                m_Player.Close();
                //Speichern
                //SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void InitComboboxes()
        {
            InitComboboxesClient();
            InitComboboxesServer();
        }
        private void InitComboboxesClient()
        {
            Sound.Items.Clear();
            Mic.Items.Clear();
            List<String> playbackNames = WinSound.WinSound.GetPlaybackNames();
            List<String> recordingNames = WinSound.WinSound.GetRecordingNames();

            //Output
            Sound.Items.Add("None");
            foreach (String name in playbackNames.Where(x => x != null))
            {
                Sound.Items.Add(name);
            }
            //Input
            foreach (String name in recordingNames.Where(x => x != null))
            {
                Mic.Items.Add(name);
            }

            //Output
            if (Sound.Items.Count > 0)
            {
                Sound.SelectedIndex = 0;
            }
            //Input
            if (Mic.Items.Count > 0)
            {
                Mic.SelectedIndex = 0;
            }
        }
        /// <summary>
        /// InitComboboxesServer
        /// </summary>
        private void InitComboboxesServer()
        {
            Sound.Items.Clear();
            Mic.Items.Clear();
            List<String> playbackNames = WinSound.WinSound.GetPlaybackNames();
            List<String> recordingNames = WinSound.WinSound.GetRecordingNames();

            //Output
            foreach (String name in playbackNames.Where(x => x != null))
            {
                Sound.Items.Add(name);
            }
            //Input
            foreach (String name in recordingNames.Where(x => x != null))
            {
                Mic.Items.Add(name);
            }

            //Output
            if (Sound.Items.Count > 0)
            {
                Sound.SelectedIndex = 0;
            }
            //Input
            if (Mic.Items.Count > 0)
            {
                Mic.SelectedIndex = 0;
            }
        }
        private void ConnectClient()
        {
            try
            {
                if (IsClientConnected == false)
                {
                    //Wenn Eingabe vorhanden
                    if (m_Config.IpAddressClient.Length > 0 && m_Config.PortClient > 0)
                    {
                        m_Client = new NF.TCPClient(m_Config.IpAddressClient, m_Config.PortClient);
                        m_Client.ClientConnected += new NF.TCPClient.DelegateConnection(OnClientConnected);
                        m_Client.ClientDisconnected += new NF.TCPClient.DelegateConnection(OnClientDisconnected);
                        m_Client.ExceptionAppeared += new NF.TCPClient.DelegateException(OnClientExceptionAppeared);
                        m_Client.DataReceived += new NF.TCPClient.DelegateDataReceived(OnClientDataReceived);
                        m_Client.Connect();



                        MessageBox.Show("Connect Good");
                    }
                }
            }
            catch (Exception ex)
            {
                m_Client = null;
                MessageBox.Show(ex.Message);
            }
        }

        private void StopRecordingFromSounddevice_Client()
        {
            try
            {
                if (IsRecorderFromSounddeviceStarted_Client)
                {
                    //Stoppen
                    m_Recorder_Client.Stop();

                    //Events entfernen
                    m_Recorder_Client.DataRecorded -= new WinSound.Recorder.DelegateDataRecorded(OnDataReceivedFromSoundcard_Client);
                    m_Recorder_Client.RecordingStopped -= new WinSound.Recorder.DelegateStopped(OnRecordingStopped_Client);
                    m_Recorder_Client = null;

                    //Wenn JitterBuffer
                    if (UseJitterBufferClientRecording)
                    {
                        m_JitterBufferClientRecording.Stop();
                    }

                    //Anzeigen
                    //ShowStreamingFromSounddeviceStopped_Client();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private bool IsRecorderFromSounddeviceStarted_Client
        {
            get
            {
                if (m_Recorder_Client != null)
                {
                    return m_Recorder_Client.Started;
                }
                return false;
            }
        }

        private void OnRecordingStopped_Client()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Code to update the UI goes here
                    //ShowStreamingFromSounddeviceStopped_Client();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        private void OnDataReceivedFromSoundcard_Client(Byte[] data)
        {
            try
            {
                lock (this)
                {
                    if (IsClientConnected)
                    {
                        //Wenn gewünscht
                        if (m_Config.ClientNoSpeakAll == false)
                        {
                            //Sounddaten in kleinere Einzelteile zerlegen
                            int bytesPerInterval = WinSound.Utils.GetBytesPerInterval((uint)m_Config.SamplesPerSecondClient, m_Config.BitsPerSampleClient, m_Config.ChannelsClient);
                            int count = data.Length / bytesPerInterval;
                            int currentPos = 0;
                            for (int i = 0; i < count; i++)
                            {
                                //Teilstück in RTP Packet umwandeln
                                Byte[] partBytes = new Byte[bytesPerInterval];
                                Array.Copy(data, currentPos, partBytes, 0, bytesPerInterval);
                                currentPos += bytesPerInterval;
                                WinSound.RTPPacket rtp = ToRTPPacket(partBytes, m_Config.BitsPerSampleClient, m_Config.ChannelsClient);

                                //Wenn JitterBuffer
                                if (UseJitterBufferClientRecording)
                                {
                                    //In Buffer legen
                                    m_JitterBufferClientRecording.AddData(rtp);
                                }
                                else
                                {
                                    //Alles in RTP Packet umwandeln
                                    Byte[] rtpBytes = ToRTPData(data, m_Config.BitsPerSampleClient, m_Config.ChannelsClient);
                                    //Absenden
                                    m_Client.Send(m_PrototolClient.ToBytes(rtpBytes));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                // System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private bool UseJitterBufferClientRecording
        {
            get
            {
                return m_Config.UseJitterBufferClientRecording;
            }
        }
        private void DisconnectClient()
        {
            try
            {
                //Aufnahme beenden
                StopRecordingFromSounddevice_Client();

                if (m_Client != null)
                {
                    //Client beenden
                    m_Client.Disconnect();
                    m_Client.ClientConnected -= new NF.TCPClient.DelegateConnection(OnClientConnected);
                    m_Client.ClientDisconnected -= new NF.TCPClient.DelegateConnection(OnClientDisconnected);
                    m_Client.ExceptionAppeared -= new NF.TCPClient.DelegateException(OnClientExceptionAppeared);
                    m_Client.DataReceived -= new NF.TCPClient.DelegateDataReceived(OnClientDataReceived);
                    m_Client = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void OnClientDisconnected(NF.TCPClient client, string info)
        {
            try
            {
                //Abspielen beenden
                StopPlayingToSounddevice_Client();
                //Streamen von Sounddevice beenden
                StopRecordingFromSounddevice_Client();

                if (m_Client != null)
                {
                    m_Client.ClientConnected -= new NF.TCPClient.DelegateConnection(OnClientConnected);
                    m_Client.ClientDisconnected -= new NF.TCPClient.DelegateConnection(OnClientDisconnected);
                    m_Client.ExceptionAppeared -= new NF.TCPClient.DelegateException(OnClientExceptionAppeared);
                    m_Client.DataReceived -= new NF.TCPClient.DelegateDataReceived(OnClientDataReceived);
                    //ShowMessage(LabelClient, String.Format("Client disconnected {0}", ""));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void OnClientDataReceived(NF.TCPClient client, Byte[] bytes)
        {
            try
            {
                if (m_PrototolClient != null)
                {
                    m_PrototolClient.Receive_LH(client, bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void OnClientExceptionAppeared(NF.TCPClient client, Exception ex)
        {
            DisconnectClient();
            //ShowError(LabelClient, ex.Message);
        }
        private void StopPlayingToSounddevice_Client()
        {
            if (m_PlayerClient != null)
            {
                m_PlayerClient.Close();
                m_PlayerClient = null;
            }

            //JitterBuffer beenden
            if (m_JitterBufferClientPlaying != null)
            {
                m_JitterBufferClientPlaying.Stop();
            }

            //Timer beenden
            m_TimerProgressBarPlayingClient.Stop();

            //Anzeigen
            Application.Current.Dispatcher.Invoke(() =>
            {
                //ComboboxOutputSoundDeviceNameClient.IsEnabled = true;
                //NumericUpDownJitterBufferClient.IsEnabled = true;
                //ProgressBarPlayingClient.Value = 0;
            });
        }

        private void InitTimerShowProgressBarPlayingClient()
        {
            m_TimerProgressBarPlayingClient = new DispatcherTimer();
            m_TimerProgressBarPlayingClient.Interval = TimeSpan.FromMilliseconds(60);
            m_TimerProgressBarPlayingClient.Tick += new EventHandler(OnTimerProgressPlayingClient);
        }
        private void OnClientConnected(NF.TCPClient client, string info)
        {
            // ShowMessage(LabelClient, String.Format("Client connected {0}", ""));
            // ShowClientConnected();
        }
        private void OnTimerProgressPlayingClient(Object obj, EventArgs e)
        {
            try
            {
                if (m_PlayerClient != null)
                {
                    //ProgressBarPlayingClient.Value = Math.Min(m_JitterBufferClientPlaying.Length, ProgressBarPlayingClient.Maximum);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                // Console.WriteLine(String.Format("FormMain.cs | OnTimerProgressPlayingClient() | {0}", ex.Message));
                m_TimerProgressBarPlayingClient.Stop();
            }
        }
        private bool IsClientConnected
        {
            get
            {
                if (m_Client != null)
                {
                    return m_Client.Connected;
                }
                return false;
            }
        }
        public class Configuration
        {
            /// <summary>
            /// Config
            /// </summary>
            public Configuration()
            {

            }

            //Attribute
            public String IpAddressClient = "26.245.118.136";
            public String IPAddressServer = "26.114.170.202";
            public int PortClient = 0;
            public int PortServer = 0;
            public String SoundInputDeviceNameClient = "";
            public String SoundOutputDeviceNameClient = "";
            public String SoundInputDeviceNameServer = "";
            public String SoundOutputDeviceNameServer = "";
            public int SamplesPerSecondClient = 8000;
            public int BitsPerSampleClient = 16;
            public int ChannelsClient = 1;
            public int SamplesPerSecondServer = 8000;
            public int BitsPerSampleServer = 16;
            public int ChannelsServer = 1;
            public bool UseJitterBufferClientRecording = true;
            public bool UseJitterBufferServerRecording = true;
            public uint JitterBufferCountServer = 20;
            public uint JitterBufferCountClient = 20;
            public string FileName = "";
            public bool LoopFile = false;
            public bool MuteClientPlaying = false;
            public bool ServerNoSpeakAll = false;
            public bool ClientNoSpeakAll = false;
            public bool MuteServerListen = false;
        }

        public class ServerThreadData
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public ServerThreadData()
            {

            }

            //Attribute
            public NF.ServerThread ServerThread;
            public WinSound.Player Player;
            public WinSound.Buffer JitterBuffer;
            public WinSound.Protocol Protocol;
            public int SamplesPerSecond = 8000;
            public int BitsPerSample = 16;
            public int SoundBufferCount = 8;
            public uint JitterBufferCount = 20;
            public uint JitterBufferMilliseconds = 20;
            public int Channels = 1;
            private bool IsInitialized = false;
            public bool IsMute = false;
            public static bool IsMuteAll = false;

            /// <summary>
            /// Init
            /// </summary>
            /// <param name="bitsPerSample"></param>
            /// <param name="channels"></param>
            public void Init(NF.ServerThread st, string soundDeviceName, int samplesPerSecond, int bitsPerSample, int channels, int soundBufferCount, uint jitterBufferCount, uint jitterBufferMilliseconds)
            {
                //Werte übernehmen
                this.ServerThread = st;
                this.SamplesPerSecond = samplesPerSecond;
                this.BitsPerSample = bitsPerSample;
                this.Channels = channels;
                this.SoundBufferCount = soundBufferCount;
                this.JitterBufferCount = jitterBufferCount;
                this.JitterBufferMilliseconds = jitterBufferMilliseconds;

                //Player
                this.Player = new WinSound.Player();
                this.Player.Open(soundDeviceName, samplesPerSecond, bitsPerSample, channels, soundBufferCount);

                //Wenn ein JitterBuffer verwendet werden soll
                if (jitterBufferCount >= 2)
                {
                    //Neuen JitterBuffer erstellen
                    this.JitterBuffer = new WinSound.Buffer(st, jitterBufferCount, jitterBufferMilliseconds);
                    this.JitterBuffer.DataAvailable += new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferDataAvailable);
                    this.JitterBuffer.Start();
                }

                //Protocol
                this.Protocol = new WinSound.Protocol(WinSound.ProtocolTypes.LH, Encoding.Default);
                this.Protocol.DataComplete += new WinSound.Protocol.DelegateDataComplete(OnProtocolDataComplete);

                //Zu Mixer hinzufügen
                CallWindowServer.DictionaryMixed[st] = new Queue<List<byte>>();

                //Initialisiert
                IsInitialized = true;
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                //Protocol
                if (Protocol != null)
                {
                    this.Protocol.DataComplete -= new WinSound.Protocol.DelegateDataComplete(OnProtocolDataComplete);
                    this.Protocol = null;
                }

                //JitterBuffer
                if (JitterBuffer != null)
                {
                    JitterBuffer.Stop();
                    JitterBuffer.DataAvailable -= new WinSound.Buffer.DelegateDataAvailable(OnJitterBufferDataAvailable);
                    this.JitterBuffer = null;
                }

                //Player
                if (Player != null)
                {
                    Player.Close();
                    this.Player = null;
                }
                m_Player.Close();
                //Nicht initialisiert
                IsInitialized = false;
            }

            /// <summary>
            /// OnProtocolDataComplete
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="data"></param>
            private void OnProtocolDataComplete(Object sender, Byte[] bytes)
            {
                //Wenn initialisiert
                if (IsInitialized)
                {
                    if (ServerThread != null && Player != null)
                    {
                        try
                        {
                            //Wenn der Player gestartet wurde
                            if (Player.Opened)
                            {

                                //RTP Header auslesen
                                WinSound.RTPPacket rtp = new WinSound.RTPPacket(bytes);

                                //Wenn Header korrekt
                                if (rtp.Data != null)
                                {
                                    //Wenn JitterBuffer verwendet werden soll
                                    if (JitterBuffer != null && JitterBuffer.Maximum >= 2)
                                    {
                                        JitterBuffer.AddData(rtp);
                                    }
                                    else
                                    {
                                        //Wenn kein Mute
                                        if (IsMuteAll == false && IsMute == false)
                                        {
                                            //Nach Linear umwandeln
                                            Byte[] linearBytes = WinSound.Utils.MuLawToLinear(rtp.Data, this.BitsPerSample, this.Channels);
                                            //Abspielen
                                            Player.PlayData(linearBytes, false);
                                        }
                                    }
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            IsInitialized = false;
                        }
                    }
                }
            }

            /// <summary>
            /// OnJitterBufferDataAvailable
            /// </summary>
            /// <param name="packet"></param>
            private void OnJitterBufferDataAvailable(Object sender, WinSound.RTPPacket rtp)
            {
                try
                {
                    if (Player != null)
                    {
                        //Nach Linear umwandeln
                        Byte[] linearBytes = WinSound.Utils.MuLawToLinear(rtp.Data, BitsPerSample, Channels);

                        //Wenn kein Mute
                        if (IsMuteAll == false && IsMute == false)
                        {
                            //Abspielen
                            Player.PlayData(linearBytes, false);
                        }

                        //Wenn Buffer nicht zu gross
                        Queue<List<Byte>> q = CallWindowServer.DictionaryMixed[sender];
                        if (q.Count < 10)
                        {
                            //Daten Zu Mixer hinzufügen
                            CallWindowServer.DictionaryMixed[sender].Enqueue(new List<Byte>(linearBytes));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    // Console.WriteLine(String.Format("MainWindow.cs | OnJitterBufferDataAvailable() | {0}", ex.Message));
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            OnClosing();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            m_Player.StartPause();

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            m_Player.EndPause();
            //m_Player.PlayData();
        }
    }
}
