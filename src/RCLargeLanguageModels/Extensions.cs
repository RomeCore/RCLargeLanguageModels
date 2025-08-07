using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels
{
	public static class Extensions
	{
		// Я ебал в рот этот английский
		// Функция, которую я выдрочил до жопы при помощи Deepseek-r1
		/// <summary>
		/// Splits the string ignoring characters within quotes or parentheses
		/// </summary>
		/// <param name="str">The string to split</param>
		/// <param name="c">Separator character</param>
		/// <param name="maxCount">Maximum number of substrings to return</param>
		/// <param name="options">String splitting options</param>
		/// <param name="pairs">Specified character pairs to exclude unwanted splitting (default: <see cref="CharPairs.PairsDefaultSet"/>)</param>
		/// <param name="validate">If <see langword="true"/> and there is invalid string, 
		/// this function will throw and <see cref="InvalidOperationException"/></param>
		/// <returns>Array of substrings</returns>
		/// <exception cref="InvalidOperationException">Thrown if there is invalid string and validation check is enabled</exception>
		public static string[] SplitSmart(this string str, char c,
			int maxCount = -1, StringSplitOptions options = StringSplitOptions.None, 
			IEnumerable<CharPair> pairs = null, bool validate = false)
		{
			if (maxCount == 0)
				return Array.Empty<string>();
			if (maxCount == 1)
				return new string[] { str };

			pairs = pairs ?? CharPairs.PairsDefaultSet;
			var openCache = pairs.ToDictionary(p => p.Opening);
			var closeCache = pairs.ToDictionary(p => p.Closing);

			int lastId = 0;
			var result = new List<string>();
			Action<string> add = result.Add;

			if ((options & StringSplitOptions.RemoveEmptyEntries) != 0)
			{
				var prevAdd = add;
				add = s =>
				{
					if (!string.IsNullOrEmpty(s))
						prevAdd(s);
				};
			}

			var pairStack = new Stack<CharPair>();
			bool isEscaped = false;

			for (int i = 0; i < str.Length; i++)
			{
				char currentChar = str[i];

				if (currentChar == '\\' && !isEscaped)
				{
					isEscaped = true;
					continue;
				}
				if (isEscaped)
				{
					isEscaped = false;
					continue;
				}

				if (pairStack.Count > 0)
				{
					// Trying to close first, because opening and closing characters may be equal
					if (closeCache.TryGetValue(currentChar, out var pair))
					{
						if (pairStack.Peek() == pair)
							pairStack.Pop();

						else if (!pair.CharsEqual && validate)
							throw new InvalidOperationException($"Input string contains invalid closing character '{currentChar}'");
					}
					else if (openCache.TryGetValue(currentChar, out pair))
					{
						pairStack.Push(pair);
					}
				}
				else
				{
					if (openCache.TryGetValue(currentChar, out var pair))
						pairStack.Push(pair);
				}

				if (currentChar == c && pairStack.Count == 0)
				{
					add(str.Substring(lastId, i - lastId));
					if (maxCount > 0 && result.Count + 1 >= maxCount)
					{
						add(str.Substring(lastId));
						return result.ToArray();
					}
					lastId = i + 1;
				}
			}

			if (validate && pairStack.Count > 0)
				throw new InvalidOperationException("Input string must contain closing characters: " +
					$"{(string.Join(", ", pairStack.Select(p => p.Closing)))}");

			add(str.Substring(lastId));
			return result.ToArray();
		}

		/// <summary>
		/// Splits the string into lines using '\r\n', '\r' and '\n' as delimiters. It is a wrapper for <see cref="string.Split(string[], StringSplitOptions)"/> with predefined parameters.
		/// </summary>
		/// <param name="str">The string to split.</param>
		/// <param name="options">String splitting options. Default is <see cref="StringSplitOptions.RemoveEmptyEntries"/>.</param>
		/// <returns>Array of lines.</returns>
		public static string[] SplitLines(this string str, StringSplitOptions options = StringSplitOptions.None)
		{
			return str.Split(new[] { "\r\n", "\r", "\n" }, options);
		}

		/// <summary>
		/// Splits the string into lines using '\r\n', '\r' and '\n' as delimiters. It is a wrapper for <see cref="string.Split(string[], StringSplitOptions)"/> with predefined parameters.
		/// </summary>
		/// <param name="str">The string to split.</param>
		/// <param name="maxCount">Maximum number of substrings to return.</param>
		/// <param name="options">String splitting options. Default is <see cref="StringSplitOptions.RemoveEmptyEntries"/>.</param>
		/// <returns>Array of lines.</returns>
		public static string[] SplitLines(this string str, int maxCount, StringSplitOptions options = StringSplitOptions.None)
		{
			return str.Split(new[] { "\r\n", "\r", "\n" }, maxCount, options);
		}

		/// <summary>
		/// Adds an indentation to each line of the provided string.
		/// </summary>
		/// <param name="str">The string to indent.</param>
		/// <param name="indentString">The string to use as the indentation.</param>
		/// <param name="addIndentToFirstLine">Whether to add indentation to the first line. Default is <see langword="true"/>.</param>
		/// <returns></returns>
		public static string Indent(this string str, string indentString = "\t", bool addIndentToFirstLine = true)
		{
			if (string.IsNullOrEmpty(str))
				return str;

			var lines = str.SplitLines();
			var sb = new StringBuilder();

			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0 || addIndentToFirstLine)
					sb.Append(indentString);
				sb.Append(lines[i]);
				if (i < lines.Length - 1)
					sb.AppendLine();
			}

			return sb.ToString();

		}

		public static void FireAndForget(this Task task, Action onCancel = null, Action<Exception> onError = null, CancellationToken cancellationToken = default)
		{
			task.ContinueWith(t =>
			{
				if (t.IsCanceled)
					onCancel?.Invoke();
				if (t.IsFaulted)
					onError?.Invoke(t.Exception);
			}, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);
		}

		public static Task<TOut> Convert<TIn, TOut>(this Task<TIn> task)
		{
			return task.ContinueWith(t =>
			{
				if (t.IsCanceled)
					throw new TaskCanceledException();
				if (t.IsFaulted)
					throw t.Exception;

				return (TOut)(object)t.Result;
			}, TaskScheduler.Default);
		}

		public static Task<TOut> Convert<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> converter)
		{
			return task.ContinueWith(t =>
			{
				if (t.IsCanceled)
					throw new TaskCanceledException();
				if (t.IsFaulted)
					throw t.Exception;

				return converter(t.Result);
			}, TaskScheduler.Default);
		}

		private static readonly ConcurrentDictionary<Type, ulong> _enumDefinedFlags = new ConcurrentDictionary<Type, ulong>();

		private static ulong GetEnumDefinedFlagRange(Type enumType)
		{
			return _enumDefinedFlags.GetOrAdd(enumType, _ =>
			{
				ulong range = 0;
				foreach (var value in enumType.GetEnumValues())
					range |= System.Convert.ToUInt64(value);
				return range;
			});
		}

		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if enum is not defined for its range.
		/// For flag enums it gets all available flags defined in enum and checks if all enum flags is defined.
		/// </summary>
		/// <param name="en">The enum value to check.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="en"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static void ThrowIfOutOfRange(this Enum en)
		{
			if (en == null)
				throw new ArgumentNullException(nameof(en));

			var type = en.GetType();
			bool defined;

			if (type.IsDefined(typeof(FlagsAttribute), true))
			{
				ulong value = System.Convert.ToUInt64(en);
				ulong range = GetEnumDefinedFlagRange(type);

				defined = value == 0
					? Enum.IsDefined(type, en) // Check if 0 is explicitly defined
					: (value & range) == value;
			}
			else
				defined = Enum.IsDefined(type, en);

			if (!defined)
				throw new ArgumentOutOfRangeException(nameof(en));
		}
	}
}