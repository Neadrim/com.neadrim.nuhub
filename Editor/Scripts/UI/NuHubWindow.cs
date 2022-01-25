using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Neadrim.NuHub
{
	public class NuHubWindow : EditorWindow
	{
		private enum ReleaseFilter
		{
			All,
			Latest,
			Update
		}

		private struct ReleaseNoteItem
		{
			public ReleaseNoteController.Style Style;
			public ReleaseNote Note;
			public Version Version;
		}

		private const int AutomaticRefreshIntervalDays = 1;
		private ListView _releaseListView;
		private ListView _releaseNotesListView;
		private Cache _cache;
		private readonly List<Release> _releases = new List<Release>();
		private readonly List<Release> _filteredReleases = new List<Release>();
		private readonly List<ReleaseNoteItem> _releaseNotes = new List<ReleaseNoteItem>();
		private readonly List<ReleaseNoteItem> _filteredReleaseNotes = new List<ReleaseNoteItem>();
		private Label _refreshDate;
		private Button _refreshButton;
		private ProgressBar _refreshProgressBar;
		private VisualElement _currentReleaseRoot;
		private VisualElement _errorMessageRoot;
		private readonly ToolbarToggle[] _releaseFilterToggles = new ToolbarToggle[3];
		private ReleaseFilter _releaseFilter;
		private string _releaseSearchText = "";
		private string _releaseNotesSearchText = "";
		private bool _areMultipleReleaseSelected = false;

		[MenuItem("Window/NuHub")]
		public static void Open()
		{
			var window = GetWindow<NuHubWindow>("NuHub");
			window.minSize = new UnityEngine.Vector2(400, 200);
		}

		private void OnEnable()
		{
			_cache = Cache.Load();
		}

		private void OnDisable()
		{
			_cache?.Dispose();
		}

		public void CreateGUI()
		{
			var root = rootVisualElement;
			var uxmlData = UIData.Instance;
			uxmlData.NuHub.CloneTree(root);

			CreateReleaseListView(root, uxmlData.ReleaseInfo);
			CreateReleaseNotesListView(root, uxmlData.ReleaseNote);

			_refreshDate = root.Q<Label>("RefreshDate");
			_refreshButton = root.Q<Button>("Refresh");
			_refreshButton.clickable.clicked += OnRefresh;

			_currentReleaseRoot = root.Q<VisualElement>("CurrentRelease");
			UpdateCurrentRelease();

			CreateFilters(root.Q<VisualElement>("ReleaseFiltersPanel"));

			_refreshProgressBar = root.Q<ProgressBar>("RefreshProgress");
			_refreshProgressBar.style.visibility = Visibility.Hidden;
			_refreshDate.text = _cache.LastRefreshDate.ToString("d MMM, yyyy");
			_errorMessageRoot = root.Q<VisualElement>("ErrorMessageRoot");
			UpdateLastRefreshDate();

			if (_cache.Releases.Count == 0 || _cache.HasMissingNotes() || (_cache.LastRefreshDate - DateTime.Now).Days >= AutomaticRefreshIntervalDays)
				OnRefresh();
			else
				RefreshReleases();
		}

		private void CreateReleaseListView(VisualElement root, VisualTreeAsset releaseInfoTree)
		{
			_releaseListView = root.Q<ListView>("Releases");
			_releaseListView.makeItem = () =>
			{
				var element = releaseInfoTree.CloneTree();
				element.userData = new ReleaseInfoController();
				return element;
			};
			_releaseListView.bindItem = (element, i) =>
			{
				var release = _filteredReleases[i];
				var controller = (ReleaseInfoController)element.userData;
				var style = release.Equals(Cache.ExecutingRelease) ? ReleaseInfoController.InstallButtonStyle.Installed :
					release.Version > Cache.ExecutingRelease.Version ? ReleaseInfoController.InstallButtonStyle.UpdateAvailable :
					ReleaseInfoController.InstallButtonStyle.DownloadAvailable;
				controller.Bind(element, _filteredReleases[i], style);
			};
			_releaseListView.unbindItem = (element, i) => ((ReleaseInfoController)element.userData).Unbind();
			_releaseListView.itemsSource = _filteredReleases;
			_releaseListView.onSelectionChange += objects => RefreshNotes();
		}

		private void CreateReleaseNotesListView(VisualElement root, VisualTreeAsset releaseNoteTree)
		{
			_releaseNotesListView = root.Q<ListView>("ReleaseNotes");
			_releaseNotesListView.makeItem = () =>
			{
				var element = releaseNoteTree.CloneTree();
				element.userData = new ReleaseNoteController();
				return element;
			};
			_releaseNotesListView.bindItem = (element, i) =>
			{
				var item = _filteredReleaseNotes[i];
				var controller = (ReleaseNoteController)element.userData;
				controller.Bind(element, item.Note, item.Style, _areMultipleReleaseSelected ? item.Version : null);
			};
			_releaseNotesListView.unbindItem = (element, i) => ((ReleaseNoteController)element.userData).Unbind();
			_releaseNotesListView.itemsSource = _filteredReleaseNotes;

			var searchField = root.Q<ToolbarSearchField>("ReleaseNotesSearch");
			searchField.RegisterValueChangedCallback(@event =>
			{
				_releaseNotesSearchText = @event.newValue;
				FilterReleaseNotes();
			});
		}

		private void CreateFilters(VisualElement root)
		{
			void RegisterFilterToggle(ReleaseFilter filter)
			{
				var toggle = root.Q<ToolbarToggle>("ReleaseFilter" + filter);
				toggle.RegisterValueChangedCallback(@event =>
				{
					if (!@event.newValue)
						return;

					SetActiveReleaseFilter(filter);
				});
				_releaseFilterToggles[(int)filter] = toggle;
			}

			RegisterFilterToggle(ReleaseFilter.All);
			RegisterFilterToggle(ReleaseFilter.Latest);
			RegisterFilterToggle(ReleaseFilter.Update);

			SetActiveReleaseFilter(ReleaseFilter.Latest);

			var searchField = root.Q<ToolbarSearchField>("ReleaseFilterSearch");
			searchField.RegisterValueChangedCallback(@event =>
			{
				_releaseSearchText = @event.newValue;
				FilterReleases();
			});
		}

		private async void OnRefresh()
		{
			_refreshProgressBar.value = 0f;
			_refreshProgressBar.title = "Refreshing";
			_refreshProgressBar.style.visibility = Visibility.Visible;
			_refreshButton.SetEnabled(false);
			_errorMessageRoot.style.display = DisplayStyle.None;

			try
			{
				await _cache.RefreshAsync(new Progress<Cache.RefreshProgress>(progress =>
				{
					_refreshProgressBar.title = progress.Step;
					_refreshProgressBar.value = progress.Progress * 100f;
				}));
			}
			finally
			{
				_refreshProgressBar.style.visibility = Visibility.Hidden;
				_refreshButton.SetEnabled(true);
				UpdateLastRefreshDate();
				UpdateCurrentRelease();
				RefreshReleases();
				RefreshNotes();
			}
		}

		private void UpdateCurrentRelease()
		{
			if (_currentReleaseRoot.userData is ReleaseInfoController controller)
				controller.Unbind();
			else
			{
				controller = new ReleaseInfoController();
				_currentReleaseRoot.userData = controller;
			}
			controller.Bind(_currentReleaseRoot, Cache.ExecutingRelease, ReleaseInfoController.InstallButtonStyle.Hidden);
		}

		private void UpdateLastRefreshDate() => _refreshDate.text = _cache.LastRefreshDate == default ? "Never" : _cache.LastRefreshDate.ToString("d MMM, yyyy");

		private void SetActiveReleaseFilter(ReleaseFilter filter)
		{
			if (_releaseFilter != filter)
				_releaseListView.ClearSelection();
			_releaseFilter = filter;
			RefreshReleases();
		}

		private void RefreshReleases()
		{
			if (_cache == null)
			{
				_errorMessageRoot.style.display = DisplayStyle.None;
				return;
			}

			for (int i = 0; i < _releaseFilterToggles.Length; ++i)
				_releaseFilterToggles[i].value = i == (int)_releaseFilter;

			_releases.Clear();
			switch (_releaseFilter)
			{
				case ReleaseFilter.All:
					for (var i = 0; i < _cache.Releases.Count; ++i)
						_releases.Add(_cache.Releases[i]);
					break;

				case ReleaseFilter.Latest:
					_cache.GetLatests(_releases);
					break;

				case ReleaseFilter.Update:
					for (var i = 0; i < _cache.Releases.Count; ++i)
					{
						var release = _cache.Releases[i];
						if (release.Version > Cache.ExecutingRelease.Version)
							_releases.Add(release);
					}
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(_releaseFilter), _releaseFilter, null);
			}

			_releases.Sort((a, b) => b.CompareTo(a));

			FilterReleases();

			if (_filteredReleases.Count > 0)
			{
				if (_releaseListView.selectedItem == null)
					_releaseListView.SetSelection(0);
				else
					RefreshNotes();
			}
			else
			{
				_releaseListView.ClearSelection();
			}

			if (_errorMessageRoot != null)
				_errorMessageRoot.style.display = _cache.HasMissingNotes() ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void FilterReleases()
		{
			_filteredReleases.Clear();
			if (_filteredReleases.Capacity < _releases.Count)
				_filteredReleases.Capacity = _releases.Count;

			bool Filter(Release release) =>
				string.IsNullOrWhiteSpace(_releaseSearchText) ||
				release.Version.ToString().IndexOf(_releaseSearchText, StringComparison.InvariantCultureIgnoreCase) != -1 ||
				release.Type.ToString().IndexOf(_releaseSearchText, StringComparison.InvariantCultureIgnoreCase) != -1 ||
				release.Stream.ToString().IndexOf(_releaseSearchText, StringComparison.InvariantCultureIgnoreCase) != -1;

			foreach (var release in _releases)
				if (Filter(release))
					_filteredReleases.Add(release);

			_releaseListView.Rebuild();
		}

		private void RefreshNotes()
		{
			_releaseNotes.Clear();
			int selectItemCount = 0;
			foreach (var index in _releaseListView.selectedIndices)
			{
				var release = _filteredReleases[index];
				foreach (var note in release.Notes)
					_releaseNotes.Add(new ReleaseNoteItem { Note = note, Style = ReleaseNoteController.Style.Description, Version = release.Version });
				++selectItemCount;
			}
			_areMultipleReleaseSelected = selectItemCount > 1;
			FilterReleaseNotes();
		}

		private void FilterReleaseNotes()
		{
			_filteredReleaseNotes.Clear();
			if (_filteredReleaseNotes.Capacity < _releaseNotes.Count)
				_filteredReleaseNotes.Capacity = _releaseNotes.Count;

			bool Filter(ReleaseNote note) =>
				string.IsNullOrWhiteSpace(_releaseNotesSearchText) ||
				note.Tag.IndexOf(_releaseNotesSearchText, StringComparison.InvariantCultureIgnoreCase) != -1 ||
				note.Description.IndexOf(_releaseNotesSearchText, StringComparison.InvariantCultureIgnoreCase) != -1;

			foreach (var item in _releaseNotes)
				if (Filter(item.Note))
					_filteredReleaseNotes.Add(item);

			_filteredReleaseNotes.Sort((a, b) =>
			{
				var result = a.Note.Category.CompareTo(b.Note.Category);
				return (result != 0) ? result : a.Note.Tag.CompareTo(b.Note.Tag);
			});

			string category = null;
			for (int i = 0; i < _filteredReleaseNotes.Count; ++i)
			{
				var item = _filteredReleaseNotes[i];
				if (item.Note.Category != category)
				{
					_filteredReleaseNotes.Insert(i++, new ReleaseNoteItem { Style = ReleaseNoteController.Style.Category, Note = item.Note });
					category = item.Note.Category;
				}
			}

			_releaseNotesListView.Rebuild();
		}
	}
}