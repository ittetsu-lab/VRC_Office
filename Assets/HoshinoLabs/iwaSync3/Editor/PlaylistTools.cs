using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HoshinoLabs.IwaSync3
{
    internal static class PlaylistTools
    {
        static Task _task = null;
        static CancellationTokenSource _cancellationTokenSource = null;

        public static bool IsCompleted => _task == null || _task.IsCompleted;

        public static void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            Cancel();
        }

        static bool IsValidURL(string url)
        {
            return url.StartsWith("http://www.youtube.com/", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://www.youtube.com/", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("http://youtu.be/", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://youtu.be/", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidVideoURL(string url)
        {
            return IsValidURL(url) && url.Contains("/watch?") && url.Contains("v=");
        }

        public static bool IsValidPlayListURL(string url)
        {
            return IsValidURL (url) && url.Contains("list=");
        }

        static string ResolveURL(string videoId)
        {
            return $"https://www.youtube.com/watch?v={videoId}";
        }

        static string ResolveVideoID(string url)
        {
            var match = Regex.Match(url, @"v=([^&]+)");
            if (!match.Success)
                return null;
            return match.Groups[1].Value;
        }

        public static async void ResolveTracks(Playlist self)
        {
            EditorUtility.DisplayProgressBar(Udon.IwaSync3.IwaSync3.APP_NAME, $"動画情報を取得中", 0f);
            using (_cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    await (_task = ResolveTracks(self, _cancellationTokenSource.Token));
                }
                catch (OperationCanceledException)
                {

                }
            }
            EditorUtility.ClearProgressBar();
        }

        static async Task ResolveTracks(Playlist self,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var serializedObject = new SerializedObject(self);
            var tracksProperty = serializedObject.FindProperty("tracks");

            for (var i = 0; i < tracksProperty.arraySize; i++)
            {
                var progress = (float)i / tracksProperty.arraySize;
                EditorUtility.DisplayProgressBar(Udon.IwaSync3.IwaSync3.APP_NAME, $"動画情報を取得中 ({i + 1}/{tracksProperty.arraySize})", progress);

                var trackProperty = tracksProperty.GetArrayElementAtIndex(i);
                var url = trackProperty.FindPropertyRelative("url").stringValue;
                if (!IsValidVideoURL(url))
                    continue;
                var track = await ResolveTrack(trackProperty.FindPropertyRelative("url").stringValue, cancellationToken);
                if(track == null)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog(Udon.IwaSync3.IwaSync3.APP_NAME, "動画情報の取得に失敗しました", "OK");
                    return;
                }
                //trackProperty.FindPropertyRelative("mode").intValue = (int)track.mode;
                trackProperty.FindPropertyRelative("title").stringValue = track.title;
                trackProperty.FindPropertyRelative("url").stringValue = track.url;
            }

            if (self)
            {
                if (serializedObject.ApplyModifiedProperties())
                {
                    var editor = (PlaylistEditor)Editor.CreateEditor(self);
                    editor.ApplyModifiedProperties();
                    GameObject.DestroyImmediate(editor);
                }
            }
        }

        static async Task<Track> ResolveTrack(string url,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var videoId = ResolveVideoID(url);
            if (videoId == null)
                return null;

            var track = new Track
            {
                url = url
            };

            var error = string.Empty;
            await YoutubeDLHelper.Execute(url, null, e =>
                {
                }, e =>
                {
                    error += e.Data;
                    var match = Regex.Match(e.Data, $@"^ERROR: unable to open for writing: \[Errno 13\] Permission denied: '(.+)-{videoId}.mp4.part'$");
                    if (match.Success)
                    {
                        var title = match.Groups[1].Value;
                        track.title = title;
                    }
                }, cancellationToken
            );

            if (string.IsNullOrEmpty(error))
                return null;

            return track;
        }

        public static async void ResolvePlaylist(Playlist self)
        {
            EditorUtility.DisplayProgressBar(Udon.IwaSync3.IwaSync3.APP_NAME, "プレイリストを取得中", 0f);
            using (_cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    await (_task = ResolvePlaylist(self, _cancellationTokenSource.Token));
                }
                catch (OperationCanceledException)
                {

                }
            }
            EditorUtility.ClearProgressBar();
        }

        static async Task ResolvePlaylist(Playlist self,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var serializedObject = new SerializedObject(self);
            var playlistUrlProperty = serializedObject.FindProperty("playlistUrl");
            var tracksProperty = serializedObject.FindProperty("tracks");

            var tracks = new List<Track>();

            var error = string.Empty;
            var context = SynchronizationContext.Current;
            await YoutubeDLHelper.Execute(playlistUrlProperty.stringValue, new[]
                {
                    $"--skip-download",
                    $"--no-check-certificate"
                }, e =>
                {
                    var match1 = Regex.Match(e.Data, @"^\[download] Downloading video (\d+) of (\d+)$");
                    if (match1.Success)
                    {
                        var left = int.Parse(match1.Groups[1].Value);
                        var right = int.Parse(match1.Groups[2].Value);
                        var progress = (float)left / right;
                        context.Post(_ => EditorUtility.DisplayProgressBar(Udon.IwaSync3.IwaSync3.APP_NAME, $"プレイリストを取得中 ({left}/{right})", progress), null);
                    }
                    var match2 = Regex.Match(e.Data, @"^\[youtube\] ([^&\s]+): Downloading webpage$");
                    if (match2.Success)
                    {
                        var videoId = match2.Groups[1].Value;
                        var track = new Track
                        {
                            url = ResolveURL(videoId)
                        };
                        tracks.Add(track);
                    }
                }, e =>
                {
                    error += e.Data;
                }, cancellationToken
            );

            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(Udon.IwaSync3.IwaSync3.APP_NAME, "プレイリストの取得に失敗しました\nプレイリストが非公開になっていないか確認して下さい", "OK");
                return;
            }

            tracksProperty.arraySize = tracks.Count;
            for (var i = 0; i < tracksProperty.arraySize; i++)
            {
                var trackProperty = tracksProperty.GetArrayElementAtIndex(i);
                var track = tracks[i];
                //trackProperty.FindPropertyRelative("mode").intValue = (int)track.mode;
                //trackProperty.FindPropertyRelative("title").stringValue = track.title;
                trackProperty.FindPropertyRelative("url").stringValue = track.url;
            }

            if (self)
            {
                serializedObject.ApplyModifiedProperties();
                await ResolveTracks(self, cancellationToken);
            }
        }
    }
}
