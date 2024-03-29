﻿using NonJobEvent.Domain;
using NonJobEvent.Domain.DomainEvents;

namespace NonJobEvent.Application.Api;

public interface ICalendarRepository
{
    Task<Persistence.Result<Calendar>> GetCalendarAsync(Guid calendarId, DateOnly dateFrom, DateOnly dateTo);

    Task<Persistence.Result<int>> SaveUpdatesAsync(IReadOnlyList<DomainEvent> updates);
}


public abstract class Persistence
{
    public record Result(Result.VersionData Versions)
    {
        public static readonly Result Empty = new(new VersionData());

        public static Result<TItem> From<TItem>(TItem item, VersionData versions) => new(item, versions);

        public static Result From(VersionData versions) => new(versions);

        public readonly record struct Version(uint Value);

        public sealed class VersionData
        {
            private readonly Dictionary<Guid, Version> versions;

            public VersionData() => this.versions = new();

            public void Add(Guid id, Version version) => this.versions.Add(id, version);

            public Version Get(Guid id) => this.versions[id];
        }
    }

    public sealed record Result<TItem>(TItem Item, Result.VersionData Versions): Result(Versions);

    public static Exception.AlreadyExists EventAlreadyExists(
        Guid calendarId,
        Guid eventId,
        System.Exception? innerExeption = null)
            => AlreadyExists($"Event with Id={eventId} already exists in calendar Id={calendarId}.", innerExeption);

    public static Exception.NotFound NotFound(
        string message,
        System.Exception? innerExeption = null) => new(message, innerExeption);

    public static Exception.AlreadyExists AlreadyExists(
        string message,
        System.Exception? innerExeption = null) => new(message, innerExeption);

    public static Exception.ConcurrencyConflict ConcurrencyConflict(
        string message,
        System.Exception? innerExeption = null) => new(message, innerExeption);

    public abstract class Exception : System.Exception
    {
        public sealed class NotFound : Persistence.Exception
        {
            public NotFound(
                string message, 
                System.Exception? innerException = null) : base(message, innerException) { }
        }

        public sealed class AlreadyExists : Persistence.Exception
        {
            public AlreadyExists(
                string message,
                System.Exception? innerException = null) : base(message, innerException) { }
        }

        public sealed class ConcurrencyConflict : Persistence.Exception
        {
            public ConcurrencyConflict(
                string message,
                System.Exception? innerException = null) : base(message, innerException) { }
        }

        private Exception(
            string message,
            System.Exception? innerException) : base(message, innerException) { }
    }
}