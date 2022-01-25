using UnityEngine;
using UnityEngine.UIElements;

namespace Neadrim.NuHub
{
	internal class ReleaseInfoController
	{
		public enum InstallButtonStyle
		{
			DownloadAvailable,
			UpdateAvailable,
			Installed,
			Hidden
		}

		private Release _release;
		private Button _install;
		private Button _viewOnWeb;

		public void Bind(VisualElement element, Release release, InstallButtonStyle installButtonStyle)
		{
			_release = release;
			element.userData = this;

			var version = element.Q<Label>("Version");
			version.text = release.ToString().ToUpper();
			
			var stream = element.Q<Label>("Stream");
			stream.text = release.Stream.ToString().ToUpper();
			
			var date = element.Q<Label>("Date");
			date.text = release.ReleaseDate == default ? "?" : release.ReleaseDate.ToString("d MMM, yyyy");

			_install = element.Q<Button>("Install");
			var installIcon = _install.Q<VisualElement>("InstallIcon");
			switch (installButtonStyle)
			{
				case InstallButtonStyle.DownloadAvailable:
					installIcon.style.backgroundImage = UIData.Instance.DownloadIcon;
					_install.style.display = DisplayStyle.Flex;
					_install.tooltip = "Install";
					_install.clicked += OnInstall;
					_install.SetEnabled(_release.HubUri != null);
					break;

				case InstallButtonStyle.UpdateAvailable:
					installIcon.style.backgroundImage = UIData.Instance.UpdateAvailableIcon;
					_install.style.display = DisplayStyle.Flex;
					_install.tooltip = "Install Update";
					_install.clicked += OnInstall;
					_install.SetEnabled(_release.HubUri != null);
					break;

				case InstallButtonStyle.Hidden:
					_install.style.display = DisplayStyle.None;
					break;

				case InstallButtonStyle.Installed:
					installIcon.style.backgroundImage = UIData.Instance.InstalledIcon;
					_install.style.display = DisplayStyle.Flex;
					_install.tooltip = "Current Version";
					break;

				default:
					break;
			}

			_viewOnWeb = element.Q<Button>("ViewOnWeb");
			_viewOnWeb.clicked += OnViewOnWeb;
			_viewOnWeb.SetEnabled(_release.NotesUri != null);
		}

		public void Unbind()
		{
			_install.clicked -= OnInstall;
			_viewOnWeb.clicked -= OnViewOnWeb;
			_install = null;
			_viewOnWeb = null;
			_release = null;
		}

		void OnInstall() => Application.OpenURL(_release.HubUri.AbsoluteUri);
		void OnViewOnWeb() => Application.OpenURL(_release.NotesUri.AbsoluteUri);
	}
}