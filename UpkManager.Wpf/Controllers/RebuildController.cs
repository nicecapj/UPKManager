﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;

using STR.Common.Extensions;
using STR.Common.Messages;

using STR.DialogView.Domain.Messages;

using STR.MvvmCommon;
using STR.MvvmCommon.Contracts;

using UpkManager.Domain.Contracts;
using UpkManager.Domain.Models;
using UpkManager.Domain.Models.UpkFile;
using UpkManager.Domain.Models.UpkFile.Tables;

using UpkManager.Wpf.Messages.Application;
using UpkManager.Wpf.Messages.FileListing;
using UpkManager.Wpf.Messages.Rebuild;
using UpkManager.Wpf.Messages.Settings;
using UpkManager.Wpf.Messages.Status;
using UpkManager.Wpf.ViewEntities;
using UpkManager.Wpf.ViewModels;


namespace UpkManager.Wpf.Controllers {

  [Export(typeof(IController))]
  public sealed class RebuildController : IController {

    #region Private Fields

    private bool isSelf;

    private FileSystemWatcher fileWatcher;

    private DomainSettings settings;

    private List<DomainUpkFile> allFiles;

    private readonly IMessenger messenger;

    private readonly IMapper mapper;

    private readonly IUpkFileRepository repository;

    private readonly  RebuildViewModel     viewModel;
    private readonly MainMenuViewModel menuViewModel;

    #endregion Private Fields

    #region Constructor

    [ImportingConstructor]
    public RebuildController(RebuildViewModel ViewModel, MainMenuViewModel MenuViewModel, IMessenger Messenger, IMapper Mapper, IUpkFileRepository Repository) {
          viewModel =     ViewModel;
      menuViewModel = MenuViewModel;

      messenger = Messenger;

      mapper = Mapper;

      repository = Repository;

      registerMessages();
      registerCommands();
    }

    #endregion Constructor

    #region Messages

    private void registerMessages() {
      messenger.Register<AppLoadedMessage>(this, onApplicationLoaded);

      messenger.Register<SettingsChangedMessage>(this, onSettingsChanged);

      messenger.Register<FileListingLoadedMessage>(this, onFileListingLoaded);
    }

    private void onApplicationLoaded(AppLoadedMessage message) {
      settings = message.Settings;

      loadExportFiles();

      setupWatchers();
    }

    private void onSettingsChanged(SettingsChangedMessage message) {
      settings = message.Settings;

      setupWatchers();
    }

    private void onFileListingLoaded(FileListingLoadedMessage message) {
      allFiles = message.Allfiles;
    }

    #endregion Messages

    #region Commands

    private void registerCommands() {
      menuViewModel.RebuildExported = new RelayCommandAsync(onRebuildExportedExecute, canRebuildExportedExecute);

      menuViewModel.DeleteExported = new RelayCommand(onDeleteExportedExecute, canDeleteExportedExecute);
    }

    #region RebuildExported Command

    private bool canRebuildExportedExecute() {
      return allFiles != null && allFiles.Any() && (viewModel.ExportsTree?.Traverse(e => e.IsChecked).Any() ?? false);
    }

    private async Task onRebuildExportedExecute() {
      await rebuildExports();
    }

    #endregion RebuildExported Command

    #region DeleteExported Command

    private bool canDeleteExportedExecute() {
      return allFiles != null && allFiles.Any() && (viewModel.ExportsTree?.Traverse(e => e.IsChecked).Any() ?? false);
    }

    private void onDeleteExportedExecute() {
      messenger.Send(new MessageBoxDialogMessage { Header = "Delete Exported Files", Message = "This will remove the files from disk.\n\nAre you sure?", Callback = onDeleteExportedResponse });
    }

    private void onDeleteExportedResponse(MessageBoxDialogMessage message) {
      if (message.IsCancel) return;

      fileWatcher.EnableRaisingEvents = false;

      List<ExportedObjectViewEntity> allExports = viewModel.ExportsTree?.Traverse(e => e.IsChecked && Path.HasExtension(e.Filename)).ToList();

      allExports?.ForEach(file => {
        if (File.Exists(file.Filename)) File.Delete(file.Filename);

        if (file.Parent != null) file.Parent.Children.Remove(file);
        else viewModel.ExportsTree.Remove(file);
      });

      Task.Delay(500).Wait();

      List<ExportedObjectViewEntity> allDirs = viewModel.ExportsTree?.Traverse(e => e.IsChecked && !Path.HasExtension(e.Filename)).ToList();

      allDirs?.ForEach(dir => {
        if (Directory.Exists(dir.Filename)) Directory.Delete(dir.Filename);

        if (dir.Parent != null) dir.Parent.Children.Remove(dir);
        else viewModel.ExportsTree.Remove(dir);
      });

      fileWatcher.EnableRaisingEvents = true;
    }

    #endregion DeleteExported Command

    #endregion Commands

    #region Private Methods

    private void setupWatchers() {
      if (String.IsNullOrEmpty(settings.ExportPath)) return;

      fileWatcher = new FileSystemWatcher {
        Path                  = settings.ExportPath,
        NotifyFilter          = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size,
        Filter                = String.Empty,
        IncludeSubdirectories = true
      };

      fileWatcher.Changed += onWatcherChanged;
      fileWatcher.Created += onWatcherChanged;
      fileWatcher.Deleted += onWatcherChanged;
      fileWatcher.Renamed += onWatcherRenamed;

      fileWatcher.EnableRaisingEvents = true;
    }

    private void onWatcherChanged(object sender, FileSystemEventArgs e) {
      loadExportFiles();
    }

    private void onWatcherRenamed(object sender, RenamedEventArgs e) {
      loadExportFiles();
    }

    private async void loadExportFiles() {
      if (String.IsNullOrEmpty(settings.ExportPath)) return;

      try {
        DomainExportedObject root = new DomainExportedObject();

        await repository.LoadDirectoryRecursive(root, settings.ExportPath);

        if (root.Children == null || !root.Children.Any()) return;

        viewModel.ExportsTree?.Traverse(e => true).ToList().ForEach(e => e.PropertyChanged -= onExportedObjectViewEntityChanged);

        IEnumerable<ExportedObjectViewEntity> temp = mapper.Map<IEnumerable<ExportedObjectViewEntity>>(root.Children);

        viewModel.ExportsTree = new ObservableCollection<ExportedObjectViewEntity>(temp);

        viewModel.ExportsTree.Traverse(e => true).ToList().ForEach(e => e.PropertyChanged += onExportedObjectViewEntityChanged);
      }
      catch(FileNotFoundException) { }
      catch(DirectoryNotFoundException) { }
      catch(Exception ex) {
        messenger.Send(new ApplicationErrorMessage { ErrorMessage = ex.Message, Exception = ex });
      }
    }

    private void onExportedObjectViewEntityChanged(object sender, PropertyChangedEventArgs args) {
      if (isSelf) return;

      ExportedObjectViewEntity entity = sender as ExportedObjectViewEntity;

      if (entity == null) return;

      switch(args.PropertyName) {
        case "IsChecked": {
          isSelf = true;

          checkAllChildren(entity);

          checkParents(entity.Parent);

          isSelf = false;

          break;
        }
        case "IsSelected": {
          if (entity.IsSelected && Path.HasExtension(entity.Filename)) messenger.Send(new ExportedObjectSelectedMessage { Filename = entity.Filename });

          break;
        }
        default: {
          break;
        }
      }
    }

    private static void checkAllChildren(ExportedObjectViewEntity parent) {
      parent.Children.ForEach(e => {
        e.IsChecked = parent.IsChecked;

        if (e.Children.Any()) checkAllChildren(e);
      });
    }

    private static void checkParents(ExportedObjectViewEntity parent) {
      while(true) {
        parent.IsChecked = parent.Children.All(e => e.IsChecked);

        if (parent.Parent != null) {
          parent = parent.Parent;

          continue;
        }

        break;
      }
    }

    private async Task rebuildExports() {
      Dictionary<ExportedObjectViewEntity, List<ExportedObjectViewEntity>> filesToMod = viewModel.ExportsTree?.Traverse(e => Path.HasExtension(e.Filename) && e.IsChecked)
                                                                                                              .GroupBy(e => e.Parent)
                                                                                                              .ToDictionary(g => g.Key, g => g.ToList());

      if (filesToMod == null || !filesToMod.Any()) return;

      LoadProgressMessage message = new LoadProgressMessage { Text = "Rebuilding...", Total = filesToMod.Count };

      foreach(KeyValuePair<ExportedObjectViewEntity, List<ExportedObjectViewEntity>> pair in filesToMod) {
        string gameFilename = $"{pair.Key.Filename.Replace(settings.ExportPath, null)}.upk";

        DomainUpkFile file = allFiles.SingleOrDefault(f => f.GameFilename.Equals(gameFilename));

        if (file == null) continue;

        DomainHeader header = await repository.LoadUpkFile(Path.Combine(settings.PathToGame, file.GameFilename));

        await header.ReadHeaderAsync(null);

        message.Current++;

        foreach(ExportedObjectViewEntity entity in pair.Value) {
          DomainExportTableEntry export = header.ExportTable.SingleOrDefault(ex => ex.NameTableIndex.Name.Equals(Path.GetFileNameWithoutExtension(entity.Filename), StringComparison.CurrentCultureIgnoreCase));

          if (export == null) continue;

          await export.ParseDomainObject(header, false, false);

          await export.DomainObject.SetObject(entity.Filename, header.NameTable);

          message.StatusText = entity.Filename;

          messenger.Send(message);
        }

        string directory = Path.Combine(settings.PathToGame, Path.GetDirectoryName(file.GameFilename), "mod");

        string filename = Path.Combine(directory, Path.GetFileName(file.GameFilename));

        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        await repository.SaveUpkFile(header, filename);

        DomainUpkFile upkFile = new DomainUpkFile { GameFilename = filename.Replace(settings.PathToGame, null), FileSize = new FileInfo(filename).Length };

        messenger.Send(new ModFileBuiltMessage { UpkFile = upkFile });
      }

      message.IsComplete = true;
      message.StatusText = null;

      messenger.Send(message);
    }

    #endregion Private Methods

  }

}
