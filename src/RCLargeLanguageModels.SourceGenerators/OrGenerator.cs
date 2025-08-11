using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace RCLargeLanguageModels.SourceGenerators
{
	[Generator]
	public class OrGenerator : IIncrementalGenerator
	{
		public static Version Version { get; } = new Version("1.0.0");

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			context.RegisterPostInitializationOutput(ctx =>
			{
				for (int i = 3; i <= 8; i++)
				{
					ctx.AddSource($"Or_{i}.g.cs", GenerateOrStruct(i));
				}
			});
		}

		private string GenerateOrStruct(int typeParamCount)
		{
			// Generate type parameters (T1, T2, ... Tn)
			var typeParams = string.Join(", ", Enumerable.Range(1, typeParamCount).Select(i => $"T{i}"));
			var typeParamRefs = string.Join(", ", Enumerable.Range(1, typeParamCount).Select(i => $"<typeparamref name=\"T{i}\"/>"));

			// Generate fields and variant indices
			var fields = string.Join("\n\t\t", Enumerable.Range(1, typeParamCount).Select(i =>
				$"private readonly T{i} _value{i};"));

			var staticConstructor =
		$@"		static Or()
		{{
			{GenerateTypeCheck(typeParamCount, typeParams)}
		}}";

			var valueProperties = string.Join("\n\n", Enumerable.Range(1, typeParamCount).Select(i =>
			$@"		/// <summary>
		/// Gets the value of the currently active variant as <typeparamref name=""T{i}""/>. Throws an exception if the wrong type is accessed.
		/// </summary>
		public T{i} Value{i} =>
			_variantIndex == {i - 1}
				? _value{i}
				: throw new InvalidOperationException($""Active variant is not {{typeof(T{i})}}."");"));

			// Generate constructors
			var constructors = string.Join("\n\n", Enumerable.Range(1, typeParamCount).Select(i =>
		$@"		/// <summary>
		/// Initializes a new instance of the <see cref=""Or<{typeParams}>""/> struct with a value of type <typeparamref name=""T{i}""/>.
		/// </summary>
		/// <param name=""value"">The value to store.</param>
		/// <exception cref=""InvalidOperationException"">Thrown when any two type parameters are the same type.</exception>
		public Or(T{i} value)
		{{
			_variantIndex = {i - 1};
			_value{i} = value;
			{GenerateDefaultAssignments(typeParamCount, i)}
		}}"));

			// Generate Create methods
			var createMethods = string.Join("\n\n", Enumerable.Range(1, typeParamCount).Select(i =>
		$@"		/// <summary>
		/// Explicitly creates a new <see cref=""Or<{typeParams}>""/> struct with a value of type <typeparamref name=""T{i}""/>.
		/// </summary>
		/// <param name=""value"">The value to store.</param>
		/// <returns>A new created <see cref=""Or{{{typeParams}}}""/> with used <typeparamref name=""T{i}""/> type as active.</returns>
		public static Or<{typeParams}> CreateT{i}(T{i} value)
		{{
			return new Or<{typeParams}>(value);
		}}"));

			// Generate AsT methods
			var asMethods = string.Join("\n\n", Enumerable.Range(1, typeParamCount).Select(i =>
		$@"		/// <summary>
		/// Gets the value as <typeparamref name=""T{i}""/> or <see langword=""default""/> if the active variant is not <typeparamref name=""T{i}""/>.
		/// </summary>
		/// <returns>The value as <typeparamref name=""T{i}""/> or <see langword=""default""/>.</returns>
		public T{i} AsT{i}() =>
			_variantIndex == {i - 1}
				? _value{i}
				: default;"));

			// Generate TryGet methods
			var tryGetMethods = string.Join("\n\n", Enumerable.Range(1, typeParamCount).Select(i =>
		$@"		/// <summary>
		/// Attempts to get the value as <typeparamref name=""T{i}""/>.
		/// </summary>
		/// <param name=""value"">When this method returns, contains the value if the active variant is <typeparamref name=""T{i}""/>; otherwise, the default value.</param>
		/// <returns>true if the active variant is <typeparamref name=""T{i}""/>; otherwise, false.</returns>
		public bool TryGetT{i}(out T{i} value)
		{{
			if (_variantIndex == {i - 1})
			{{
				value = _value{i};
				return true;
			}}
			value = default!;
			return false;
		}}"));

			// Generate Is method
			var isMethod = $@"		/// <summary>
		/// Determines whether the active variant is of the specified type.
		/// </summary>
		/// <typeparam name=""T"">The type to check against.</typeparam>
		/// <returns>true if the active variant is of type <typeparamref name=""T""/>; otherwise, false.</returns>
		public bool Is<T>()
		{{
			{string.Join("\n\t\t\t", Enumerable.Range(1, typeParamCount).Select(i =>
						$"if (typeof(T) == typeof(T{i}) && VariantIndex == {i - 1}) return true;"))}

			return false;
		}}";

			// Generate Match method
			var matchParams = string.Join(", ", Enumerable.Range(1, typeParamCount)
				.Select(i => $"Func<T{i}, T> t{i}Handler"));

			var matchCases = string.Join("\n\t\t\t", Enumerable.Range(0, typeParamCount)
				.Select(i => $"case {i}:\n\t\t\t\treturn t{i + 1}Handler(_value{i + 1});"));

			var matchMethod = $@"		/// <summary>
		/// Matches the active variant and executes the corresponding handler function.
		/// </summary>
		/// <typeparam name=""T"">The return type of the handler functions.</typeparam>
		{string.Join("\n\t\t", Enumerable.Range(1, typeParamCount).Select(i =>
					$"/// <param name=\"t{i}Handler\">The function to execute if the active variant is <typeparamref name=\"T{i}\"/>.</param>"))}
		/// <returns>The result of the executed handler function.</returns>
		/// <exception cref=""InvalidOperationException"">Thrown when the variant index is invalid.</exception>
		public T Match<T>({matchParams})
		{{
			switch (VariantIndex)
			{{
			{matchCases}
			default:
				throw new InvalidOperationException(""Invalid variant index."");
			}}
		}}";

			// Generate Switch method
			var switchParams = string.Join(", ", Enumerable.Range(1, typeParamCount)
				.Select(i => $"Action<T{i}> t{i}Handler"));

			var switchCases = string.Join("\n\t\t\t", Enumerable.Range(0, typeParamCount)
				.Select(i => $"case {i}:\n\t\t\t\tt{i + 1}Handler(_value{i + 1});\n\t\t\t\treturn;"));

			var switchMethod = $@"		/// <summary>
		/// Executes an action based on the active variant.
		/// </summary>
		{string.Join("\n\t\t", Enumerable.Range(1, typeParamCount).Select(i =>
					$"/// <param name=\"t{i}Handler\">The action to execute if the active variant is <typeparamref name=\"T{i}\"/>.</param>"))}
		/// <exception cref=""InvalidOperationException"">Thrown when the variant index is invalid.</exception>
		public void Switch({switchParams})
		{{
			switch (VariantIndex)
			{{
			{switchCases}
			default:
				throw new InvalidOperationException(""Invalid variant index."");
			}}
		}}";

			// Generate Deconstruct method
			var deconstructParams = string.Join(", ", Enumerable.Range(1, typeParamCount)
				.Select(i => $"out T{i} value{i}"));

			var deconstructAssignments = string.Join("\n\t\t\t", Enumerable.Range(1, typeParamCount)
				.Select(i => $"value{i} = _value{i};"));

			var deconstructMethod = $@"		/// <summary>
		/// Deconstructs the active variant into output parameters; non-active variants will be set to <see langword=""default""/>.
		/// </summary>
		{string.Join("\n\t\t", Enumerable.Range(1, typeParamCount).Select(i =>
					$"/// <param name=\"value{i}\">The value to set if active type is <typeparamref name=\"T{i}\"/>.</param>"))}
		public void Deconstruct({deconstructParams})
		{{
			{deconstructAssignments}
		}}";

			// Generate equality methods
			var equalsCases = string.Join("\n\t\t\t", Enumerable.Range(0, typeParamCount)
				.Select(i => $"case {i}:\n\t\t\t\treturn Equals(_value{i + 1}, other._value{i + 1});"));

			var equalityMethods = $@"		public override bool Equals(object other)
		{{
			if (other is null)
				return false;

			if (other is Or<{typeParams}> otherOr)
				return Equals(otherOr);

			return false;
		}}

		public bool Equals(Or<{typeParams}> other)
		{{
			if (VariantIndex != other.VariantIndex)
				return false;

			switch (VariantIndex)
			{{
			{equalsCases}
			default:
				return false;
			}}
		}}

		public override int GetHashCode()
		{{
			int hc = VariantIndex.GetHashCode();
			switch (VariantIndex)
			{{
				{string.Join("\n\t\t\t\t", Enumerable.Range(0, typeParamCount)
							.Select(i => $"case {i}:\n\t\t\t\t\treturn (hc * 377) ^ _value{i + 1}?.GetHashCode() ?? 0;"))}
			default:
				return hc;
			}}
		}}";

			// Generate conversion operators
			var conversionOperators = string.Join("\n\n", Enumerable.Range(1, typeParamCount)
				.Select(i => $@"		public static implicit operator Or<{typeParams}>(T{i} value)
		{{
			return new Or<{typeParams}>(value);
		}}"));

			// Generate equality operators
			var equalityOperators = $@"		public static bool operator ==(Or<{typeParams}> left, Or<{typeParams}> right)
		{{
			return left.Equals(right);
		}}

		public static bool operator !=(Or<{typeParams}> left, Or<{typeParams}> right)
		{{
			return !(left == right);
		}}";

			// Generate cross-type equality operators
			var crossEqualityOperators = string.Join("\n\n", Enumerable.Range(1, typeParamCount)
				.Select(i => $@"		public static bool operator ==(Or<{typeParams}> left, T{i} right)
		{{
			if (left.VariantIndex == {i - 1})
				return Equals(left._value{i}, right);
			return false;
		}}

		public static bool operator !=(Or<{typeParams}> left, T{i} right)
		{{
			return !(left == right);
		}}

		public static bool operator ==(T{i} left, Or<{typeParams}> right)
		{{
			return right == left;
		}}

		public static bool operator !=(T{i} left, Or<{typeParams}> right)
		{{
			return right != left;
		}}"));

			return
$@"// <auto-generated/>
// This code is auto-generated by {typeof(OrGenerator).FullName}.

using System;

namespace RCLargeLanguageModels
{{
	/// <summary>
	/// Represents a type that can hold either a value of type {typeParamRefs},
	/// but not more than one at the same time. This is a discriminated union for {typeParamCount} types.
	/// </summary>
	/// {string.Join("\n\t/// ", Enumerable.Range(1, typeParamCount).Select(i => $"<typeparam name=\"T{i}\">The {GetOrdinal(i)} possible type.</typeparam>"))}
	[System.CodeDom.Compiler.GeneratedCode(""{typeof(OrGenerator).FullName}"", ""{Version}"")]
	[System.Diagnostics.DebuggerNonUserCode]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public readonly struct Or<{typeParams}> : IEquatable<Or<{typeParams}>>
	{{
		private readonly byte _variantIndex;
		{fields}

		/// <summary>
		/// Gets the index of the currently active variant ({string.Join(", ", Enumerable.Range(0, typeParamCount).Select(i => $"{i} for <typeparamref name=\"T{i + 1}\"/>"))}).
		/// </summary>
		public int VariantIndex => _variantIndex;

{valueProperties}

{staticConstructor}

{constructors}

{createMethods}

{asMethods}

{tryGetMethods}

{isMethod}

{matchMethod}

{switchMethod}

{deconstructMethod}

{equalityMethods}

{conversionOperators}

{equalityOperators}

{crossEqualityOperators}
	}}
}}
".Replace("    ", "\t");
		}

		private string GenerateTypeCheck(int typeParamCount, string typeParams)
		{
			var checks = new List<string>();
			for (int i = 1; i <= typeParamCount; i++)
			{
				for (int j = i + 1; j <= typeParamCount; j++)
				{
					checks.Add($"(typeof(T{i}) == typeof(T{j}))");
				}
			}

			return
$@"if ({string.Join(" || ", checks)})
				throw new InvalidOperationException($""Types in Or<{string.Join(", ", Enumerable.Range(1, typeParamCount).Select(i => $"{{typeof(T{i}).Name}}"))}> cannot repeat!"");";
		}

		private string GenerateDefaultAssignments(int typeParamCount, int excludeIndex)
		{
			return string.Join("\n\t\t\t", Enumerable.Range(1, typeParamCount)
				.Where(i => i != excludeIndex)
				.Select(i => $"_value{i} = default!;"));
		}

		private string GetOrdinal(int number)
		{
			switch (number)
			{
				case 1: return "first";
				case 2: return "second";
				case 3: return "third";
				case 4: return "fourth";
				case 5: return "fifth";
				case 6: return "sixth";
				case 7: return "seventh";
				case 8: return "eighth";
				case 9: return "ninth";
				default: return number.ToString() + "th";
			}
		}
	}
}