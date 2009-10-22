﻿// 
// Copyright (c) 2004-2009 Jaroslaw Kowalski <jaak@jkowalski.net>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

#if SILVERLIGHT

using System;
using System.Windows;
using System.Windows.Browser;
using Microsoft.Silverlight.Testing;
using Microsoft.Silverlight.Testing.Harness;
using Microsoft.Silverlight.Testing.UnitTesting.Harness;

namespace NLog.UnitTests
{
    public partial class App : Application
    {
        private VisualStudioLogProvider vsProvider = new VisualStudioLogProvider();

        public App()
        {
            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            this.UnhandledException += this.Application_UnhandledException;

            //InitializeComponent();
        }

        class MyLogProvider : LogProvider
        {
            public override void Process(LogMessage logMessage)
            {
                if (logMessage.HasDecorator(UnitTestLogDecorator.ScenarioResult))
                {
                    var result = (ScenarioResult)logMessage[UnitTestLogDecorator.ScenarioResult];

                    HtmlPage.Window.Invoke("scenarioResult",
                                           result.Started.Ticks,
                                           result.Finished.Ticks,
                                           (result.TestClass != null) ? result.TestClass.Type.FullName : null,
                                           (result.TestMethod != null) ? result.TestMethod.Name : null,
                                           result.Result.ToString(),
                                           (result.Exception != null) ? result.Exception.ToString() : null);
                }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            UnitTestSettings settings = UnitTestSystem.CreateDefaultSettings();
            settings.LogProviders.Add(new MyLogProvider());
            vsProvider.TestRunId = Guid.NewGuid().ToString();
            settings.LogProviders.Add(vsProvider);
            settings.TestHarness.TestHarnessCompleted += new EventHandler<TestHarnessCompletedEventArgs>(TestHarness_TestHarnessCompleted);
            this.RootVisual = UnitTestSystem.CreateTestPage(settings);
        }

        class MyHarness : UnitTestHarness
        {
            public string TrxContent { get; private set; }

            public override void WriteLogFile(string logName, string fileContent)
            {
                this.TrxContent = fileContent;
            }
        }

        void TestHarness_TestHarnessCompleted(object sender, TestHarnessCompletedEventArgs e)
        {
            var harness = new MyHarness();

            vsProvider.WriteLogFile(harness);
            HtmlPage.Window.Invoke("testCompleted", harness.TrxContent);
        }

        private void Application_Exit(object sender, EventArgs e)
        {
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }
        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
            }
            catch (Exception)
            {
            }
        }
    }
}

#endif