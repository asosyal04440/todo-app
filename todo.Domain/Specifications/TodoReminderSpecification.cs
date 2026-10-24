using System.Linq.Expressions;
using TodoApi.Domain.Entities;

namespace TodoApi.Domain.Specifications;

public sealed class TodoReminderSpecification
{
    private static readonly Expression<Func<TodoItem, DateTime, DateTime, bool>> DueRule =
        (todo, nowUtc, dueBeforeUtc) =>
            !todo.IsDeleted
            && !todo.IsCompleted
            && todo.ReminderSentAtUtc == null
            && todo.DueAtUtc != null
            && todo.DueAtUtc > nowUtc
            && todo.DueAtUtc <= dueBeforeUtc;

    private static readonly Expression<Func<TodoItem, int, DateTime, bool>> RetryRule =
        (todo, maximumAttempts, lastFailedAttemptBeforeUtc) =>
            !todo.IsDeleted
            && todo.ReminderAttemptCount < maximumAttempts
            && (todo.ReminderLastAttemptAtUtc == null
                || todo.ReminderLastAttemptAtUtc <= lastFailedAttemptBeforeUtc);

    private static readonly Func<TodoItem, DateTime, DateTime, bool> IsDue = DueRule.Compile();
    private static readonly Func<TodoItem, int, DateTime, bool> CanRetry = RetryRule.Compile();

    private readonly DateTime _nowUtc;
    private readonly DateTime _dueBeforeUtc;
    private readonly DateTime _lastFailedAttemptBeforeUtc;
    private readonly int _maximumAttempts;
    private readonly Expression<Func<TodoItem, bool>> _expression;

    public TodoReminderSpecification(
        DateTime nowUtc,
        TimeSpan leadTime,
        int maximumAttempts,
        TimeSpan retryDelay)
    {
        _nowUtc = nowUtc;
        _dueBeforeUtc = nowUtc.Add(leadTime);
        _lastFailedAttemptBeforeUtc = nowUtc.Subtract(retryDelay);
        _maximumAttempts = maximumAttempts;

        ParameterExpression todo = Expression.Parameter(typeof(TodoItem), "todo");
        DateTime dueBeforeUtc = _dueBeforeUtc;
        DateTime lastFailedAttemptBeforeUtc = _lastFailedAttemptBeforeUtc;
        Expression<Func<DateTime>> now = () => nowUtc;
        Expression<Func<DateTime>> dueBefore = () => dueBeforeUtc;
        Expression<Func<int>> attempts = () => maximumAttempts;
        Expression<Func<DateTime>> retryBefore = () => lastFailedAttemptBeforeUtc;

        Expression due = Bind(DueRule, todo, now.Body, dueBefore.Body);
        Expression retry = Bind(RetryRule, todo, attempts.Body, retryBefore.Body);
        _expression = Expression.Lambda<Func<TodoItem, bool>>(Expression.AndAlso(due, retry), todo);
    }

    public Expression<Func<TodoItem, bool>> ToExpression() => _expression;

    public bool IsSatisfiedBy(TodoItem todo)
    {
        ArgumentNullException.ThrowIfNull(todo);
        return IsDue(todo, _nowUtc, _dueBeforeUtc)
            && CanRetry(todo, _maximumAttempts, _lastFailedAttemptBeforeUtc);
    }

    internal static bool NeedsReminder(TodoItem todo, DateTime nowUtc, TimeSpan leadTime) =>
        IsDue(todo, nowUtc, nowUtc.Add(leadTime));

    internal static bool CanRetryReminder(TodoItem todo, DateTime nowUtc, int maximumAttempts, TimeSpan retryDelay) =>
        CanRetry(todo, maximumAttempts, nowUtc.Subtract(retryDelay));

    private static Expression Bind(LambdaExpression rule, params Expression[] values)
    {
        var replacements = new Dictionary<ParameterExpression, Expression>();
        for (int i = 0; i < rule.Parameters.Count; i++)
        {
            replacements.Add(rule.Parameters[i], values[i]);
        }

        return new ParameterBinder(replacements).Visit(rule.Body)!;
    }

    private sealed class ParameterBinder(IReadOnlyDictionary<ParameterExpression, Expression> replacements)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            replacements.TryGetValue(node, out Expression? replacement) ? replacement : node;
    }
}
