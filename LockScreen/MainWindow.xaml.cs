using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.System;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.Media.Capture.Frames;
using ZXing;
using ZXing.Windows.Compatibility;
using System.Drawing;
using Microsoft.UI.Input;
using Windows.UI.Core.Preview;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LockScreen
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool WindowHandled = false;
        public MainWindow()
        {
            this.InitializeComponent();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId WndID = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appW = AppWindow.GetFromWindowId(WndID);
            OverlappedPresenter presenter = appW.Presenter as OverlappedPresenter;
            presenter.IsAlwaysOnTop = true;
            appW.SetPresenter(AppWindowPresenterKind.FullScreen);
            this.Closed += MainWindow_Closed;

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (((Button)sender).Name == "ButtonPassword")
            {

                passwordBox.Visibility = Visibility.Visible;
                ButtonQRCode.Visibility = Visibility.Collapsed;
                ButtonPassword.Visibility = Visibility.Collapsed;
                OkButton.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                return;
            }
            else if (((Button)sender).Name == "ButtonQRCode")
            {
                ButtonQRCode.Visibility = Visibility.Collapsed;
                OkButton.Visibility = Visibility.Visible;
                CaptureElementPanel.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                ButtonPassword.Visibility = Visibility.Collapsed;
                StartCaptureElement();
                return;
            }

            if (passwordBox.Password != "123")
            {
                statusText.Text = "The password is incorrect. Try Again.";
            }
            else
            {
                WindowHandled = false;
                this.Close();
            }

        }

        private MediaFrameSourceGroup mediaFrameSourceGroup;
        private MediaCapture mediaCapture;
        private bool isScanning = false;
        private async void StartCaptureElement()
        {

            var groups = await MediaFrameSourceGroup.FindAllAsync();

            if (groups.Count == 0)
            {
                statusText.Text = "No camera devices found.";
                return;
            }

            mediaFrameSourceGroup = groups.First();

            mediaCapture = new MediaCapture();
            var mediaCaptureInitializationSettings = new MediaCaptureInitializationSettings()
            {
                SourceGroup = this.mediaFrameSourceGroup,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Auto
            };
            await mediaCapture.InitializeAsync(mediaCaptureInitializationSettings);

            var frameSource = mediaCapture.FrameSources[this.mediaFrameSourceGroup.SourceInfos[0].Id];
            captureElement.Source = Windows.Media.Core.MediaSource.CreateFromMediaFrameSource(frameSource);


            StartScanning();
        }

        private async void StartScanning()
        {

            isScanning = true;
            statusText.Text = "Scanning...";

            while (isScanning)
            {

                var imgFormat = ImageEncodingProperties.CreateJpeg();
                var stream = new InMemoryRandomAccessStream();
                await mediaCapture.CapturePhotoToStreamAsync(imgFormat, stream);
                stream.Seek(0);
                BitmapImage bmpImage = new BitmapImage();
                await bmpImage.SetSourceAsync(stream);



                try
                {

                    byte[] byteArray;
                    using (DataReader dataReader = new DataReader(stream.GetInputStreamAt(0)))
                    {
                        byteArray = new byte[stream.Size];
                        await dataReader.LoadAsync((uint)stream.Size);
                        dataReader.ReadBytes(byteArray);
                    }

                    using (MemoryStream memoryStream = new MemoryStream(byteArray))
                    {

                        Bitmap bitmap = new Bitmap(memoryStream);
                        BarcodeReader reader = new BarcodeReader { AutoRotate = true, Options = { TryInverted = true } };

                        Result result = reader.Decode(bitmap);

                        if (result != null)
                        {
                            string decoded = result.ToString().Trim();

                            if (decoded == "123")
                            {
                                StopScanningButton_Click(null, null);

                                WindowHandled = false;

                                this.Close();
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }

                await Task.Delay(2000);
            }


        }
        private void StopScanningButton_Click(object sender, RoutedEventArgs e)
        {
            if (isScanning)
            {
                isScanning = false;
                mediaCapture.Dispose();
                captureElement.Source = null;

            }
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            StopScanningButton_Click(sender, e);
            statusText.Text = "";
            OkButton.Visibility = Visibility.Collapsed;
            CaptureElementPanel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
            passwordBox.Visibility = Visibility.Collapsed;
            ButtonQRCode.Visibility = Visibility.Visible;
            ButtonPassword.Visibility = Visibility.Visible;
        }

        private void MainGrid_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            WindowHandled = true;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            args.Handled = WindowHandled;
        }




    }
}
