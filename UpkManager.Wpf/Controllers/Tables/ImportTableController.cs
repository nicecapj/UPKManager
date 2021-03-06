﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using AutoMapper;

using STR.MvvmCommon.Contracts;

using UpkManager.Wpf.Messages.FileListing;
using UpkManager.Wpf.ViewEntities.Tables;
using UpkManager.Wpf.ViewModels.Tables;


namespace UpkManager.Wpf.Controllers.Tables {

  [Export(typeof(IController))]
  public class ImportTableController : IController {

    #region Private Fields

    private readonly ImportTableViewModel viewModel;

    private readonly IMessenger messenger;
    private readonly IMapper       mapper;

    #endregion Private Fields

    #region Constructor

    [ImportingConstructor]
    public ImportTableController(ImportTableViewModel ViewModel, IMessenger Messenger, IMapper Mapper) {
      viewModel = ViewModel;

      viewModel.ImportTableEntries = new ObservableCollection<ImportTableEntryViewEntity>();

      messenger = Messenger;
         mapper = Mapper;

      registerMessages();
    }

    #endregion Constructor

    #region Messages

    private void registerMessages() {
      messenger.Register<FileLoadingMessage>(this, onFileLoading);
      messenger.Register<FileLoadedMessage>(this, onFileLoaded);
    }

    private void onFileLoading(FileLoadingMessage message) {
      viewModel.ImportTableEntries.Clear();
    }

    private void onFileLoaded(FileLoadedMessage message) {
      viewModel.ImportTableEntries = new ObservableCollection<ImportTableEntryViewEntity>(mapper.Map<IEnumerable<ImportTableEntryViewEntity>>(message.File.Header.ImportTable));
    }

    #endregion Messages

  }

}
