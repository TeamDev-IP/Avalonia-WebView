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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using DotNetBrowser.AvaloniaUi.Demo.TabModels;
using DotNetBrowser.AvaloniaUi.Demo.Views;
using DotNetBrowser.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;

namespace DotNetBrowser.AvaloniaUi.Demo
{
    public partial class MainWindow : Window
    {
        public BrowserTabsModel Model { get; set; }

        public MainWindow()
        {
            LoggerProvider.Instance.Level = SourceLevels.Verbose;
            LoggerProvider.Instance.FileLoggingEnabled = true;
            LoggerProvider.Instance.OutputFile = "dotnetbrowser.log";
            InitializeComponent();
            Model = new BrowserTabsModel();
            Model.AllTabsClosed += (_, _) => Close();
            Model.EngineCrashed += (_, e) => ShowError(e.Message, e.Title);
            Model.EngineInitFailed += (_, e) => ShowError(e.Message, e.Title);
            Model.NoLicenseFound += ShowNoLicenseMessage;
            Model.TabCreated += (_, model) => MainTabControl.SelectedItem = model;
            DataContext = Model;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Model.DisposeEngine();
        }

        private void ShowError(string message, string title)
        {
            MessageBoxCustomParams parameters = new()
            {
                Height = 150,
                ContentTitle = title,
                ContentMessage = message,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ButtonDefinitions = new[]
                    { new ButtonDefinition { Name = "OK", IsDefault = true } },
            };
            IMsBox<string> messageBoxStandardWindow = MessageBoxManager
               .GetMessageBoxCustom(parameters);

            messageBoxStandardWindow.ShowWindowDialogAsync(this);
        }

        private void ShowNoLicenseMessage(object sender, EventArgs args)
        {
            NoLicenseDialog dialog = new();
            dialog.ShowDialog<string>(this)
                  .ContinueWith(t =>
                   {
                       string license = t.Result;
                       if (!string.IsNullOrWhiteSpace(license))
                       {
                           string directory =
                               Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
                           string path = Path.Combine(directory, "dotnetbrowser.license");
                           File.WriteAllText(Path.GetFullPath(path),
                                             license);
                           Dispatcher.UIThread.InvokeAsync(Initialize);
                       }
                       else
                       {
                           Close();
                       }
                   }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        // ReSharper disable UnusedParameter.Local
        private void MainWindow_Opened(object sender, EventArgs e)
        {
            if (Design.IsDesignMode)
            {
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            Model.CreateEngine(this);
            Model.CreateTab();
        }
        // ReSharper restore UnusedParameter.Local
    }
}