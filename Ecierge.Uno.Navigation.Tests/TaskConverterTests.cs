namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;

/// <summary>
/// A constructor parameter of type <c>Task&lt;T&gt;</c> is given a task of the entity that may have another result type,
/// so the task is converted. The converted task has to complete whatever the original task does.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class TaskConverterTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    // The converted task is a Task of the destination type, so its result is read through the Result property
    private static async Task<object?> ResultOf(Task task, Type destinationType)
    {
        var converted = TypeExtensions.TaskConverter.Convert(task, destinationType);
        await converted.WaitAsync(Patience);
        return converted.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(converted);
    }

    [TestMethod]
    public async Task A_result_that_fits_must_complete_the_converted_task_with_it()
    {
        var result = await ResultOf(Task.FromResult("text"), typeof(object));

        Assert.AreEqual("text", result, "Expected the result of the original task.");
    }

    [TestMethod]
    public async Task A_result_that_does_not_fit_must_fail_the_converted_task_instead_of_leaving_it_pending()
    {
        await Assert.ThrowsExactlyAsync<InvalidCastException>(
            () => ResultOf(Task.FromResult("text"), typeof(int)),
            "Expected the converted task to fail, not to wait for ever for a result that cannot be converted.");
    }

    [TestMethod]
    public async Task A_failed_task_must_fail_the_converted_task_with_the_same_exception()
    {
        var failure = new HttpRequestException("The server is down");
        var thrown = await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => ResultOf(Task.FromException<string>(failure), typeof(object)),
            "Expected the exception of the original task.");

        Assert.AreSame(failure, thrown, "Expected the exception itself and not a copy of it.");
    }

    [TestMethod]
    public async Task A_canceled_task_must_cancel_the_converted_task()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => ResultOf(Task.FromCanceled<string>(new CancellationToken(canceled: true)), typeof(object)),
            "Expected the converted task to be canceled with the original one.");
    }

    [TestMethod]
    public async Task A_null_result_must_fit_a_reference_type_and_a_nullable_type_but_not_a_value_type()
    {
        Assert.IsNull(await ResultOf(Task.FromResult<string?>(null), typeof(string)), "Expected null to fit a reference type.");
        Assert.IsNull(await ResultOf(Task.FromResult<int?>(null), typeof(int?)), "Expected null to fit a nullable type.");

        await Assert.ThrowsExactlyAsync<InvalidCastException>(
            () => ResultOf(Task.FromResult<string?>(null), typeof(int)),
            "Expected null not to fit a value type.");
    }

    [TestMethod]
    public void A_task_without_a_result_must_be_refused()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => TypeExtensions.TaskConverter.Convert(new Task(() => { }), typeof(object)),
            "Expected a task that returns nothing to have nothing to convert.");
    }
}
