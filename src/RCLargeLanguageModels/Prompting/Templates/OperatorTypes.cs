namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents the type of binary expression.
	/// </summary>
	public enum UnaryOperatorType
	{
		Negate,
		LogicalNot
	}

	/// <summary>
	/// Represents the type of binary expression.
	/// </summary>
	public enum BinaryOperatorType
	{
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulus,
		LessThan,
		LessThanOrEqual,
		GreaterThan,
		GreaterThanOrEqual,
		Equal,
		NotEqual,
		LogicalAnd,
		LogicalOr
	}
}