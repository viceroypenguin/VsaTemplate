using System.Linq.Expressions;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace VsaTemplate.Web.Database;

public sealed class NullableConverterAttribute : ValueConverterAttribute
{
	/// <summary>
	/// Returns <see cref="IValueConverter"/> for specific column.
	/// </summary>
	public override IValueConverter? GetValueConverter(ColumnDescriptor columnDescriptor)
	{
		if (ValueConverter is not null)
			return ValueConverter;

		if (ConverterType is null)
			return null;

		return _converter ??= BuildNullableConverter(ConverterType);
	}

	private sealed class NullableConverter : IValueConverter
	{
		public bool HandlesNulls => false;
		public required LambdaExpression FromProviderExpression { get; init; }
		public required LambdaExpression ToProviderExpression { get; init; }
	}

	private IValueConverter? _converter;
	private static NullableConverter BuildNullableConverter(Type type)
	{
		var dynamicConverter = (IValueConverter)TypeAccessor.GetAccessor(type).CreateInstance();
		var from = HandleNull(dynamicConverter.FromProviderExpression);
		var to = HandleNull(dynamicConverter.ToProviderExpression);

		return new NullableConverter
		{
			FromProviderExpression = from,
			ToProviderExpression = to,
		};
	}

	private static LambdaExpression HandleNull(
		LambdaExpression lambda
	)
	{
		var aType = typeof(Nullable<>).MakeGenericType(lambda.Parameters[0].Type);
		var bType = typeof(Nullable<>).MakeGenericType(lambda.ReturnType);

		var aParam = Expression.Parameter(aType, "a");
		var prop = aType.GetProperty("Value");

		var body = lambda.Body.Replace(
			lambda.Parameters[0],
			Expression.Property(aParam, prop!)
		);

		return Expression.Lambda(
			Expression.Convert(
				body,
				bType
			),
			aParam
		);
	}
}
