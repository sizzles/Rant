﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rant.Localization
{
	internal static class Txtres
	{
		public const string LanguageResourceNamespace = "Rant.Localization";
		public const string FallbackLanguageCode = "en-US";

		private static readonly Dictionary<string, Dictionary<string, string>> _languages = new Dictionary<string, Dictionary<string, string>>(); 
		private static Dictionary<string, string> _currentTable = new Dictionary<string, string>();
		private static string _langName = CultureInfo.CurrentCulture.Name;

		static Txtres()
		{
			try
			{
				var ass = Assembly.GetExecutingAssembly();
				var lang = CultureInfo.CurrentCulture.Name;
				using (var stream =
					ass.GetManifestResourceStream($"{LanguageResourceNamespace}.{CultureInfo.CurrentCulture.Name}.lang")
					?? ass.GetManifestResourceStream($"{LanguageResourceNamespace}.{lang = FallbackLanguageCode}.lang"))
				{
					if (stream == null)
					{
#if DEBUG
						Console.WriteLine($"Txtres error: Missing language definition Localization/{CultureInfo.CurrentCulture.Name}.lang");
#endif
						return;
					}

					LoadStringTableData(lang, stream, _currentTable);
				}
#if DEBUG
				Console.WriteLine($"Loaded string resources for {CultureInfo.CurrentCulture.Name}");
#endif
			}
			catch (Exception ex)
			{
#if DEBUG
				Console.WriteLine($"Txtres error: {ex.Message}");
#endif
			}
		}

		private static void LoadStringTableData(string lang, Stream stream, Dictionary<string, string> table)
		{
			using (var reader = new StreamReader(stream, Encoding.Default, true, 256, true))
			{
				loop:
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if (line == null || line.Length == 0) continue;
					var kv = line.Split(new[] { '=' }, 2);
					if (kv.Length != 2) continue;
					var key = kv[0].Trim();
					if (!key.All(c => Char.IsLetterOrDigit(c) || c == '-' || c == '_')) continue;
					var valueLiteral = kv[1].Trim();
					var sb = new StringBuilder();
					int i = 0;
					int len = valueLiteral.Length;
					while (i < len)
					{
						if ((i == 0 || i == valueLiteral.Length - 1))
						{
							if (valueLiteral[i] != '"') goto loop;
							i++;
							continue;
						}
						switch (valueLiteral[i])
						{
							case '\\':
								{
									if (i == valueLiteral.Length - 1) goto loop;
									switch (valueLiteral[i + 1])
									{
										case 'a':
											sb.Append('\a');
											break;
										case 'b':
											sb.Append('\b');
											break;
										case 'f':
											sb.Append('\f');
											break;
										case 'n':
											sb.Append('\n');
											break;
										case 'r':
											sb.Append('\r');
											break;
										case 't':
											sb.Append('\t');
											break;
										case 'v':
											sb.Append('\v');
											break;
										case 'u':
											{
												if (i + 5 >= valueLiteral.Length) goto loop;
												short code;
												if (!short.TryParse(valueLiteral.Substring(i + 1, 4),
													NumberStyles.AllowHexSpecifier,
													CultureInfo.InvariantCulture, out code)) goto loop;
												sb.Append((char)code);
												i += 6;
												continue;
											}
										default:
											sb.Append(valueLiteral[i + 1]);
											break;
									}
									i += 2;
									continue;
								}
							default:
								sb.Append(valueLiteral[i]);
								break;
						}
						i++;
					}
					table[key] = sb.ToString();
				}
				_languages[lang] = table;
			}
		}

		private static void CheckLanguage()
		{
			if (CultureInfo.CurrentCulture.Name == _langName) return;
			try
			{
				_langName = CultureInfo.CurrentCulture.Name;
				Dictionary<string, string> table;
				if (!_languages.TryGetValue(_langName, out table))
				{
					using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{LanguageResourceNamespace}.{_langName}.lang"))
					{
						if (stream == null) return;
						table = new Dictionary<string, string>();
						LoadStringTableData(_langName, stream, table);
					}
				}
				_currentTable = table;
			}
			catch (Exception ex)
			{
#if DEBUG
				Console.WriteLine($"Txtres error: {ex.Message}");
#endif
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void ForceLoad()
		{
		}

		public static string GetString(string name)
		{
			CheckLanguage();
			string str;
			return _currentTable.TryGetValue(name, out str) ? str : name;
		}

		public static string GetString(string name, params object[] args)
		{
			CheckLanguage();
			string str;
			try
			{
				return _currentTable.TryGetValue(name, out str) ? String.Format(str, args) : name;
			}
			catch
			{
				return "<Format Error>";
			}
		}
	}
}