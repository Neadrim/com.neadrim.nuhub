using HtmlAgilityPack;
using System;
using UnityEngine;

namespace Neadrim.NuHub
{
	internal static class ArchiveParser
	{
		public static void Parse(string content, Cache cache)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(content);
			var nodes = doc.DocumentNode.SelectNodes(@"//div[@class='download-release-wrapper']");
			if (nodes == null)
			{
				Debug.LogError("Failed to parse archive");
				return;
			}

			foreach (var node in nodes)
			{
				if (!ReadVersion(node, out var newRelease))
				{
					Debug.LogError($"Failed to parse archive version: \n {node.InnerHtml}");
					continue;
				}

				if (!Cache.IsSupported(newRelease.Version))
					continue;
				
				if (!cache.TryGet(newRelease, out var release))
				{
					release = newRelease;
					release.Stream = ReleaseStream.Tech;
					cache.Add(release);
				}

				if (ReadDate(node, out var date))
					release.ReleaseDate = date;

				ReadLinks(node, release);
			}
		}

		private static bool ReadVersion(HtmlNode node, out Release release)
		{
			var titleNode = node.SelectSingleNode("div/div[@class='release-title']");
			if (titleNode?.InnerText == null)
			{
				release = null;
				return false;
			}

			return Release.TryParse(titleNode.InnerText.Replace("Unity ", ""), out release);
		}

		private static bool ReadDate(HtmlNode node, out DateTime date)
		{
			var dateNode = node.SelectSingleNode("div/div[@class='release-date']");
			if (dateNode?.InnerText == null)
			{
				date = new DateTime();
				return false;
			}
			return DateTime.TryParse(dateNode.InnerText, out date);
		}

		private static void ReadLinks(HtmlNode node, Release release)
		{
			var linksNode = node.SelectSingleNode("div[@class='release-links']");
			if (linksNode == null)
			{
				Debug.LogError($"[{release}] Failed to parse links");
				return;
			}

			foreach (var linkNode in linksNode.SelectNodes("div"))
			{
				var urlNode = linkNode.SelectSingleNode("a");
				if (urlNode != null)
				{
					var url = urlNode.GetAttributeValue("href", null);
					if (url.StartsWith("unityhub:", StringComparison.InvariantCultureIgnoreCase))
						release.HubUri = new Uri(url);
					else if (url.EndsWith("notes", StringComparison.InvariantCultureIgnoreCase))
						release.NotesUri = UnityWeb.Links.Absolute(url);
				}
			}
		}
	}
}
