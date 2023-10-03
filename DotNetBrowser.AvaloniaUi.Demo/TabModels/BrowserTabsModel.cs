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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using DotNetBrowser.AvaloniaUi.Dialogs;
using DotNetBrowser.Engine;
using DotNetBrowser.Handlers;
using DotNetBrowser.Logging;
using DotNetBrowser.Permissions.Handlers;

namespace DotNetBrowser.AvaloniaUi.Demo.TabModels
{
    public class BrowserTabsModel
    {
        private const string DefaultUrl =
            "https://teamdev.com/dotnetbrowser/blog/chrome-extensions-in-dotnetbrowser/";

        private IEngine engine;
        private RenderingMode renderingMode;

        public ObservableCollection<BrowserTabModel> Tabs { get; } = new();

        public event EventHandler AllTabsClosed;
        public event EventHandler<BrowserTabModel> TabCreated;
        public event EventHandler<MessageEventArgs> EngineCrashed;
        public event EventHandler<MessageEventArgs> EngineInitFailed;
        public event EventHandler NoLicenseFound;

        public void CreateEngine(Visual parent)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            renderingMode = RenderingMode.HardwareAccelerated;
            ProprietaryFeatures proprietaryFeatures = ProprietaryFeatures.None;
            if (arguments.FirstOrDefault(arg => arg.ToLower().Contains("lightweight"))
                != null)
            {
                renderingMode = RenderingMode.OffScreen;
            }

            if (arguments.FirstOrDefault(arg => arg.ToLower().Contains("enable-file-log"))
                != null)
            {
                LoggerProvider.Instance.Level = SourceLevels.Verbose;
                LoggerProvider.Instance.FileLoggingEnabled = true;
                string logFile = $"DotNetBrowser-AvaloniaUi-{Guid.NewGuid()}.log";
                LoggerProvider.Instance.OutputFile = Path.GetFullPath(logFile);
            }

            if (arguments.FirstOrDefault(arg => arg.ToLower().Contains("proprietary"))
                != null)
            {
                proprietaryFeatures = ProprietaryFeatures.Aac
                                      | ProprietaryFeatures.H264
                                      | ProprietaryFeatures.Widevine;
            }

            try
            {
                engine = EngineFactory.Create(new EngineOptions.Builder
                {
                    RenderingMode = renderingMode,
                    ProprietaryFeatures = proprietaryFeatures,
                    ChromiumSwitches = { "--force-renderer-accessibility" }
                }.Build());

                engine.Profiles.Default.Network.AuthenticateHandler =
                    new DefaultAuthenticationHandler(parent);
                engine.Profiles.Default.Permissions.RequestPermissionHandler =
                    new Handler<RequestPermissionParameters,
                        RequestPermissionResponse>(_ => RequestPermissionResponse
                                                      .Grant());
                engine.Disposed += (_, args) =>
                {
                    if (args.ExitCode != 0)
                    {
                        string message =
                            $"The Chromium engine exit code was {args.ExitCode:x8}";
                        Trace.WriteLine(message);
                        EngineCrashed?.Invoke(this,
                                              new MessageEventArgs(message,
                                               "DotNetBrowser Warning"));
                    }
                };
            }
            catch (NoLicenseException)
            {
                NoLicenseFound?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                EngineInitFailed?.Invoke(this,
                                         new MessageEventArgs(e.Message,
                                                              "DotNetBrowser Initialization Error"));
            }
        }

        public void CreateTab(string url = DefaultUrl)
        {
            if (engine == null)
            {
                return;
            }
            BrowserTabModel browserTabModel = new("New Tab", url, CloseTab)
            {
                Browser = engine?.CreateBrowser(),
                RenderingMode = renderingMode
            };
            Tabs.Insert(Tabs.Count, browserTabModel);
            TabCreated?.Invoke(this, browserTabModel);
        }

        public void DisposeEngine()
        {
            engine?.Dispose();
        }

        public void OnNewTab()
        {
            CreateTab();
        }

        private void CloseTab(BrowserTabModel browserTabModel)
        {
            Tabs.Remove(browserTabModel);
            if (Tabs.Count == 0)
            {
                AllTabsClosed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}