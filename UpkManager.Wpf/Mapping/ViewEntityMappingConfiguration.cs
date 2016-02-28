﻿using System;
using System.ComponentModel.Composition;
using System.Windows;

using AutoMapper;

using UpkManager.Domain.Contracts;
using UpkManager.Domain.Models;
using UpkManager.Domain.Models.Tables;

using UpkManager.Wpf.Messages.Status;
using UpkManager.Wpf.ViewEntities;
using UpkManager.Wpf.ViewEntities.Tables;


namespace UpkManager.Wpf.Mapping {

  [Export(typeof(IAutoMapperConfiguration))]
  public class ViewEntityMappingConfiguration : IAutoMapperConfiguration {

    #region IAutoMapperConfiguration Implementation

    public void RegisterMappings(IMapperConfiguration config) {

      #region Settings

      config.CreateMap<DomainSettings, SettingsDialogViewEntity>().ReverseMap();

      config.CreateMap<DomainSettings, SettingsWindowViewEntity>().ForMember(dest => dest.AreSettingsChanged, opt => opt.Ignore())
                                                                  .ForMember(dest => dest.MainWindowState,    opt => opt.ResolveUsing(src => src.Maximized ? WindowState.Maximized : WindowState.Normal));

      config.CreateMap<SettingsWindowViewEntity, DomainSettings>().ForMember(dest => dest.Maximized,  opt => opt.ResolveUsing(src => src.MainWindowState == WindowState.Maximized))
                                                                  .ForMember(dest => dest.PathToGame, opt => opt.Ignore())
                                                                  .ForMember(dest => dest.ExportPath, opt => opt.Ignore());

      #endregion Settings

      #region Messages

      config.CreateMap<DomainLoadProgress, LoadProgressMessage>().ForMember(dest => dest.CanAsync, opt => opt.Ignore());

      #endregion Messages

      #region Header

      config.CreateMap<DomainHeader, HeaderViewEntity>().ForMember(dest => dest.Group, opt => opt.MapFrom(src => src.Group.String))
                                                        .ForMember(dest => dest.Guid,  opt => opt.ResolveUsing(src => new Guid(src.Guid)));

      #endregion Header

      #region Tables

      config.CreateMap<DomainExportTableEntry, ExportTableEntryViewEntity>().ForMember(dest => dest.Guid, opt => opt.ResolveUsing(src => new Guid(src.Guid)));

      config.CreateMap<DomainImportTableEntry, ImportTableEntryViewEntity>();

      config.CreateMap<DomainNameTableEntry, NameTableEntryViewEntity>().ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.String));

      #endregion Tables

    }

    #endregion IAutoMapperConfiguration Implementation

  }

}