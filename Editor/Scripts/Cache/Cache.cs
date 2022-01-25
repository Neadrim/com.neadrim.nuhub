using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Neadrim.NuHub
{
	[Serializable]
	internal class Cache : ISerializationCallbackReceiver, IDisposable
	{
		public struct RefreshProgress
		{
			public string Step;
			public float Progress;
			public int TotalReleaseCount;
			public int DownloadedReleaseCount;
			public int ParsedReleaseCount;
			public int CompletedReleaseCount;
			public int ErrorCount;
		}
		
		enum NoteProgress
		{
			Downloaded,
			Parsed,
			Error
		}

		public static Release ExecutingRelease;

		private const string CacheDir = "Library/" + nameof(Neadrim) + "/" + nameof(NuHub) + "/";
		private const string CacheFilePath = CacheDir + nameof(NuHub) + ".cache";
		private const int SimultaneousRequestCount = 10;

		private string GetCachePath(string fileName) => CacheDir + fileName;

		[SerializeField] private List<Release> _releases;
		[SerializeField] private long _serializedLastRefreshDate;

		private readonly Dictionary<int, List<Release>> _byMajor = new Dictionary<int, List<Release>>();
		private readonly Dictionary<ReleaseStream, List<Release>> _byStream = new Dictionary<ReleaseStream, List<Release>>();
		private RefreshProgress _currentProgress;
		private CancellationTokenSource _tokenSource;

		static Cache()
		{
			if (!Release.TryParse(Application.unityVersion, out ExecutingRelease))
				Debug.LogError($"Failed to identify current Unity version '{Application.unityVersion}'");
		}

		public static bool IsSupported(Version version) => version.Major >= 2017;
		
		public static Cache Empty() => new Cache();

		public static Cache Load()
		{
			if (!File.Exists(CacheFilePath))
				return new Cache();

			try
			{
				var content = File.ReadAllText(CacheFilePath);
				var cache = JsonUtility.FromJson<Cache>(content);
				cache.Initialize();
				return cache;
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				return new Cache();
			}
		}

		public static bool IsExecutingRelease(Release release) => ExecutingRelease.Equals(release);

		private Cache() => _releases = new List<Release>();

		public DateTime LastRefreshDate { get; private set; }

		public IReadOnlyList<Release> Releases => _releases;

		public async Task RefreshAsync(IProgress<RefreshProgress> progress = null)
		{
			_tokenSource = new CancellationTokenSource();
			var cancellationToken = _tokenSource.Token;

			_currentProgress = new RefreshProgress { Step = "Fetching versions info" };
			progress?.Report(_currentProgress);

			var ltsTask = UnityWeb.GetLtsReleaseAsync();
			var archiveTask = UnityWeb.GetArchiveAsync();
			var allTasks = Task.WhenAll(ltsTask, archiveTask);

			try
			{
				await allTasks;
			}
			catch (Exception exception)
			{
				_tokenSource.Dispose();
				_tokenSource = null;
				throw new Exception($"Failed to refresh Unity releases: \n{exception}");
			}

			cancellationToken.ThrowIfCancellationRequested();

			
			WriteCacheFile("lts.html", ltsTask.Result);
			WriteCacheFile("archive.html", archiveTask.Result);

			_currentProgress.Step = "Parsing release info";
			_currentProgress.Progress = 0.5f;
			progress?.Report(_currentProgress);

			LtsParser.Parse(ltsTask.Result, this);
			ArchiveParser.Parse(archiveTask.Result, this);

			Save();

			_currentProgress.Step = "Downloading release notes";
			_currentProgress.TotalReleaseCount = _releases.Count;
			_currentProgress.Progress = 0.1f;
			progress?.Report(_currentProgress);

			await GetMissingNotes(progress, 0.9f, _tokenSource.Token);

			cancellationToken.ThrowIfCancellationRequested();

			_currentProgress.Step = "Sorting releases";
			_currentProgress.Progress = 0.90f;
			progress?.Report(_currentProgress);

			LastRefreshDate = DateTime.Now;
			_releases.Sort((a, b) => b.Version.CompareTo(a.Version));

			_currentProgress.Step = "Saving";
			_currentProgress.Progress = 1f;
			progress?.Report(_currentProgress);

			Save();

			_tokenSource.Dispose();
			_tokenSource = null;
		}

		private async Task GetMissingNotes(IProgress<RefreshProgress> progress, float toProgress, CancellationToken cancellationToken)
		{
			var releasesToFetch = new List<Release>();
			foreach (var release in _releases)
			{
				if (release.Notes.Count == 0 && release.NotesUri != null)
					releasesToFetch.Add(release);
			}

			float fromProgress = _currentProgress.Progress;
			float progressRange = toProgress - fromProgress;
			_currentProgress.CompletedReleaseCount = _releases.Count - releasesToFetch.Count;

			void Report()
			{
				_currentProgress.Progress = fromProgress + (float)_currentProgress.CompletedReleaseCount / (float)_currentProgress.TotalReleaseCount * progressRange;
				progress?.Report(_currentProgress);
			}

			void OnNoteProgress(NoteProgress noteProgress)
			{
				switch (noteProgress)
				{
					case NoteProgress.Downloaded:
						++_currentProgress.DownloadedReleaseCount;
						break;
					case NoteProgress.Parsed:
						++_currentProgress.ParsedReleaseCount;
						++_currentProgress.CompletedReleaseCount;
						break;
					case NoteProgress.Error:
						++_currentProgress.ErrorCount;
						++_currentProgress.CompletedReleaseCount;
						break;
					default:
						break;
				}
				Report();
			}

			List<Task> tasks = new List<Task>(SimultaneousRequestCount);
			int currentIndex = 0;
			while (currentIndex < releasesToFetch.Count)
			{
				cancellationToken.ThrowIfCancellationRequested();

				for (int i = 0; i < SimultaneousRequestCount && currentIndex < releasesToFetch.Count; ++i)
				{
					var release = releasesToFetch[currentIndex++];
					if (release.Notes.Count == 0 && release.NotesUri != null)
						tasks.Add(GetNotesAsync(release, new Progress<NoteProgress>(OnNoteProgress), cancellationToken));
				}

				await Task.WhenAll(tasks);
				tasks.Clear();
				Save();
			}
		}

		private async Task GetNotesAsync(Release release, IProgress<NoteProgress> progress, CancellationToken cancellationToken)
		{
			try
			{
				var notes = await UnityWeb.GetAsync(release.NotesUri);
				cancellationToken.ThrowIfCancellationRequested();
				if (notes == null)
				{
					progress.Report(NoteProgress.Error);
					return;
				}

				progress.Report(NoteProgress.Downloaded);
				WriteCacheFile(release.Version + ".html", notes);
				
				ReleaseNoteParser.Parse(notes, release);
				cancellationToken.ThrowIfCancellationRequested();
				progress.Report(NoteProgress.Parsed);
			}
			catch (Exception e)
			{
				if (e is OperationCanceledException canceledException)
					throw canceledException;

				Debug.LogException(e);
				progress.Report(NoteProgress.Error);
			}
		}

		public Release[] ToArray(int major)
		{
			var filtered = new List<Release>();
			foreach (var release in _releases)
				if (release.Version.Major == major)
					filtered.Add(release);
			return filtered.ToArray();
		}

		public Release[] ToArray(ReleaseStream stream)
		{
			var filtered = new List<Release>();
			foreach (var release in _releases)
				if (release.Stream == stream)
					filtered.Add(release);
			return filtered.ToArray();
		}

		public bool HasMissingNotes()
		{
			foreach (var release in _releases)
				if (release.Notes.Count == 0/* && release.NotesUri != null*/)
					return true;
			return false;
		}

		public int MajorVersionCount => _byMajor.Count;

		public int[] GetMajorVersions()
		{
			var count = MajorVersionCount;
			var majors = new int[count];
			int i = 0;
			foreach (var major in _byMajor.Keys)
				majors[i++] = major;
			return majors;
		}

		public void GetMajorVersions(List<int> output)
		{
			output.Clear();
			foreach (var major in _byMajor.Keys)
				output.Add(major);
		}

		public bool TryGetLatest(ReleaseType type, out Release latestRelease)
		{
			latestRelease = null;
			foreach (var release in _releases)
				if (release.Type == type && (latestRelease == null || release.Version > latestRelease.Version))
					latestRelease = release;
			return latestRelease != null;
		}

		public void GetLatests(List<Release> releases)
		{
			releases.Clear();
			if (releases.Capacity < _byMajor.Count)
				releases.Capacity = _byMajor.Count;

			foreach (var major in _byMajor.Keys)
				releases.Add(GetLatest(major));
		}
		
		public Release GetLatest(Release release) => GetLatest(release.Version.Major);

		public Release GetLatest(int major)
		{
			Release latest = null;
			foreach (var release in _releases)
				if (release.Version.Major == major && (latest == null || release.Version > latest.Version))
					latest = release;
			return latest;
		}

		public bool TryGet(Release release, out Release result)
		{
			foreach (var r in _releases)
			{
				if (r.Equals(release))
				{
					result = r;
					return true;
				}
			}
			result = null;
			return false;
		}

		public bool TryGet(Version version, out Release release)
		{
			foreach (var r in _releases)
			{
				if (r.Version == version)
				{
					release = r;
					return true;
				}
			}
			release = null;
			return false;
		}

		public bool TryGet(string versionString, out Release release)
		{
			foreach (var r in _releases)
			{
				if (versionString.StartsWith(r.ToString(), StringComparison.OrdinalIgnoreCase))
				{
					release = r;
					return true;
				}
			}
			release = null;
			return false;
		}

		public IEnumerable<Release> Major(int major)
		{
			foreach (var release in _releases)
				if (release.Version.Major == major)
					yield return release;
		}

		public IEnumerable<Release> Stream(ReleaseStream stream)
		{
			foreach (var release in _releases)
				if (release.Stream == stream)
					yield return release;
		}

		internal void Save()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath));
				var json = JsonUtility.ToJson(this);
				File.WriteAllText(CacheFilePath, json);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		internal void Add(Release release)
		{
			if (!_releases.Contains(release))
			{
				_releases.Add(release);
				UpdateExecutingRelease(release);
				AddByMajor(release);
				AddByStream(release);
			}
		}

		internal void Clear()
		{
			_releases.Clear();
			_byMajor.Clear();
			_byStream.Clear();
		}

		private void Initialize()
		{
			_byMajor.Clear();
			_byStream.Clear();

			foreach (var release in _releases)
			{
				UpdateExecutingRelease(release);
				AddByMajor(release);
				AddByStream(release);
			}
		}

		private void UpdateExecutingRelease(Release release)
		{
			if (ExecutingRelease != null && !ReferenceEquals(release, ExecutingRelease) && release.Version.Equals(ExecutingRelease.Version))
				ExecutingRelease = release;
		}

		[System.Diagnostics.Conditional("NUHUB_DEBUG")]
		private void WriteCacheFile(string fileName, string content)
		{
			try
			{
				Directory.CreateDirectory(CacheDir);
				File.WriteAllText(GetCachePath(fileName), content);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void AddByStream(Release release)
		{
			if (_byStream.TryGetValue(release.Stream, out var releases))
				releases.Add(release);
			else
				_byStream.Add(release.Stream, new List<Release> { release });
		}

		private void AddByMajor(Release release)
		{
			if (_byMajor.TryGetValue(release.Version.Major, out var releases))
				releases.Add(release);
			else
				_byMajor.Add(release.Version.Major, new List<Release> { release });
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			_serializedLastRefreshDate = LastRefreshDate.ToBinary();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			LastRefreshDate = DateTime.FromBinary(_serializedLastRefreshDate);
		}

		public void Dispose()
		{
			if (_tokenSource != null)
			{
				_tokenSource.Cancel();
				_tokenSource = null;
			}
		}
	}
}
