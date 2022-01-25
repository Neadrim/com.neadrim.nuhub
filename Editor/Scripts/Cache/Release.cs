using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Neadrim.NuHub
{
	internal enum ReleaseStream
	{
		Tech,
		Lts
	}

	internal enum ReleaseType
	{
		Alpha,
		Beta,
		Official
	}

	[Serializable]
	internal class Release : IEquatable<Release>, IComparable<Release>, ISerializationCallbackReceiver
	{
		private static readonly Regex VersionRegex = new Regex(@"^(?<version>\d+\.\d+\.\d+)((?<type>[a-z]+)\d+)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		public Version Version;
		public ReleaseType Type;
		public ReleaseStream Stream;
		public DateTime ReleaseDate;
		public Uri HubUri;
		public Uri NotesUri;
		public List<ReleaseNote> Notes = new List<ReleaseNote>();

		public override int GetHashCode() => Version.GetHashCode();
		public int CompareTo(Release other) => other == null ? 1 : Version.CompareTo(other.Version);

		public override bool Equals(object other) => other is Release otherVersion && Equals(otherVersion);
		public bool Equals(Release other) => other != null && Version != null && Type == other.Type && Version.Equals(other.Version);
		public override string ToString() => Version != null ? Version.ToString() : "Unknown";

		[SerializeField] private string _serializedVersion;
		[SerializeField] private long _serializedReleaseDate;
		[SerializeField] private string _serializedHubUri;
		[SerializeField] private string _serializedNotesUri;

		public static bool TryParse(string unityVersion, out Release release)
		{
			var match = VersionRegex.Match(unityVersion);
			if (match.Success && Version.TryParse(match.Groups["version"].Value, out var version))
			{
				release = new Release
				{
					Version = version,
					Type = match.Groups.Count > 1 ? GetVersionType(match.Groups["type"].Value) : ReleaseType.Official
				};
				return true;
			}
			release = null;
			return false;
		}

		private static ReleaseType GetVersionType(string versionLetter)
		{
			return versionLetter switch
			{
				"f" => ReleaseType.Official,
				"b" => ReleaseType.Beta,
				"a" => ReleaseType.Alpha,
				_ => ReleaseType.Official
			};
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			_serializedVersion = Version?.ToString();
			_serializedReleaseDate = ReleaseDate.ToBinary();
			_serializedNotesUri = NotesUri?.AbsoluteUri;
			_serializedHubUri = HubUri?.AbsoluteUri;
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			if (!string.IsNullOrEmpty(_serializedVersion))
			{
				try
				{
					Version = new Version(_serializedVersion);
				}
				catch (Exception e)
				{
					Debug.LogError($"Failed to read serialized version {_serializedVersion}: \n{e}");
				}

			}
			ReleaseDate = DateTime.FromBinary(_serializedReleaseDate);
			if (!string.IsNullOrEmpty(_serializedNotesUri))
				NotesUri = new Uri(_serializedNotesUri);
			if (!string.IsNullOrEmpty(_serializedHubUri))
				HubUri = new Uri(_serializedHubUri);
		}

		public bool HasReleaseNotes => Notes != null && Notes.Count > 0;
	}
}
