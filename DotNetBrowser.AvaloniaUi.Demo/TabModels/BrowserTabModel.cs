#region Copyright

// Copyright © 2023, TeamDev. All rights reserved.
// 
// Redistribution and use in source and/or binary forms, with or without
// modification, must retain the above copyright notice and the following
// disclaimer.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media;
using DotNetBrowser.Browser;
using DotNetBrowser.Browser.Handlers;
using DotNetBrowser.Engine;
using DotNetBrowser.Handlers;
using DotNetBrowser.Print.Handlers;
using SkiaSharp;

namespace DotNetBrowser.AvaloniaUi.Demo.TabModels
{
    public class BrowserTabModel : INotifyPropertyChanged
    {
        private readonly Action<BrowserTabModel> action;
        private readonly string url;
        private IBrowser browser;
        private IImage favicon;
        private string header;
        private bool scrollbarsHidden;
        private string status;

        public IBrowser Browser
        {
            get => browser;
            set
            {
                browser = value;
                if (browser != null)
                {
                    browser.TitleChanged += (_, _) => Header = Browser.Title;
                    browser.StatusChanged += (_, e) => Status = e.Text;
                    browser.FaviconChanged +=
                        (_, e) => Favicon = e.NewFavicon.ToUiBitmap();
                    browser.Navigation.FrameLoadFinished += (_, _) => UpdateStates();
                    LoadUrl(url);
                }
            }
        }

        public bool CanGoBack => Browser?.Navigation.CanGoBack() ?? false;
        public bool CanGoForward => Browser?.Navigation.CanGoForward() ?? false;

        public IImage Favicon
        {
            get => favicon;
            set => SetField(ref favicon, value);
        }

        public string Header
        {
            get => header;
            private set => SetField(ref header, value);
        }

        public RenderingMode RenderingMode { get; set; }

        public bool ScrollbarsHidden
        {
            get => scrollbarsHidden;
            set => SetField(ref scrollbarsHidden, value);
        }

        public string Status
        {
            get => status;
            set => SetField(ref status, value);
        }

        public event EventHandler StatesUpdated;

        public event PropertyChangedEventHandler PropertyChanged;

        public BrowserTabModel(string header, string url, Action<BrowserTabModel> action)
        {
            this.url = url;
            this.action = action;
            Header = header;
        }

        public void LoadUrl(string newUrl)
        {
            Browser?.Navigation.LoadUrl(newUrl)
                    .ContinueWith(_ => { UpdateStates(); },
                                  TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void OnClose()
        {
            action(this);
            Browser?.Dispose();
        }

        public void OpenDevTools()
        {
            Browser?.DevTools.Show();
        }

        public void Print()
        {
            Browser?.MainFrame.Print();
        }

        public Task<string> PrintToPdf(string file)
        {
            IHandler<RequestPrintParameters, RequestPrintResponse> handler =
                Browser.RequestPrintHandler;
            Browser.RequestPrintHandler =
                new Handler<RequestPrintParameters, RequestPrintResponse>(
                     p => RequestPrintResponse.Print()
                    );
            TaskCompletionSource<string> whenCompleted = new();
            // Configure how the browser prints an HTML page.
            browser.PrintHtmlContentHandler =
                new Handler<PrintHtmlContentParameters, PrintHtmlContentResponse>(
                 p =>
                 {
                     // Use the PDF printer.
                     var printer = p.Printers.Pdf;
                     var job = printer.PrintJob;

                     // Set PDF file path.
                     job.Settings.PdfFilePath = file;

                     job.PrintCompleted += (_, _) =>
                     {
                         whenCompleted.SetResult(file);
                     };

                     Browser.RequestPrintHandler = handler;
                     // Proceed with printing using the PDF printer.
                     return PrintHtmlContentResponse.Print(printer);
                 });
            Print();
            return whenCompleted.Task;
        }

        public void TakeScreenshot(string fileName)
        {
            SKImage img = Browser?.TakeImage().ToSkImage();
            using FileStream stream = File.OpenWrite(Path.GetFullPath(fileName));
            SKData d = img?.Encode(SKEncodedImageFormat.Png, 100);
            d?.SaveTo(stream);
        }

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value,
                                   [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void UpdateStates()
        {
            if (!Browser.IsDisposed)
            {
                Header = Browser.Title;
                Favicon = Browser.Favicon.ToUiBitmap();
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
                StatesUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}