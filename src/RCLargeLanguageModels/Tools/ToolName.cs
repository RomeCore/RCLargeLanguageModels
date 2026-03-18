using System;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// The utility class for working with tool names.
	/// </summary>
	public static class ToolName
	{
		/// <summary>
		/// Checks if the provided tool name is valid.
		/// A valid tool name must be at most 64 characters long and contain only alphanumeric characters and underscores.
		/// </summary>
		/// <param name="name">The tool name to check.</param>
		/// <returns><see langword="true"/> if the tool name is valid; otherwise, <see langword="false"/>.</returns>
		public static bool CheckValid(string name)
		{
			if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
				return false;
			
			for (int i = 0; i < name.Length; i++)
			{
				char c = name[i];
				if (c == '-' || c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
					continue;

				return false;
			}

			return true;
		}

		/// <summary>
		/// Ensures that the provided tool name is valid according to the specified rules.
		/// A valid tool name must be at most 64 characters long and contain only alphanumeric characters and underscores.
		/// If not, throws an <see cref="ArgumentException"/>.
		/// </summary>
		/// <param name="name">The tool name to validate.</param>
		/// <exception cref="ArgumentException">Thrown when the provided tool name is invalid.</exception>
		public static void EnsureValid(string name)
		{
			if (!CheckValid(name))
				throw new ArgumentException($"Invalid tool name '{name}'. " +
					$"Tool names must be up to 64 characters long and can only contain alphanumeric characters, " +
					$"dashes, and underscores.", nameof(name));
		}
	}
}