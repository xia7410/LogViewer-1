﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace LogViewer.ViewModels
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Catel;
    using Catel.Configuration;
    using Catel.MVVM;
    using Orc.Squirrel;
    using Orc.WorkspaceManagement;
    using Services;
    using Settings = LogViewer.Settings;

    public class SettingsViewModel : ViewModelBase
    {
        #region Fields
        private readonly IConfigurationService _configurationService;
        private readonly ILogTableConfigurationService _logTableConfigurationService;
        private readonly IManageUserDataService _manageUserDataService;
        private readonly IUpdateService _updateService;
        private readonly IWorkspaceManager _workspaceManager;
        #endregion

        #region Constructors
        public SettingsViewModel(IConfigurationService configurationService, IWorkspaceManager workspaceManager, IManageUserDataService manageUserDataService, IUpdateService updateService,
            ILogTableConfigurationService logTableConfigurationService)
        {
            Argument.IsNotNull(() => configurationService);
            Argument.IsNotNull(() => workspaceManager);
            Argument.IsNotNull(() => manageUserDataService);
            Argument.IsNotNull(() => updateService);
            Argument.IsNotNull(() => logTableConfigurationService);

            _configurationService = configurationService;
            _workspaceManager = workspaceManager;
            _manageUserDataService = manageUserDataService;
            _updateService = updateService;
            _logTableConfigurationService = logTableConfigurationService;

            OpenApplicationDataDirectory = new Command(OnOpenApplicationDataDirectoryExecute);
            BackupUserData = new TaskCommand(OnBackupUserDataExecuteAsync);
            ResetFilters = new TaskCommand(OnResetFiltersExecuteAsync);
            ResetWorkspaces = new TaskCommand(OnResetWorkspacesExecuteAsync);

            Title = "Settings";
        }
        #endregion

        #region Properties
        public bool IsUpdateSystemAvailable { get; private set; }
        public bool CheckForUpdates { get; set; }
        public List<UpdateChannel> AvailableUpdateChannels { get; private set; }
        public UpdateChannel UpdateChannel { get; set; }
        public bool EnableAnalytics { get; set; }
        public bool EnableTooltips { get; set; }
        public bool IsTimestampVisible { get; set; }
        #endregion

        #region Methods
        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var workspace = _workspaceManager.Workspace;
            EnableTooltips = workspace.GetWorkspaceValue(Settings.Workspace.General.EnableTooltips, Settings.Workspace.General.EnableTooltipsDefaultValue);

            EnableAnalytics = _configurationService.GetRoamingValue<bool>(Settings.Application.General.EnableAnalytics);

            IsUpdateSystemAvailable = _updateService.IsUpdateSystemAvailable;
            CheckForUpdates = _updateService.CheckForUpdates;
            AvailableUpdateChannels = new List<UpdateChannel>(_updateService.AvailableChannels);
            UpdateChannel = _updateService.CurrentChannel;

            IsTimestampVisible = _logTableConfigurationService.GetIsTimestampVisibile();
        }

        protected override async Task<bool> SaveAsync()
        {
            var workspace = _workspaceManager.Workspace;
            workspace.SetWorkspaceValue(Settings.Workspace.General.EnableTooltips, EnableTooltips);

            await _workspaceManager.StoreAndSaveAsync();

            _configurationService.SetRoamingValue(Settings.Application.General.EnableAnalytics, EnableAnalytics);

            _updateService.CheckForUpdates = CheckForUpdates;
            _updateService.CurrentChannel = UpdateChannel;

            _logTableConfigurationService.SetIsTimestampVisibile(IsTimestampVisible);

            return await base.SaveAsync();
        }
        #endregion

        #region Commands
        public Command OpenApplicationDataDirectory { get; private set; }

        private void OnOpenApplicationDataDirectoryExecute()
        {
            _manageUserDataService.OpenApplicationDataDirectory();
        }

        public TaskCommand BackupUserData { get; private set; }

        private async Task OnBackupUserDataExecuteAsync()
        {
            await _manageUserDataService.BackupUserDataAsync();
        }

        public TaskCommand ResetFilters { get; private set; }

        private async Task OnResetFiltersExecuteAsync()
        {
            await _manageUserDataService.ResetFiltersAsync();
        }

        public TaskCommand ResetWorkspaces { get; private set; }

        private async Task OnResetWorkspacesExecuteAsync()
        {
            await _manageUserDataService.ResetWorkspacesAsync();
        }
        #endregion
    }
}