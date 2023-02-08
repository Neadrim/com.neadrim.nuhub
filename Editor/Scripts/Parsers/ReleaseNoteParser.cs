using System;
using System.IdentityModel.Tokens;
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

			var releaseContentNode = doc.DocumentNode.SelectSingleNode("//div[@class='release-notes']");
			if (releaseContentNode == null)
			{
				Debug.LogError($"[{release.Version}] Failed to parse release notes");
				return;
			}

			string category = null;

			foreach (var childNode in releaseContentNode.ChildNodes)
			{
				if (childNode.NodeType != HtmlNodeType.Element || childNode.Name.Length != 2)
					continue;

				if (childNode.Name[0] == 'h')
				{
					category = childNode.InnerText;
					if (category.EndsWith("release notes", StringComparison.InvariantCultureIgnoreCase))
						continue;
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
				var p = node.SelectSingleNode("p");
				string text = p != null ? p.InnerText : node.InnerText;
				var note = ParseNote(text);
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
