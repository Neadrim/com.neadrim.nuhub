using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Neadrim.NuHub
{
	internal static class UnityWeb
	{
		public static class Links
		{
			public static readonly Uri UnityDomain = new Uri("https://unity3d.com");
			public static readonly Uri Archive = new Uri(UnityDomain, "/get-unity/download/archive");
			public static readonly Uri LtsReleases = new Uri(UnityDomain, "/unity/qa/lts-releases");
			public static Uri Absolute(string relativeUrl) => new Uri(UnityDomain, relativeUrl);
		}

		public static async Task<string> GetArchiveAsync() => await GetAsync(Links.Archive);

		public static async Task<string> GetLtsReleaseAsync() => await GetAsync(Links.LtsReleases);

		public static async Task<string> GetAsync(Uri uri)
		{
			using var request = UnityWebRequest.Get(uri);
			request.SetRequestHeader("User-Agent", $"UnityEditor/{Application.unityVersion} (UPM {nameof(Neadrim)}.{nameof(NuHub)})");
			await request.SendWebRequest();
			switch (request.result)
			{
				case UnityWebRequest.Result.Success:
					return request.downloadHandler.text;

				case UnityWebRequest.Result.ConnectionError:
				case UnityWebRequest.Result.DataProcessingError:
				case UnityWebRequest.Result.ProtocolError:
					throw new Exception(request.error);

				case UnityWebRequest.Result.InProgress:
				default:
					throw new Exception("Unhandled request result");
			}
		}
	}

	internal static class UnityWebRequestExtension
	{
		public static TaskAwaiter<UnityWebRequest.Result> GetAwaiter(this UnityWebRequestAsyncOperation webOperation)
		{
			var completionSource = new TaskCompletionSource<UnityWebRequest.Result>();
			if (webOperation.isDone)
				completionSource.TrySetResult(webOperation.webRequest.result);
			else
				webOperation.completed += asyncOp => completionSource.TrySetResult(webOperation.webRequest.result);

			return completionSource.Task.GetAwaiter();
		}
	}
}
