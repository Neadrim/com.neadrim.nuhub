using UnityEngine.UIElements;

namespace Neadrim.NuHub
{
	internal class ReleaseNoteController
	{
		public enum Style
		{
			Category,
			Description
		}

		private ReleaseNote _releaseNote;
		private Style _style;

		public void Bind(VisualElement element, ReleaseNote releaseNote, Style style, System.Version version)
		{
			_releaseNote = releaseNote;
			_style = style;
			element.userData = this;

			var header = element.Q<VisualElement>("Header");
			var note = element.Q<VisualElement>("Note");

			switch (_style)
			{
				case Style.Category:
					header.style.display = DisplayStyle.Flex;
					note.style.display = DisplayStyle.None;
					var category = header.Q<Label>("Category");
					category.text = _releaseNote.Category.ToUpper();
					break;

				case Style.Description:
					header.style.display = DisplayStyle.None;
					note.style.display = DisplayStyle.Flex;
					var tag = element.Q<Label>("Tag");
					var description = element.Q<Label>("Description");
					tag.text = _releaseNote.Tag;
					description.text = _releaseNote.Description;
					var versionLabel = element.Q<Label>("Version");
					if (version != null)
					{
						versionLabel.style.display = DisplayStyle.Flex;
						versionLabel.text = version.ToString();
					}
					else
						versionLabel.style.display = DisplayStyle.None;
					break;

				default:
					break;
			}
		}

		public void Unbind()
		{
			_releaseNote = null;
		}
	}
}