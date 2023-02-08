using HtmlAgilityPack;
using System;
using UnityEngine;

namespace Neadrim.NuHub
{
	internal static class LtsParser
	{
		public static void Parse(string content, Cache cache)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(content);
			var releaseNodes = doc.DocumentNode.SelectNodes(@"//div[@class='component-releases-item__show__inner-header']");
			if (releaseNodes == null)
			{
				Debug.LogError($"Failed to parse LTS list");
				return;
			}

			foreach (var node in releaseNodes)
			{
				if (!ReadVersion(node, out var newRelease))
				{
					Debug.LogError($"Failed to parse LTS version: \n {node.InnerHtml}");
					continue;
				}
				
				if (!Cache.IsSupported(newRelease.Version))
					continue;

				if (!cache.TryGet(newRelease, out var release))
				{
					release = newRelease;
					cache.Add(release);
				}

				release.Stream = ReleaseStream.Lts;

				if (ReadDate(node, out var date))
					release.ReleaseDate = date;

				// TODO: Read the links and release notes from this page because the 
				// latest LTS version is not present in the archive page so this info is missing
			}
		}

		private static bool ReadVersion(HtmlNode node, out Release release)
		{
			var childNode = node.SelectSingleNode("h4[@class='component-releases-item__title']/span");
			if (childNode == null || childNode.InnerText == null)
			{
				release = null;
				return false;
			}

			return Release.TryParse(childNode.InnerText, out release);
		}

		private static bool ReadDate(HtmlNode node, out DateTime date)
		{
			var dateNode = node.SelectSingleNode("div/time");
			var dateTime = dateNode.GetAttributeValue("datetime", null) ?? dateNode.InnerText;
			if (dateTime == null)
			{
				date = new DateTime();
				return false;
			}
			return DateTime.TryParse(dateTime, out date);
		}
	}
}
