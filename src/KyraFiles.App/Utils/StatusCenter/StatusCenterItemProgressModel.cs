// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KyraFiles.App.Data.Enums;
using KyraFiles.App.Helpers;

namespace KyraFiles.App.Utils.StatusCenter
{
	/// <summary>
	/// Represents a model for file system operation progress.
	/// </summary>
	/// <remarks>
	/// Every instance that have the same <see cref="IProgress{T}"/> instance will update the same progress.
	/// <br/>
	/// Therefore, the storage operation classes can portably instance this class and update progress from everywhere with the same <see cref="IProgress{T}"/> instance.
	/// </remarks>
	public sealed partial class StatusCenterItemProgressModel : INotifyPropertyChanged
	{
		private readonly IProgress<StatusCenterItemProgressModel>? _progress;

		private readonly ConcurrentDictionary<string, bool> _dirtyTracker;

		private readonly IntervalSampler _sampler, _sampler2;

		private bool _criticalReport;

		private long _previousProcessedSize;

		private long _previousProcessedItemsCount;

		private DateTimeOffset _previousReportTime;

		private FileSystemStatusCode? _Status;
		public FileSystemStatusCode? Status
		{
			get => _Status;
			set
			{
				if (_Status != value)
					_criticalReport = true;

				SetProperty(ref _Status, value);
			}
		}

		private bool _EnumerationCompleted;
		public bool EnumerationCompleted
		{
			get => _EnumerationCompleted;
			set
			{
				if (_EnumerationCompleted != value)
					_criticalReport = true;

				SetProperty(ref _EnumerationCompleted, value);
			}
		}

		private string? _FileName;
		public string? FileName
		{
			get => _FileName;
			set => SetProperty(ref _FileName, value);
		}

		private long _TotalSize;
		public long TotalSize
		{
			get => _TotalSize;
			set => SetProperty(ref _TotalSize, value);
		}

		private long _ProcessedSize;
		public long ProcessedSize
		{
			get => _ProcessedSize;
		}

		private long _ItemsCount;
		public long ItemsCount
		{
			get => _ItemsCount;
			set => SetProperty(ref _ItemsCount, value);
		}

		private long _ProcessedItemsCount;
		public long ProcessedItemsCount
		{
			get => _ProcessedItemsCount;
		}

		public double ProcessingSizeSpeed { get; private set; }

		public double ProcessingItemsCountSpeed { get; private set; }

		private double _smoothedSizeSpeed;
		private double _smoothedItemSpeed;

		public TimeSpan? EstimatedRemainingTime { get; private set; }

		public string? EstimatedRemainingTimeText { get; private set; }

		private DateTimeOffset _StartTime;
		public DateTimeOffset StartTime
		{
			get => _StartTime;
			set => SetProperty(ref _StartTime, value);
		}

		private DateTimeOffset _CompletedTime;

		public DateTimeOffset CompletedTime
		{
			get => _CompletedTime;
			set => SetProperty(ref _CompletedTime, value);
		}

		/// <summary>
		/// Only used when detailed count isn't available.
		/// You should NEVER set this property directly, instead, use <see cref="Report(double?)" /> to update the percentage.
		/// </summary>
		public double? Percentage { get; private set; }

		public event PropertyChangedEventHandler? PropertyChanged;

		public StatusCenterItemProgressModel(IProgress<StatusCenterItemProgressModel>? progress, bool enumerationCompleted = false, FileSystemStatusCode? status = null, long itemsCount = 0, int samplerInterval = 100)
		{
			// Initialize
			_progress = progress;
			_sampler = new(samplerInterval);
			_sampler2 = new(samplerInterval);
			_dirtyTracker = new();
			EnumerationCompleted = enumerationCompleted;
			Status = status;
			ItemsCount = itemsCount;
			StartTime = DateTimeOffset.Now;
			_previousReportTime = StartTime - TimeSpan.FromSeconds(1);
		}

		public void AddProcessedItemsCount(long value)
		{
			Interlocked.Add(ref _ProcessedItemsCount, value);
			_dirtyTracker[nameof(ProcessedItemsCount)] = true;
		}

		public void SetProcessedSize(long value)
		{
			Interlocked.Exchange(ref _ProcessedSize, value);
			_dirtyTracker[nameof(ProcessedSize)] = true;
		}

		private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			field = value;

			if (propertyName is not null)
			{
				_dirtyTracker[propertyName] = true;
			}
		}

		public void Report(double? percentage = null)
		{
			// Set the progress state as success
			if ((EnumerationCompleted &&
				ProcessedItemsCount == ItemsCount &&
				ProcessedSize == TotalSize &&
				TotalSize is not 0 ||
				(percentage is double p && Math.Abs(p - 100) <= double.Epsilon)) &&
				_Status is FileSystemStatusCode.InProgress or null)
			{
				_Status = FileSystemStatusCode.Success;
			}

			// Set time at completed when succeed
			if (_Status is FileSystemStatusCode.Success)
				CompletedTime = DateTimeOffset.Now;

			if (percentage is not null && Percentage != percentage)
			{
				SetProcessedSize((long)(TotalSize * percentage / 100));
				Percentage = percentage;
			}

			if (_sampler2.CheckNow())
			{
				var elapsedSeconds = (DateTimeOffset.Now - _previousReportTime).TotalSeconds;
				if (elapsedSeconds >= 0.1)
				{
					var deltaBytes = ProcessedSize - _previousProcessedSize;
					var deltaItems = ProcessedItemsCount - _previousProcessedItemsCount;

					if (deltaBytes >= 0)
					{
						var instantSizeSpeed = deltaBytes / elapsedSeconds;
						_smoothedSizeSpeed = _smoothedSizeSpeed <= 0 ? instantSizeSpeed : (0.3 * instantSizeSpeed) + (0.7 * _smoothedSizeSpeed);
						ProcessingSizeSpeed = _smoothedSizeSpeed;
						_dirtyTracker[nameof(ProcessingSizeSpeed)] = true;
					}

					if (deltaItems >= 0)
					{
						var instantItemSpeed = deltaItems / elapsedSeconds;
						_smoothedItemSpeed = _smoothedItemSpeed <= 0 ? instantItemSpeed : (0.3 * instantItemSpeed) + (0.7 * _smoothedItemSpeed);
						ProcessingItemsCountSpeed = _smoothedItemSpeed;
						_dirtyTracker[nameof(ProcessingItemsCountSpeed)] = true;
					}

					long remainingBytes = Math.Max(0, TotalSize - ProcessedSize);
					if (_smoothedSizeSpeed > 1024 && remainingBytes > 0)
					{
						var remainingSeconds = remainingBytes / _smoothedSizeSpeed;
						if (remainingSeconds < 86400)
						{
							EstimatedRemainingTime = TimeSpan.FromSeconds(remainingSeconds);
							EstimatedRemainingTimeText = FormatRemainingTime(EstimatedRemainingTime.Value);
							_dirtyTracker[nameof(EstimatedRemainingTime)] = true;
							_dirtyTracker[nameof(EstimatedRemainingTimeText)] = true;
						}
					}
					else if (remainingBytes == 0 && TotalSize > 0)
					{
						EstimatedRemainingTime = TimeSpan.Zero;
						EstimatedRemainingTimeText = null;
						_dirtyTracker[nameof(EstimatedRemainingTime)] = true;
						_dirtyTracker[nameof(EstimatedRemainingTimeText)] = true;
					}

					_previousReportTime = DateTimeOffset.Now;
					_previousProcessedSize = ProcessedSize;
					_previousProcessedItemsCount = ProcessedItemsCount;
				}
			}

			if (_criticalReport || _sampler.CheckNow())
			{
				_criticalReport = false;
				foreach (var propertyName in _dirtyTracker.Keys)
				{
					if (_dirtyTracker[propertyName])
					{
						_dirtyTracker[propertyName] = false;
						PropertyChanged?.Invoke(this, new(propertyName));
					}
				}

				_progress?.Report(this);
			}
		}

		public void ReportStatus(FileSystemStatusCode status, double? percentage = null)
		{
			Status = status;

			Report(percentage);
		}

		private static string FormatRemainingTime(TimeSpan timeSpan)
		{
			if (timeSpan.TotalHours >= 1)
			{
				return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m restantes";
			}
			if (timeSpan.TotalMinutes >= 1)
			{
				return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s restantes";
			}
			return $"{Math.Max(1, (int)timeSpan.TotalSeconds)}s restantes";
		}
	}
}
