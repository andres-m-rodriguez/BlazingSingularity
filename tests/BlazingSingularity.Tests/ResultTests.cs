using BlazingSingularity.Commands;
using Xunit;

namespace BlazingSingularity.Tests;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Exception);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result.Failure(exception);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(exception, result.Exception);
        Assert.Equal("Test error", result.ErrorMessage);
    }

    [Fact]
    public void Match_ActionOverload_CallsOnSuccessForSuccessfulResult()
    {
        var result = Result.Success();
        var successCalled = false;
        var failureCalled = false;

        result.Match(
            onSuccess: () => successCalled = true,
            onFailure: _ => failureCalled = true
        );

        Assert.True(successCalled);
        Assert.False(failureCalled);
    }

    [Fact]
    public void Match_ActionOverload_CallsOnFailureForFailedResult()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result.Failure(exception);
        var successCalled = false;
        Exception? capturedEx = null;

        result.Match(
            onSuccess: () => successCalled = true,
            onFailure: ex => capturedEx = ex
        );

        Assert.False(successCalled);
        Assert.Same(exception, capturedEx);
    }

    [Fact]
    public void Match_FuncOverload_ReturnsOnSuccessValueForSuccessfulResult()
    {
        var result = Result.Success();

        var value = result.Match(
            onSuccess: () => "success",
            onFailure: _ => "failure"
        );

        Assert.Equal("success", value);
    }

    [Fact]
    public void Match_FuncOverload_ReturnsOnFailureValueForFailedResult()
    {
        var result = Result.Failure(new InvalidOperationException("Test"));

        var value = result.Match(
            onSuccess: () => "success",
            onFailure: ex => ex.Message
        );

        Assert.Equal("Test", value);
    }
}

public class ResultGenericTests
{
    [Fact]
    public void Success_CreatesSuccessfulResultWithValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Exception);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result<int>.Failure(exception);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(default, result.Value);
        Assert.Same(exception, result.Exception);
        Assert.Equal("Test error", result.ErrorMessage);
    }

    [Fact]
    public void Match_ActionOverload_CallsOnSuccessWithValueForSuccessfulResult()
    {
        var result = Result<int>.Success(42);
        int? capturedValue = null;
        var failureCalled = false;

        result.Match(
            onSuccess: v => capturedValue = v,
            onFailure: _ => failureCalled = true
        );

        Assert.Equal(42, capturedValue);
        Assert.False(failureCalled);
    }

    [Fact]
    public void Match_ActionOverload_CallsOnFailureForFailedResult()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result<int>.Failure(exception);
        var successCalled = false;
        Exception? capturedEx = null;

        result.Match(
            onSuccess: _ => successCalled = true,
            onFailure: ex => capturedEx = ex
        );

        Assert.False(successCalled);
        Assert.Same(exception, capturedEx);
    }

    [Fact]
    public void Match_FuncOverload_TransformsSuccessValue()
    {
        var result = Result<int>.Success(42);

        var value = result.Match(
            onSuccess: v => v.ToString(),
            onFailure: _ => "error"
        );

        Assert.Equal("42", value);
    }

    [Fact]
    public void Match_FuncOverload_TransformsFailureValue()
    {
        var result = Result<int>.Failure(new InvalidOperationException("Test"));

        var value = result.Match(
            onSuccess: v => v.ToString(),
            onFailure: ex => ex.Message
        );

        Assert.Equal("Test", value);
    }
}
