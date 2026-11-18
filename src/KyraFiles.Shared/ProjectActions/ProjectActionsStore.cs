// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.ProjectActions
{
	public enum SelectionExecutionMode
	{
		AllTogether,
		OneByOne,
	}

	public sealed class CustomProjectCommand
	{
		public string Id { get; set; } = Guid.NewGuid().ToString("N");

		public string Name { get; set; } = string.Empty;

		public string Executable { get; set; } = string.Empty;

		public List<string> Arguments { get; set; } = [];

		public List<ProjectKind> ApplicableKinds { get; set; } = [];

		public SelectionExecutionMode SelectionMode { get; set; } = SelectionExecutionMode.AllTogether;

		public bool UsePowerShell { get; set; }
	}

	public sealed class ProjectActionsConfigModel
	{
		public int SchemaVersion { get; set; } = 1;

		public List<CustomProjectCommand> Commands { get; set; } = [];
	}

	[JsonSourceGenerationOptions(
		WriteIndented = true,
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
		UseStringEnumConverter = true)]
	[JsonSerializable(typeof(ProjectActionsConfigModel))]
	[JsonSerializable(typeof(CustomProjectCommand))]
	[JsonSerializable(typeof(List<CustomProjectCommand>))]
	[JsonSerializable(typeof(List<ProjectKind>))]
	[JsonSerializable(typeof(List<string>))]
	public sealed partial class ProjectActionsJsonContext : JsonSerializerContext
	{
	}

	public sealed class ProjectActionsStore
	{
		private readonly object _syncRoot = new();
		private readonly string? _filePath;
		private List<CustomProjectCommand> _commands = [];

		public ProjectActionsStore(string? filePath = null)
		{
			_filePath = filePath;
		}

		public string? LastError { get; private set; }

		public IReadOnlyList<CustomProjectCommand> GetAll()
		{
			lock (_syncRoot)
				return [.. _commands];
		}

		public IReadOnlyList<CustomProjectCommand> GetForKinds(IEnumerable<ProjectKind> kinds)
		{
			var set = kinds as HashSet<ProjectKind> ?? new HashSet<ProjectKind>(kinds);
			lock (_syncRoot)
			{
				return _commands
					.Where(c => c.ApplicableKinds.Count == 0 || c.ApplicableKinds.Any(set.Contains))
					.ToList();
			}
		}

		public (bool Ok, string? Error) Load()
		{
			if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
			{
				LastError = null;
				return (true, null);
			}

			return ImportFromFile(_filePath, saveAfter: false);
		}

		public (bool Ok, string? Error) ImportFromJson(string json, bool saveAfter = true)
		{
			try
			{
				var model = JsonSerializer.Deserialize(json, ProjectActionsJsonContext.Default.ProjectActionsConfigModel);
				if (model is null)
				{
					LastError = "JSON de acoes de projeto vazio ou invalido.";
					return (false, LastError);
				}

				var validated = new List<CustomProjectCommand>();
				foreach (var cmd in model.Commands ?? [])
				{
					if (cmd is null || string.IsNullOrWhiteSpace(cmd.Name) || string.IsNullOrWhiteSpace(cmd.Executable))
					{
						LastError = "Cada comando precisa de 'name' e 'executable' preenchidos.";
						return (false, LastError);
					}

					if (string.IsNullOrWhiteSpace(cmd.Id))
						cmd.Id = Guid.NewGuid().ToString("N");

					cmd.Arguments ??= [];
					cmd.ApplicableKinds ??= [];
					validated.Add(cmd);
				}

				lock (_syncRoot)
					_commands = validated;

				LastError = null;
				if (saveAfter)
					Save();

				return (true, null);
			}
			catch (Exception ex)
			{
				LastError = $"JSON invalido: {ex.Message}";
				return (false, LastError);
			}
		}

		public (bool Ok, string? Error) ImportFromFile(string path, bool saveAfter = true)
		{
			try
			{
				var json = File.ReadAllText(path);
				return ImportFromJson(json, saveAfter);
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
				return (false, LastError);
			}
		}

		public string ExportToJson()
		{
			ProjectActionsConfigModel model;
			lock (_syncRoot)
			{
				model = new ProjectActionsConfigModel
				{
					SchemaVersion = 1,
					Commands = [.. _commands],
				};
			}

			return JsonSerializer.Serialize(model, ProjectActionsJsonContext.Default.ProjectActionsConfigModel);
		}

		public void Upsert(CustomProjectCommand command)
		{
			ArgumentNullException.ThrowIfNull(command);
			if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Executable))
				throw new ArgumentException("Nome e executavel sao obrigatorios.");

			if (string.IsNullOrWhiteSpace(command.Id))
				command.Id = Guid.NewGuid().ToString("N");

			lock (_syncRoot)
			{
				var idx = _commands.FindIndex(c => string.Equals(c.Id, command.Id, StringComparison.Ordinal));
				if (idx >= 0)
					_commands[idx] = command;
				else
					_commands.Add(command);
			}

			Save();
		}

		public bool Remove(string id)
		{
			bool removed;
			lock (_syncRoot)
				removed = _commands.RemoveAll(c => string.Equals(c.Id, id, StringComparison.Ordinal)) > 0;

			if (removed)
				Save();

			return removed;
		}

		public void Save()
		{
			if (string.IsNullOrWhiteSpace(_filePath))
				return;

			var dir = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);

			var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
			File.WriteAllText(tempPath, ExportToJson());
			File.Move(tempPath, _filePath, overwrite: true);
		}
	}
}
