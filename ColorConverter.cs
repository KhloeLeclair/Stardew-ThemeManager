﻿using System;
using System.Text.RegularExpressions;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SColor = System.Drawing.Color;

namespace Leclair.Stardew.ThemeManager
{
	/// <summary>
	/// ColorConverter is a JsonConverter for Newtonsoft.Json that has better
	/// support for deserializing colors from strings. It uses CSS's hex and
	/// rgb() syntaxes, while falling back to System.Drawing.ColorTranslator
	/// to parse color names.
	/// </summary>
	public class ColorConverter : JsonConverter
	{
		#region Color Parsing

		public static readonly Regex HEX_REGEX = new(@"^#([0-9a-f]{3,4}|(?:[0-9a-f]{2}){3,4})$", RegexOptions.IgnoreCase);
		public static readonly Regex RGB_REGEX = new(@"^\s*rgba?\s*\(\s*(\d+%?)\s*,\s*(\d+%?)\s*,\s*(\d+%?)(?:\s*,\s*(\d+%|[\d.]+))?\s*\)\s*$", RegexOptions.IgnoreCase);

		private static int HydrateRGBValue(string value)
		{
			if (value.EndsWith('%'))
				return (int)Math.Floor(Convert.ToInt32(value[0..^1]) / 100f * 255);

			return Convert.ToInt32(value);
		}

		/// <summary>
		/// Parse a CSS color string and return a Color. This supports hex
		/// and rgb() syntaxes for input, and falls back to 
		/// System.Drawing.ColorTranslator.FromHtml() for handling color
		/// names. HSL/HSV/etc. are not supported, only RGB.
		/// </summary>
		/// <param name="input">A CSS color string in hex, rgb, or name format</param>
		/// <returns>A Color object, or null if no valid color was present</returns>
		public static Color? ParseColor(string input)
		{
			if (string.IsNullOrEmpty(input))
				return null;

			int r = -1;
			int g = -1;
			int b = -1;
			int a = 255;

			// CSS hex format is:
			//   #RGB
			//   #RGBA
			//   #RRGGBB
			//   #RRGGBBAA
			// ColorTranslator.FromHtml does this wrong, so let's do it ourselves.
			var match = HEX_REGEX.Match(input);
			if (match.Success)
			{
				string value = match.Groups[1].Value;
				if (value.Length == 3 || value.Length == 4)
				{
					r = Convert.ToInt32(value[0..1], 16) * 17;
					g = Convert.ToInt32(value[1..2], 16) * 17;
					b = Convert.ToInt32(value[2..3], 16) * 17;

					if (value.Length == 4)
						a = Convert.ToInt32(value[3..4], 16) * 17;
				}

				if (value.Length == 6 || value.Length == 8)
				{
					r = Convert.ToInt32(value[0..2], 16);
					g = Convert.ToInt32(value[2..4], 16);
					b = Convert.ToInt32(value[4..6], 16);

					if (value.Length == 8)
						a = Convert.ToInt32(value[6..8], 16);
				}
			}

			// CSS rgb format
			match = RGB_REGEX.Match(input);
			if (match.Success)
			{
				r = HydrateRGBValue(match.Groups[1].Value);
				g = HydrateRGBValue(match.Groups[2].Value);
				b = HydrateRGBValue(match.Groups[3].Value);

				if (match.Groups[4].Success)
				{
					string value = match.Groups[4].Value;
					if (value.EndsWith('%'))
						a = HydrateRGBValue(value);
					else
					{
						float fval = Convert.ToSingle(value);
						if (fval >= 0)
							a = (int)Math.Floor(255 * fval);
					}
				}
			}

			// Did we assign r/g/b values?
			if (r != -1 && g != -1 && b != -1)
			{
				// ... and are they in range?
				if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255 && a >= 0 && a <= 255)
					return new Color(r, g, b, a);

				// If not in range, return null, it's invalid.
				return null;
			}

			// Fall back on ColorTranslator for handling color names so we don't need
			// to include our own lookup table.
			SColor color;
			try
			{
				color = System.Drawing.ColorTranslator.FromHtml(input);
			}
			catch (Exception)
			{
				return null;
			}

			if (color == SColor.Empty)
				return null;

			return new Color(color.R, color.G, color.B, color.A);
		}

        #endregion

        #region JsonConverter

        public override bool CanWrite => false;

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Color) || Nullable.GetUnderlyingType(objectType) == typeof(Color);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string path = reader.Path;
			switch (reader.TokenType)
			{
				case JsonToken.Null:
					return null;
				case JsonToken.String:
					return ReadString(JToken.Load(reader).Value<string>());
				case JsonToken.StartObject:
					return ReadObject(JObject.Load(reader), path);
				default:
					throw new JsonReaderException($"Can't parse Color? from {reader.TokenType} node (path: {reader.Path}).");
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new InvalidOperationException("This converter does not support writing.");
		}

		private static Color? ReadString(string value)
		{
			return ParseColor(value);
		}

		private static bool TryReadInt(JObject obj, string path, out int result)
		{
			if (!obj.TryGetValue(path, StringComparison.OrdinalIgnoreCase, out var token))
			{
				result = default;
				return false;
			}

			result = token.Value<int>();
			return true;
		}

		private static Color? ReadObject(JObject obj, string path)
		{
			try
			{
				if (!TryReadInt(obj, "R", out int R) ||
					!TryReadInt(obj, "G", out int G) ||
					!TryReadInt(obj, "B", out int B)
				)
					return null;

				if (TryReadInt(obj, "A", out int A))
					return new Color(R, G, B, A);

				return new Color(R, G, B);
			}
			catch (Exception ex)
			{
				throw new JsonReaderException($"Can't parse Color? from JSON object node (path: {path}).", ex);
			}
		}

		#endregion
	}
}
