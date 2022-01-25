using HtmlAgilityPack;
using System;
using UnityEngine;

namespace Neadrim.NuHub
{
	internal static class ArchiveParser
	{
		public static void Parse(string content, ReleaseCache cache)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(content);
			var nodes = doc.DocumentNode.SelectNodes(@"//div[@class='row clear']");
			foreach (var node in nodes)
			{
				var metaNode = node.SelectSingleNode("div[@class='g2']");
				if (metaNode == null)
					continue;

				if (!ReadVersion(metaNode, out var newRelease))
				{
					Debug.LogError($"Failed to parse archive version: \n {node.InnerHtml}");					
					continue;
				}

				if (!ReleaseCache.IsSupported(newRelease.Version))
					continue;
				
				if (!cache.TryGet(newRelease, out var release))
				{
					release = newRelease;
					release.Stream = ReleaseStream.Tech;
					cache.Add(release);
				}

				if (ReadDate(metaNode, out var date))
					release.ReleaseDate = date;
				
				var linksNode = node.SelectSingleNode("div[@class='g10']");
				if (linksNode == null)
					continue;

				ReadLinks(linksNode, release);
			}
		}

		private static bool ReadVersion(HtmlNode node, out Release release)
		{
			var childNode = node.SelectSingleNode("div/h4");
			if (childNode?.InnerText == null)
			{
				release = null;
				return false;
			}

			return Release.TryParse(childNode.InnerText.Replace("Unity ", ""), out release);
		}

		private static bool ReadDate(HtmlNode node, out DateTime date)
		{
			var dateNode = node.SelectSingleNode("p[@class='mb0 cl']");
			if (dateNode?.InnerText == null)
			{
				date = new DateTime();
				return false;
			}
			return DateTime.TryParse(dateNode.InnerText, out date);
		}

		private static void ReadLinks(HtmlNode node, Release release)
		{
			var childNode = node.SelectSingleNode("a[@class='btn right']");
			if (childNode != null)
			{
				var url = childNode.GetAttributeValue("href", null);
				release.NotesUri = UnityWeb.Links.Absolute(url);
			}
			else
			{
				Debug.LogError($"[{release}] Failed to parse release notes URL");
			}

			childNode = node.SelectSingleNode("div[@class='sb right mr10']/a");
			if (childNode != null)
			{
				var url = childNode.GetAttributeValue("href", null);
				release.HubUri = new Uri(url);
			}
			else
			{
				Debug.LogError($"[{release}] Failed to parse Hub install URL");
			}
		}
	}
}
