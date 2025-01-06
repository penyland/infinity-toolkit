namespace System.Collections.Generic;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Check if all items are equal.
    /// </summary>
    /// <typeparam name="T">Type of elements.</typeparam>
    /// <param name="values">Collection to iterate.</param>
    public static bool AllEqual<T>(this IEnumerable<T> values)
        where T : class
    {
        if (!values.Any())
        {
            return true;
        }

        var first = values.First();
        return values.Skip(1).All(v => first.Equals(v));
    }

    /// <summary>
    /// Check if all items are equal.
    /// </summary>
    /// <typeparam name="T">Type of elements.</typeparam>
    /// <param name="values">Collection to iterate.</param>
    /// <param name="value">The value to check if all are equal to.</param>
    public static bool AllEqual<T>(this IEnumerable<T> values, T value)
        where T : class
    {
        if (!values.Any())
        {
            return true;
        }

        return values.All(v => value.Equals(v));
    }

    /// <summary>
    /// Apply an action to each element.
    /// </summary>
    /// <typeparam name="T">Type of elements.</typeparam>
    /// <param name="enumerable">Collection to iterate.</param>
    /// <param name="action">Action to apply on each element.</param>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }

    /// <summary>
    /// Converts an expected IEnumerable set to null to an empty IEnumerable.
    /// </summary>
    /// <typeparam name="T">Generic type expected to be produced by the IEnumerable.</typeparam>
    /// <param name="input">IEnumerable producing the generic type T, or null.</param>
    /// <returns>The original IEnumerable or an empty IEnumerable if the original IEnumerable is set to null.</returns>
    public static IEnumerable<T> NullToEmpty<T>(this IEnumerable<T>? input) =>
        input ?? [];

    /// <summary>
    /// Converts a Task{IEnumerable{T}} to an IAsyncEnumerable{T}.
    /// </summary>
    /// <typeparam name="T">Generic type expected to be produced by the IAsyncEnumerable.</typeparam>
    /// <param name="input">A Task returning an IEnumerable{T}.</param>
    /// <returns>An IAsyncEnumerable{T}.</returns>
    public static async IAsyncEnumerable<T> ToIAsyncEnumerable<T>(this Task<IEnumerable<T>> input)
    {
        var result = await input;

        foreach (var item in result)
        {
            yield return item;
        }
    }
}
