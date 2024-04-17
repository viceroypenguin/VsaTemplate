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
		if (lambda is not
			{
				Body: { } body,
				Parameters: [{ } parameter],
				ReturnType: { } returnType,
			}
		)
		{
			throw new NotSupportedException("Unknown lambda.");
		}

		if (parameter.Type.IsValueType)
		{
			var aType = typeof(Nullable<>).MakeGenericType(parameter.Type);
			var prop = aType.GetProperty("Value");
			var aParam = Expression.Parameter(aType, "a");

			body = body.Replace(
				parameter,
				Expression.Property(
					aParam,
					prop!
				)
			);

			parameter = aParam;
		}

		if (returnType.IsValueType)
		{
			var bType = typeof(Nullable<>).MakeGenericType(returnType);
			body = Expression.Convert(
				body,
				bType
			);
		}

		return Expression.Lambda(
			body,
			parameter
		);
	}
}
