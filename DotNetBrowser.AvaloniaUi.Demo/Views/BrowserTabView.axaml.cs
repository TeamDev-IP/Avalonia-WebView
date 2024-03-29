#region Copyright

// Copyright © 2024, TeamDev. All rights reserved.
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
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DotNetBrowser.AvaloniaUi.Demo.TabModels;
using DotNetBrowser.Browser;
using DotNetBrowser.Handlers;
using DotNetBrowser.Input;
using DotNetBrowser.Input.Keyboard.Events;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

// ReSharper disable UnusedParameter.Local

namespace DotNetBrowser.AvaloniaUi.Demo.Views
{
    public partial class BrowserTabView : UserControl
    {
        public BrowserTabModel Model => DataContext as BrowserTabModel;

        public BrowserTabView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void AddressBarKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Model?.LoadUrl(AddressBar.Text);
                e.Handled = true;
            }
        }

        private void FullScreen(object sender, RoutedEventArgs e)
        {
            FullScreenWindow fullScreenWindow = new FullScreenWindow();
            fullScreenWindow.Opened += (_, __) =>
            {
                Model.Browser.Focus();
            };
            fullScreenWindow.Closed += (s, e1) =>
            {
                Model.Browser.Keyboard.KeyPressed.Handler = null;
                View.InitializeFrom(Model.Browser);
                View.IsVisible = true;
            };

            Model.Browser.Keyboard.KeyPressed.Handler =
                new Handler<IKeyPressedEventArgs, InputEventResponse>(p =>
                {
                    if (p.VirtualKey is KeyCode.F11 or KeyCode.Escape)
                    {
                        Dispatcher.UIThread.InvokeAsync(() => fullScreenWindow.Close());
                    }

                    return InputEventResponse.Proceed;
                });
            View.IsVisible = false;
            fullScreenWindow.View.InitializeFrom(Model.Browser);

            fullScreenWindow.Show();
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if (Model != null)
            {
                View.InitializeFrom(Model.Browser);
                Model.StatesUpdated += OnStatesUpdated;
                DataContextChanged -= OnDataContextChanged;
            }
        }

        private void OnMenuButtonClick(object sender, RoutedEventArgs e)
        {
            Menu.ContextMenu?.Open();
        }

        private void OnStatesUpdated(object sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() => AddressBar.Text = Model?.Browser.Url);
        }

        private void PrintToPdf(object sender, RoutedEventArgs e)
        {
            IStorageProvider provider = TopLevel.GetTopLevel(this)?.StorageProvider;

            provider?.SaveFilePickerAsync(new FilePickerSaveOptions
                      {
                          Title = "Select where to save the PDF file",
                          DefaultExtension = "pdf",
                          FileTypeChoices = new[] { FilePickerFileTypes.Pdf },
                      })
                     .ContinueWith(async t =>
                      {
                          string file = t.Result?.Path.LocalPath;
                          if (!string.IsNullOrWhiteSpace(file) && Model != null)
                          {
                              return await Model.PrintToPdf(file);
                          }

                          return string.Empty;
                      }, TaskScheduler.FromCurrentSynchronizationContext())
                     .ContinueWith(async t1 =>
                      {
                          string pdf = await t1.Result;
                          if (!string.IsNullOrWhiteSpace(pdf))
                          {
                              Model?.LoadUrl(pdf);
                          }
                      }, default, TaskContinuationOptions.OnlyOnRanToCompletion,
                      TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void TakeScreenshot(object sender, RoutedEventArgs e)
        {
            IStorageProvider provider = TopLevel.GetTopLevel(this)?.StorageProvider;

            provider?.SaveFilePickerAsync(new FilePickerSaveOptions
                      {
                          Title = "Select where to save the screenshot",
                          DefaultExtension = "png",
                          FileTypeChoices = new[] { FilePickerFileTypes.ImagePng },
                      })
                     .ContinueWith(t =>
                      {
                          string file = t.Result?.Path.LocalPath;
                          if (!string.IsNullOrWhiteSpace(file))
                          {
                              Model?.TakeScreenshot(file);
                          }
                      }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}