using System;
using HtmlAgilityPack;
using UnityEngine;

namespace Neadrim.NuHub
{
	internal static class ReleaseNoteParser
	{
		public static void Parse(string content, Release release)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(content);

			var releaseContentNode = doc.DocumentNode.SelectSingleNode("//div[@class='g12 nest full-release rel']/div/div[@class='w100 clear']");
			var divs = releaseContentNode.SelectNodes("div[@class='g12 markdown']");
			if (divs.Count < 2)
			{
				Debug.LogError($"Failed to parse release notes content");
				return;
			}

			string category = null;
			var releaseNotesParent = divs[1];

			foreach (var childNode in releaseNotesParent.ChildNodes)
			{
				if (childNode.NodeType != HtmlNodeType.Element || childNode.Name.Length != 2)
					continue;

				if (childNode.Name[0] == 'h')
				{
					category = childNode.InnerText;
					if (category.IndexOf("Known Issues", StringComparison.InvariantCultureIgnoreCase) != -1)
						category = "Known Issues";
				}
				else if (childNode.Name == "ul")
					ParseNotesBlocks(childNode, release, category);
			}
		}

		private static void ParseNotesBlocks(HtmlNode rootNode, Release version, string category)
		{
			var nodes = rootNode.SelectNodes("li");
			if (nodes == null)
			{
				Debug.LogError($"Failed to parse release notes in category {category}");
				return;
			}
			
			foreach (var node in nodes)
			{
				var note = ParseNote(node.InnerText);
				note.Category = category;
				version.Notes.Add(note);
			}
		}

		private static ReleaseNote ParseNote(string htmlText)
		{
			var text = HtmlEntity.DeEntitize(htmlText);

			if (text.StartsWith("com.", StringComparison.InvariantCultureIgnoreCase))
				return new ReleaseNote { Description = SingleLine(text), Tag = "Packages" };

			var tagIndex = text.IndexOf(':');
			if (tagIndex > 0 && tagIndex < 30)
			{
				var note = new ReleaseNote
				{
					Tag = text.Substring(0, tagIndex),
					Description = SingleLine(text.Substring(tagIndex + 1)).Trim()
				};

				var splitTagIndex = note.Tag.IndexOf("-");
				if (splitTagIndex > 0)
				{
					// We may have found a note in an old format like: "(123456) - Editor: ..."
					// where the issue id was at the start instead of at the end, so we want to move it back into the description intead of the tag.
					var issueID = note.Tag.Substring(0, splitTagIndex).Trim();
					var startOffset = splitTagIndex + 1;
					note.Tag = note.Tag.Substring(startOffset, note.Tag.Length - startOffset).Trim();
					note.Description = issueID + " " + note.Description;
				}
				else if (!char.IsLetter(note.Tag[0]))
				{
					// We may have found a note in an old format like: "(123456) https://..."
					// where ":" was found, but no "-" to split into a valid tag.
					note.Tag = "Miscellaneous";
					note.Description = SingleLine(text);
				}
				return note;
			}

			return new ReleaseNote { Description = SingleLine(text), Tag = "Miscellaneous" };
		}

		private static string SingleLine(string text) => text.Replace("\n\r", " ").Replace('\n', ' ');
	}
}
