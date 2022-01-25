using HtmlAgilityPack;
using System;
using UnityEngine;

namespace Neadrim.NuHub
{
	internal static class LtsParser
	{
		public static void Parse(string content, ReleaseCache cache)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(content);
			var releaseNodes = doc.DocumentNode.SelectNodes(@"//div[@class='g12 pt0 pb0 patch contextual-links-region']");
			foreach (var node in releaseNodes)
			{
				var metaNode = node.SelectSingleNode("div/div[@class='left meta']");
				if (!ReadVersion(metaNode, out var newRelease))
				{
					Debug.LogError($"Failed to parse LTS version: \n {node.InnerHtml}");
					continue;
				}
				
				if (!ReleaseCache.IsSupported(newRelease.Version))
					continue;

				if (!cache.TryGet(newRelease, out var release))
				{
					release = newRelease;
					cache.Add(release);
				}

				release.Stream = ReleaseStream.Lts;

				if (ReadDate(metaNode, out var date))
					release.ReleaseDate = date;
			}
		}

		private static bool ReadVersion(HtmlNode node, out Release release)
		{
			var childNode = node.SelectSingleNode("h4[@class='mb5 expand']");
			if (childNode == null || childNode.InnerText == null)
			{
				release = null;
				return false;
			}

			return Release.TryParse(childNode.InnerText.Replace("LTS Release ", ""), out release);
		}

		private static bool ReadDate(HtmlNode node, out DateTime date)
		{
			var dateNode = node.SelectSingleNode("p[@class='mb0 c-mg']");
			if (dateNode?.InnerText == null)
			{
				date = new DateTime();
				return false;
			}
			return DateTime.TryParse(dateNode.InnerText.Replace("Released: ", ""), out date);
		}
	}
}
