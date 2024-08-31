﻿namespace DirectorySync.Infrastructure;

public class ActivityContext
{
    private static readonly AsyncLocal<ActivityContext> _value = new();

    /// <summary>
    /// Current context activity id.
    /// </summary>
    public string ActivityId { get; private set; }

    /// <summary>
    /// Returns current ActivityContext or creates new if null.
    /// </summary>
    public static ActivityContext Current
    {
        get => _value.Value ??= new ActivityContext();
        set => _value.Value = value;
    }

    private ActivityContext()
    {
        ActivityId = Guid.NewGuid().ToString();
        Current = this;
    }

    private ActivityContext(string activityId)
    {
        ActivityId = activityId;
        Current = this;
    }

    /// <summary>
    /// Creates and sets current ActivityContext then returns it.
    /// </summary>
    /// <param name="activityId">Specified activity id.</param>
    /// <returns>Current ActivityContext.</returns>
    public static ActivityContext Create(string activityId) => new(activityId);

    /// <summary>
    /// Sets activity id to the current ActivityContext.
    /// </summary>
    /// <param name="activityId">New activity id.</param>
    public void SetActivityId(string activityId) => ActivityId = activityId;
}
