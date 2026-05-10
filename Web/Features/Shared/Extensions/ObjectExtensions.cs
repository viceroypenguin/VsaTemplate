namespace VsaTemplate.Web.Features.Shared.Extensions;

public static class ObjectExtensions
{
	/// <summary>
	///     Applies an action to an object and returns the object itself.
	/// </summary>
	public static T Apply<T>(this T input, Action<T> action)
	{
		action(input);
		return input;
	}

	/// <summary>
	///     Applies a function to an object if a condition is true and returns the result, otherwise returns the object itself.
	/// </summary>
	public static T ApplyIf<T>(this T input, bool? condition, Func<T, T> func)
		=> condition == true ? func(input) : input;

	/// <summary>
	///     Applies a function to an object if a condition is not null and returns the result, otherwise returns the object itself.
	/// </summary>
	public static T ApplyIfNotNull<T, TCondition>(this T input, TCondition? condition, Func<T, T> apply)
		=> condition is { } ? apply(input) : input;

	/// <summary>
	///     Applies a function to an object if a condition is not null and returns the result, otherwise return the object itself.
	/// </summary>
	public static T ApplyIfNotNull<T, TCondition>(this T input, TCondition? condition, Func<TCondition, T, T> apply)
		=> condition is { } ? apply(condition, input) : input;

	/// <summary>
	///     Applies a function to an object if a condition is not null and returns the result, otherwise return the object itself.
	/// </summary>
	public static T ApplyIfNotNull<T, TCondition>(this T input, TCondition? condition, Func<TCondition, T, T> apply)
		where TCondition : struct
		=> condition is { } ? apply(condition.Value, input) : input;

	/// <summary>
	///     Wraps an object in a Task.
	/// </summary>
	public static Task<T> ToTask<T>(this T input)
		=> Task.FromResult(input);

	/// <summary>
	///     Wraps an object in a ValueTask.
	/// </summary>
	public static ValueTask<T> ToValueTask<T>(this T input)
		=> ValueTask.FromResult(input);

	/// <summary>
	///     Transforms an object using a function.
	/// </summary>
	public static TOut Transform<TIn, TOut>(this TIn input, Func<TIn, TOut> func)
		=> func(input);

	/// <summary>
	///     Transforms an object using a function.
	/// </summary>
	public static async Task<TOut> Transform<TIn, TOut>(this Task<TIn> input, Func<TIn, TOut> func)
		=> func(await input);

	/// <summary>
	///     Transforms an object using a function.
	/// </summary>
	public static async ValueTask<TOut> Transform<TIn, TOut>(this ValueTask<TIn> input, Func<TIn, TOut> func)
		=> func(await input);

	/// <summary>
	///     Transforms an object using a function.
	/// </summary>
	public static TIn TransformIf<TIn>(this TIn input, bool condition, Func<TIn, TIn> func)
		=> condition ? func(input) : input;

	/// <summary>
	///     Transforms an object using a function if a condition is not null and returns the result, otherwise applies another function.
	/// </summary>
	public static TOut TransformIfNotNull<TIn, TOut, TCondition>(
		this TIn input,
		TCondition? condition,
		Func<TIn, TOut> applyTrue,
		Func<TIn, TOut> applyFalse
	)
		=> condition is { } ? applyTrue(input) : applyFalse(input);

	/// <summary>
	///     Transforms an object using a function if a condition is not null and returns the result, otherwise applies another function.
	/// </summary>
	public static TOut TransformIfNotNull<TIn, TOut, TCondition>(
		this TIn input,
		TCondition? condition,
		Func<TCondition, TIn, TOut> applyTrue,
		Func<TIn, TOut> applyFalse
	)
		=> condition is { } ? applyTrue(condition, input) : applyFalse(input);

	/// <summary>
	///     Makes a collection out of a value.
	/// </summary>
	public static IEnumerable<T> Yield<T>(this T input)
		=> [input];
}
