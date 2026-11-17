// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

// System
global using global::System;
global using global::System.Collections;
global using global::System.Collections.Generic;
global using global::System.Collections.ObjectModel;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;
global using global::System.ComponentModel;
global using global::System.Diagnostics;
global using global::System.Text.Json;
global using global::System.Text.Json.Serialization;
global using SystemIO = global::System.IO;

// CommunityToolkit.Mvvm
global using global::CommunityToolkit.Mvvm.ComponentModel;
global using global::CommunityToolkit.Mvvm.DependencyInjection;
global using global::CommunityToolkit.Mvvm.Input;
global using global::CommunityToolkit.Mvvm.Messaging;

// KyraFiles.App
global using global::KyraFiles.App.Helpers;
global using global::KyraFiles.App.Extensions;
global using global::KyraFiles.App.Utils;
global using global::KyraFiles.App.Utils.Cloud;
global using global::KyraFiles.App.Utils.FileTags;
global using global::KyraFiles.App.Utils.Git;
global using global::KyraFiles.App.Utils.Library;
global using global::KyraFiles.App.Utils.Serialization;
global using global::KyraFiles.App.Utils.Shell;
global using global::KyraFiles.App.Utils.StatusCenter;
global using global::KyraFiles.App.Utils.Storage;
global using global::KyraFiles.App.Utils.Taskbar;
global using global::KyraFiles.App.Data.Behaviors;
global using global::KyraFiles.App.Data.Commands;
global using global::KyraFiles.App.Data.Contexts;
global using global::KyraFiles.App.Data.Contracts;
global using global::KyraFiles.App.Data.EventArguments;
global using global::KyraFiles.App.Data.Factories;
global using global::KyraFiles.App.Data.Items;
global using global::KyraFiles.App.Data.Models;
global using global::KyraFiles.App.Data.Parameters;
global using global::KyraFiles.App.Data.TemplateSelectors;
global using global::KyraFiles.App.Services;
global using global::KyraFiles.App.UserControls;
global using global::KyraFiles.App.UserControls.TabBar;
global using global::KyraFiles.App.UserControls.Widgets;
global using global::KyraFiles.App.ViewModels;
global using global::KyraFiles.App.ViewModels.UserControls;
global using global::KyraFiles.App.ViewModels.UserControls.Widgets;
global using global::KyraFiles.App.Views;
global using global::KyraFiles.App.Views.Layouts;
global using global::KyraFiles.App.Views.Shells;
global using global::KyraFiles.App.Data.Enums;
global using global::KyraFiles.App.Data.Messages;
global using global::KyraFiles.App.Services.DateTimeFormatter;
global using global::KyraFiles.App.Services.PreviewPopupProviders;
global using global::KyraFiles.App.Services.Settings;
global using global::KyraFiles.App.ViewModels.Dialogs;
global using global::KyraFiles.App.ViewModels.Dialogs.AddItemDialog;
global using global::KyraFiles.App.ViewModels.Dialogs.FileSystemDialog;
global using global::KyraFiles.App.Utils.CommandLine;

// KyraFiles.Core.Storage

global using global::KyraFiles.Core.Storage;
global using global::KyraFiles.Core.Storage.Enums;
global using global::KyraFiles.Core.Storage.EventArguments;
global using global::KyraFiles.Core.Storage.Extensions;
global using global::OwlCore.Storage;

// KyraFiles.App.Storage

global using global::KyraFiles.App.Storage;
global using global::KyraFiles.App.Storage.Storables;
global using global::KyraFiles.App.Storage.Watchers;

// KyraFiles.Shared
global using global::KyraFiles.Shared;
global using global::KyraFiles.Shared.Attributes;
global using global::KyraFiles.Shared.Extensions;
