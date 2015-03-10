using System;
using System.Text.RegularExpressions;
using RyuaNerin.OAuth;

namespace Fimage
{
	internal static class TwitterOAuth
	{
		private static string ParseQueryStringParameter(string parameterName, string text)
		{
			Match expressionMatch = Regex.Match(text, string.Format(@"{0}=(?<value>[^&]+)", parameterName));

			if (!expressionMatch.Success)
				return null;

			return expressionMatch.Groups["value"].Value;
		}

		public static bool request_token(Twitter twitter)
		{
			try
			{
				lock (twitter)
				{
					string body = twitter.Call("POST", "https://api.twitter.com/oauth/request_token", "");

					twitter.UserToken = TwitterOAuth.ParseQueryStringParameter("oauth_token", body);
					twitter.UserSecret = TwitterOAuth.ParseQueryStringParameter("oauth_token_secret", body);
				}

				return true;
			}
			catch
			{
				return false;
			}
		}
		
		public static bool access_token(Twitter twitter, string oauthVerifier, out string outId, out string outName)
		{
			try
			{
				lock (twitter)
				{
					string body = twitter.Call("POST", "https://api.twitter.com/oauth/access_token", new { oauth_verifier = oauthVerifier });

					twitter.UserToken = TwitterOAuth.ParseQueryStringParameter("oauth_token", body);
					twitter.UserSecret = TwitterOAuth.ParseQueryStringParameter("oauth_token_secret", body);
					outId = TwitterOAuth.ParseQueryStringParameter("user_id", body);
					outName = TwitterOAuth.ParseQueryStringParameter("screen_name", body);
				}

				return true;
			}
			catch
			{
				outId = null;
				outName = null;
				return false;
			}
		}
	}
}
