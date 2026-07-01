using FluentValidation;
using MediatR;

namespace LTSBackend.Comman.Middleware;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(x => x is not null)
            .ToList();

        if (failures.Count > 0)
        {
            // `ValidationException` is ambiguous here — both `FluentValidation.ValidationException`
            // (via `using FluentValidation;` above) and `LTSBackend.Comman.Exceptions.ValidationException`
            // are in scope. Fully-qualifying removes the ambiguity regardless of using-directive order.
            throw new LTSBackend.Comman.Exceptions.ValidationException(
                failures.Select(x => x.ErrorMessage).ToList());
        }

        return await next();
    }
}