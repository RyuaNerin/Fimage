using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerBeacon.Json;
using RyuaNerin.OAuth;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Fimage
{
	class Program
	{
		static string AppToken = "nFtO3EHY4qqfyZEIlZs5AQ";
		static string AppSecret = "BWXvhmTK6eqtMmvrcrk6Xn3y12PgTZkCGPpacmBFlj8";
		static string UserId;
		static string UserName;

		static Twitter tw;

		public static bool Setting_Mp4 = false;
		static bool Setting_Convertable = false;
		static bool Setting_Convert = false;
		static bool Setting_Unfav = false;
		static bool Setting_UserPath = false;

		static IList<string> lstCache = new List<string>();

		static WebClient wc;

		static string PathExe = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		static string PathDir = Path.Combine(PathExe, "Fimage");
		static string PathTemp = Path.Combine(PathDir, "temp");
		static string PathFFmpeg = Path.Combine(PathExe, "ffmpeg.exe");

		static void Load()
		{
			string path = Path.Combine(PathDir, Path.ChangeExtension(UserName, ".cache"));
			
			byte[] buffInt = new byte[4];
			int len;
			byte[] buffStr = new byte[4096];

			if (File.Exists(path))
			{
				using (Stream stream = new FileStream(path, FileMode.Open))
				{
					while (stream.Position < stream.Length)
					{
						stream.Read(buffInt, 0, 4);
						len = BitConverter.ToInt32(buffInt, 0);

						stream.Read(buffStr, 0, len);

						lstCache.Add(Encoding.UTF8.GetString(buffStr, 0, len));
					}

					stream.Close();
				}
			}
		}
		static void Save()
		{
			string path = Path.Combine(PathDir, Path.ChangeExtension(UserName, ".cache"));
			string temp = Path.Combine(PathDir, Path.ChangeExtension(UserName, ".cache.temp"));

			byte[] buffInt;
			byte[] buffStr;

			using (Stream stream = new FileStream(temp, FileMode.Create))
			{
				stream.SetLength(0);
					
				for (int i = 0; i < lstCache.Count; ++i)
				{
					buffInt = BitConverter.GetBytes(lstCache[i].Length);
					buffStr = Encoding.UTF8.GetBytes(lstCache[i]);

					stream.Write(buffInt, 0, 4);
					stream.Write(buffStr, 0, buffStr.Length);
				}

				stream.Flush();
				stream.Close();
			}

			File.Delete(path);
			File.Move(temp, path);
		}

		static void Main(string[] args)
		{
			wc = new WebClient();
			wc.Encoding = Encoding.UTF8;

			Console.WriteLine("Fimage v1");
			Console.WriteLine("CopyRight (C) 2014, RyuaNerin");

			Setting_Convertable = File.Exists(PathFFmpeg);
			if (Setting_Convertable)
				Console.WriteLine("FFMpeg 가 설치되어 있습니다. mp4 to gif 가능");

			tw = new Twitter(AppToken, AppSecret);

			if (!GetToken() && End())
				return;

			Load();

			Console.WriteLine("{0} Images cached", lstCache.Count);

			Console.WriteLine("Read downloaded log");

			Setting();
			
			GetFav();

			Console.WriteLine("저장이 완료되었습니다");

			End();
		}

		static bool End()
		{
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("아무 키나 누르면 종료됩니다");
			Console.ReadKey();
			return true;
		}

		static bool GetToken()
		{
			Console.WriteLine();

			Console.Write("토큰 얻는중...");

			if (!TwitterOAuth.request_token(tw))
			{
				Console.WriteLine("오류!");
				return false;
			}
			Console.WriteLine();
			System.Diagnostics.Process.Start("explorer", String.Format("\"https://api.twitter.com/oauth/authenticate?oauth_token={0}\"", tw.UserToken));

			Console.Write("Pin 번호 : ");
			if (!TwitterOAuth.access_token(tw, Console.ReadLine(), out UserId, out UserName))
			{
				Console.WriteLine("잘못된 Pin 번호입니다!");
				return false;
			}
			Console.WriteLine();

			return true;
		}

		static void Setting()
		{
			char k;

			Console.WriteLine();

			Console.CursorVisible = false;

			Console.Write("Mp4 파일 저장             : (Y) / N : ");
			k = Console.ReadKey().KeyChar;
			Setting_Mp4 = !(k == 'n' || k == 'N');
			Console.WriteLine(Setting_Mp4 ? "Y" : "N");

			if (Setting_Convertable)
			{
				Console.Write("MP4 파일을 Gif 로 변환    : (Y) / N : ");
				k = Console.ReadKey().KeyChar;
				Setting_Convert = (k == 'Y' || k == 'y');
				Console.WriteLine(Setting_Convert ? "Y" : "N");
			}
			else
			{
				Setting_Convert = false;
			}

			Console.Write("언페이버릿하지 않음       : (Y) / N : ");
			k = Console.ReadKey().KeyChar;
			Setting_Unfav = (k == 'n' || k == 'N');
			Console.WriteLine(Setting_Unfav ? "N" : "Y");

			Console.Write("유저별로 분류             : (Y) / N : ");
			k = Console.ReadKey().KeyChar;
			Setting_UserPath = !(k == 'n' || k == 'N');
			Console.WriteLine(Setting_UserPath ? "Y" : "N");

			Console.WriteLine();
		}

		static void GetFav()
		{
			Console.WriteLine();
			Console.WriteLine("페이버릿 리스트 불러오는중");

			string body = null;
			JsonArray ja;
			JsonObject jo;
			int count = 0;

			long max_id = 0;

			int countNow = 0;
			int countTotal = 0;

			bool t = true;
			bool suc = false;
			
			//////////////////////////////////////////////////////////////////////////

			// 전체 이미지 수 얻는 부분

			do 
			{
				try
				{
					body = tw.Call("GET", "https://api.twitter.com/1.1/users/show.json", new { user_id = UserId, include_entities = true });

					jo = new JsonObject(body);

					countTotal = jo.GetInt32("favourites_count");

					if (countTotal > 3200)
						countTotal = 3200;

					suc = true;
				}
				catch
				{
					if (t)
					{
						Console.WriteLine("API 리밋 해제를 기다리는중입니다");
						t = false;
					}
					suc = false;

					Thread.Sleep(60 * 1000);
				}
			} while (!suc);



			//////////////////////////////////////////////////////////////////////////

			int i;
			do
			{
				while(true)
				{
					try
					{
						if (max_id == 0)
							body = tw.Call("GET", "https://api.twitter.com/1.1/favorites/list.json", new { user_id  = UserId, count = 200 });
						else
							body = tw.Call("GET", "https://api.twitter.com/1.1/favorites/list.json", new { user_id  = UserId, count = 200, max_id = max_id });

						break;
					}
					catch
					{
						if (t)
						{
							Console.WriteLine("API 리밋 해제를 기다리는중입니다");
							t = false;
						}
						Thread.Sleep(60 * 1000);	
					}
				}

				ja = new JsonArray(body);
				count = ja.Count;

				for (i = 0; i < ja.Count; ++i)
				{
					jo = (JsonObject)ja[i];
					max_id = jo.GetInt64("id");

					Tweet(jo, countNow++, countTotal);
				}

				t = true;
			} while (count == 200);
		}

		static void Tweet(JsonObject jsonObject, int now, int total)
		{
			string id = jsonObject.GetJsonObject("user").GetString("screen_name");

			JsonObject joEntities = jsonObject.GetJsonObject("entities");

			if (joEntities != null)
			{
				JsonArray ja;
				JsonObject jo;
				int j;

				ja = joEntities.GetJsonArray("media");
				if (ja != null)
				{
					for (j = 0; j < ja.Count; ++j)
					{
						jo = (JsonObject)ja[j];
						Download(now, total, id, jo.GetString("id_str"), jo.GetString("media_url_https"), DateParse(jsonObject.GetString("created_at")), true);
					}
				}

				ja = joEntities.GetJsonArray("urls");
				if (ja != null)
				{
					for (j = 0; j < ja.Count; ++j)
					{
						jo = (JsonObject)ja[j];
						Download(now, total, id, jo.GetString("id_str"), jo.GetString("expanded_url"), DateParse(jsonObject.GetString("created_at")), false);
					}
				}
			}
		}
		static DateTime DateParse(string s)
		{
			return DateTime.ParseExact(s, "ddd MMM dd HH:mm:ss zz00 yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
		}

		static void Download(int now, int total, string userid, string tweetId, string uriOrig, DateTime date, bool isMedia)
		{
			Console.Write("[{0} / {1}] {2} ", now, total, uriOrig.Replace("https://", "").Replace("http://", ""));

			if (lstCache.IndexOf(uriOrig) >= 0)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("[Cached]");
				Console.ForegroundColor = ConsoleColor.Gray;
				return;
			}

			string uri = isMedia ? Media.GetOrig(uriOrig) : Media.GetURL(wc, uriOrig);
			if (uri == null)
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("[Pass]");
				Console.ForegroundColor = ConsoleColor.Gray;
				return;
			}
			
			string temp = Path.Combine(PathTemp, MakeTempString());
			Directory.CreateDirectory(PathTemp);

			bool complete;
			try
			{
				wc.DownloadFile(uri, temp);
				complete = true;
			}
			catch
			{
				File.Delete(temp);
				complete = false;
			}

			if (complete)
			{
				string path = Setting_UserPath ? Path.Combine(PathDir, userid) : PathDir;
				Directory.CreateDirectory(path);

				path = Path.Combine(path, GetFileName(uri));

				try
				{
					File.Move(temp, path);

					if (Setting_Convert && path.EndsWith(".mp4"))
					{
						using (Process p = new Process())
						{
							p.StartInfo =
						new ProcessStartInfo(
									PathFFmpeg,
									string.Format("-i \"{0}\" -pix_fmt rgb24 \"{1}\"", path, Path.ChangeExtension(path, ".gif"))
								)
								{
									CreateNoWindow = true,
									WindowStyle = ProcessWindowStyle.Hidden
								};

							p.Start();
							p.WaitForExit();
							p.Dispose();
						}
					}

					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine("[OK]");
					Console.ForegroundColor = ConsoleColor.Gray;
				}
				catch
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("[Dup]");
					Console.ForegroundColor = ConsoleColor.Gray;
				}

				File.SetCreationTime(path, date.ToLocalTime());
				File.SetLastAccessTime(path, date.ToLocalTime());
				File.SetLastWriteTime(path, date.ToLocalTime());

				lstCache.Add(uriOrig);
				Save();

				if (Setting_Unfav)
					tw.Call("post", "https://api.twitter.com/1.1/favorites/destroy.json", new { id = tweetId });
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(" [Error]");
				Console.ForegroundColor = ConsoleColor.Gray;
			}
		}

		private static Regex regFilename = new Regex("filename=\"?([^\";]+)\"?", RegexOptions.Compiled);
		public static string GetFileName(string uri)
		{
			string name = null;
			
			try
			{
				name = wc.ResponseHeaders["Content-Disposition"];
				name = Regex.Match(name, "filename=\"?([^\";]+)\"?").Groups[1].Value;
			}
			catch
			{
				name = new Uri(uri).AbsolutePath;
				int k = name.LastIndexOf('/');
				if (k >= 0)
					name = name.Substring(k + 1);

				if (name.EndsWith(":orig"))
					name = name.Substring(0, name.IndexOf(":orig"));
			}

			return name;
		}

		public static Random _random = new Random(DateTime.UtcNow.Millisecond);
		private static char[] _tempChars = {
			'A', 'B', 'C', 'D', 'E',
			'F', 'G', 'H', 'I', 'J',
			'K', 'L', 'M', 'N', 'O',
			'P', 'Q', 'R', 'S', 'T',
			'U', 'V', 'W', 'X', 'Y',
			'Z',
			'0', '1', '2', '3', '4',
			'5', '6', '7', '8', '9'
		};
		public static string MakeTempString()
		{
			StringBuilder stringBuilder = new StringBuilder(30);

			for (int i = 0; i < 30; ++i)
				stringBuilder.Append(_tempChars[_random.Next(0, _tempChars.Length)]);

			return stringBuilder.ToString();
		}
	}
}
