﻿using System;
using System.Windows;
using Autofac;
using GalaSoft.MvvmLight.Messaging;
using log4net;
using Loci.Interfaces;
using Loci.Models;
using Loci.Utilities;
using Loci.ViewModels;
using Loci.Views;

namespace Loci
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : IDisposable
    {
        public static IContainer Container { get; set; }

        private static readonly ILog Logger =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Handles the OnStartup event of the App control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="StartupEventArgs"/> instance containing the event data.</param>
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();

            var builder = new ContainerBuilder();
            builder.Register(m => Messenger.Default).As<IMessenger>();
            builder.RegisterType<Configuration>().SingleInstance();
            builder.RegisterType<ConfigurationLoader>().As<IConfigurationLoader>();
            builder.RegisterType<ExportViewModel>();
            builder.RegisterType<ExportWindow>();
            builder.RegisterType<ImportViewModel>();
            builder.RegisterType<ImportWindow>();
            builder.RegisterType<MainViewModel>();
            builder.RegisterType<MainWindow>();
            builder.RegisterType<SettingsViewModel>();
            builder.RegisterType<SettingsWindow>();
            builder.RegisterType<NewProjectViewModel>();
            builder.RegisterType<NewProjectWindow>();
            Container = builder.Build();
            Container.Resolve<MainWindow>().Show();            
        }

        public void Dispose()
        {
        }
    }
}