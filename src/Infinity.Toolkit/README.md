# Infinity.Toolkit

Infinity.Toolkit is a .NET 9.0 library that provides a set of tools and utilities for various purposes. This document provides an overview of the public methods available in the library.

## Installation

To install Infinity.Toolkit, add the following package reference to your project file:

```<PackageReference Include="Infinity.Toolkit" Version="1.0.0" />```

## Usage

### Public Methods

#### ValueStopwatch

- **IsActive**: Indicates whether the stopwatch is active.
- **StartNew**: Starts a new instance of the `ValueStopwatch`.
- **GetElapsedTime**: Gets the total elapsed time measured by the current instance, in milliseconds.

#### RequiredIfAttribute
- **FormatErrorMessage**: Formats the error message to display if validation fails.
- **IsValid**: Validates the specified value with respect to the current validation attribute.

#### EnvironmentHelper

- **IsRunningInAzureAppService**: Checks if the application is running in Azure App Service.
- **IsRunningInContainer**: Checks if the application is running in a container.

#### AuthenticationBuilderExtensions

- **AddMultipleBearerPolicySchemes**: Adds a policy scheme that can dynamically select the authentication scheme based on the token issuer.

#### IEnumerableExtensions

- **AllEqual**: 
  - **Definition**: `public static bool AllEqual<T>(this IEnumerable<T> values) where T : class`
  - **Description**: Check if all items are equal.
  - **Parameters**: 
    - `values`: Collection to iterate.
  - **Returns**: `bool` indicating if all items are equal.

- **AllEqual**: 
  - **Definition**: `public static bool AllEqual<T>(this IEnumerable<T> values, T value) where T : class`
  - **Description**: Check if all items are equal to a specified value.
  - **Parameters**: 
    - `values`: Collection to iterate.
    - `value`: The value to check if all are equal to.
  - **Returns**: `bool` indicating if all items are equal to the specified value.

- **ForEach**: 
  - **Definition**: `public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)`
  - **Description**: Apply an action to each element.
  - **Parameters**: 
    - `enumerable`: Collection to iterate.
    - `action`: Action to apply on each element.

- **NullToEmpty**: 
  - **Definition**: `public static IEnumerable<T> NullToEmpty<T>(this IEnumerable<T>? input)`
  - **Description**: Converts an expected IEnumerable set to null to an empty IEnumerable.
  - **Parameters**: 
    - `input`: IEnumerable producing the generic type T, or null.
  - **Returns**: The original IEnumerable or an empty IEnumerable if the original IEnumerable is set to null.

- **ToIAsyncEnumerable**: 
  - **Definition**: `public static async IAsyncEnumerable<T> ToIAsyncEnumerable<T>(this Task<IEnumerable<T>> input)`
  - **Description**: Converts a Task{IEnumerable{T}} to an IAsyncEnumerable{T}.
  - **Parameters**: 
    - `input`: A Task returning an IEnumerable{T}.
  - **Returns**: An IAsyncEnumerable{T}.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

This project is licensed under the MIT License.
