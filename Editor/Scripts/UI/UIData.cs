using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neadrim.NuHub
{
	internal class UIData : ScriptableObject
	{
		[field: SerializeField] public VisualTreeAsset NuHub { get; private set; }
		[field: SerializeField] public VisualTreeAsset ReleaseInfo { get; private set; }
		[field: SerializeField] public VisualTreeAsset ReleaseNote { get; private set; }

		[field: SerializeField] public Texture2D DownloadIcon { get; private set; }
		[field: SerializeField] public Texture2D InstalledIcon { get; private set; }
		[field: SerializeField] public Texture2D UpdateAvailableIcon { get; private set; }

		static UIData _instance;
		public static UIData Instance
		{
			get
			{
				if (!_instance)
				{
					var guids = AssetDatabase.FindAssets("t:" + nameof(UIData));
					if (guids.Length == 0)
						throw new System.IO.FileNotFoundException($"No asset of type {nameof(UIData)} was found.");
					_instance = AssetDatabase.LoadAssetAtPath<UIData>(AssetDatabase.GUIDToAssetPath(guids[0]));
				}
				return _instance;
			}
		}
	}
}
