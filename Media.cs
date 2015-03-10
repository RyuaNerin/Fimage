using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Fimage
{
	public static class Media
	{
		public static string GetOrig(string url)
		{
			return string.Format("{0}:orig", url);
		}

		static Regex rTwitpple = new Regex("p.twittple.jp/(.+)", RegexOptions.Compiled);
		static Regex rLockerz = new Regex("lockerz.com/s/(.+)", RegexOptions.Compiled);
		static Regex rTwitrpix = new Regex("twitrpix.com/(.+)", RegexOptions.Compiled);
		static Regex rImgly = new Regex("img.ly/(.+)", RegexOptions.Compiled);
		static Regex rPikchur = new Regex("pikchur.com/(.+)", RegexOptions.Compiled);
		static Regex rPkgd = new Regex("pk.gd/(.+)", RegexOptions.Compiled);
		static Regex rGrabby = new Regex("grab.by", RegexOptions.Compiled);
		static Regex rViame = new Regex("via.me/-(.+)", RegexOptions.Compiled);
		static Regex rViame2 = new Regex("\"media_url\" ?: ?\"([^\"]+)\"", RegexOptions.Compiled);
		static Regex rPuush = new Regex("puu.sh/[a-zA-Z0-9]+", RegexOptions.Compiled);
		static Regex rPckles = new Regex("pckles.com/.+", RegexOptions.Compiled);
		static Regex rTwitpic = new Regex("twitpic.com/([a-zA-z0-9]+)", RegexOptions.Compiled);
		static Regex rTwitterMp4 = new Regex("https?://twitter\\.com/[a-zA-Z0-9_]+/status/[0-9]+/photo/[0-9]+", RegexOptions.Compiled);
		static Regex rTwitterMp4Path = new Regex("\\<source video-src=\"([^\"]+)\" type=\"video/mp4\" class=\"source-mp4\"\\>", RegexOptions.Compiled);

		public static string GetURL(WebClient wc, string url)
		{
			Match m;

			m = rTwitterMp4.Match(url);
			if (m.Success && Program.Setting_Mp4)
			{
				try
				{
					m = rTwitterMp4Path.Match(wc.DownloadString(url));
					if (m.Success)
						return m.Groups[1].Value;
				}
				catch
				{

				}
			}

			m = rTwitpple.Match(url);
			if (m.Success)
				return String.Format("http://p.twpl.jp/show/orig/{0}", m.Groups[1].Value);

			m = rLockerz.Match(url);
			if (m.Success)
				return String.Format("http://api.plixi.com/api/tpapi.svc/imagefromurl?url=http://plixi.com/p/{0}&size=big", m.Groups[1].Value);

			m = rTwitrpix.Match(url);
			if (m.Success)
				return String.Format("http://img.twitrpix.com/{0}", m.Groups[1].Value);

			m = rImgly.Match(url);
			if (m.Success)
				return String.Format("http://img.ly/show/full/{0}", m.Groups[1].Value);

			m = rPikchur.Match(url);
			if (m.Success)
				return String.Format("http://img.pikchur.com/pic_{0}_l.jpg", m.Groups[1].Value);

			m = rPkgd.Match(url);
			if (m.Success)
				return String.Format("http://img.pikchur.com/pic_{0}_l.jpg", m.Groups[1].Value);

			m = rImgly.Match(url);
			if (m.Success)
				return String.Format("http://img.ly/show/full/{0}", m.Groups[1].Value);

			m = rImgly.Match(url);
			if (m.Success)
				return String.Format("http://img.ly/show/full/{0}", m.Groups[1].Value);

			m = rPuush.Match(url);
			if (m.Success)
				return url;

			m = rPckles.Match(url);
			if (m.Success)
				return url;

			m = rTwitpic.Match(url);
			if (m.Success)
				return String.Format("http://www.twitpic.com/show/full/{0}", m.Groups[1].Value);

			m = rViame.Match(url);
			if (m.Success)
			{
				try
				{
					string body = wc.DownloadString(String.Format("https://api.via.me/v1/posts/{0}", m.Groups[1].Value));

					m = rViame2.Match(url);

					if (m.Success)
						return m.Groups[1].Value;
				}
				catch
				{

				}
			}

			if ((url.IndexOf("tistory.com/image/") + url.IndexOf("tistory.com/original/")) >= 0)
				return url;

			if ((url.IndexOf(".jpg") + url.IndexOf(".png") + url.IndexOf(".bmp") + url.IndexOf(".gif")) >= 0)
				return url;

			return null;
		}
	}
}
