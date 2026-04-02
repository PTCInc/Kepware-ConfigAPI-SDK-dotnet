using Kepware.Api.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents the type of operation applied during compare-and-apply.
    /// </summary>
    public enum ApplyOperation
    {
        /// <summary>
        /// Insert operation.
        /// </summary>
        Insert = 0,

        /// <summary>
        /// Update operation.
        /// </summary>
        Update = 1,

        /// <summary>
        /// Delete operation.
        /// </summary>
        Delete = 2,
    }

    /// <summary>
    /// Represents one failed apply operation and the attempted item.
    /// </summary>
    public sealed class ApplyFailure
    {
        /// <summary>
        /// Gets the operation that failed.
        /// </summary>
        public required ApplyOperation Operation { get; init; }

        /// <summary>
        /// Gets the original item used for the failed operation.
        /// </summary>
        public required BaseEntity AttemptedItem { get; init; }

        /// <summary>
        /// Gets the response code associated with the failed operation.
        /// </summary>
        public int? ResponseCode { get; init; }

        /// <summary>
        /// Gets the response message associated with the failed operation.
        /// </summary>
        public string? ResponseMessage { get; init; }

        /// <summary>
        /// Gets the property name associated with an insert validation failure.
        /// </summary>
        public string? Property { get; init; }

        /// <summary>
        /// Gets the detailed description associated with an insert validation failure.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the error line associated with an insert validation failure.
        /// </summary>
        public int? ErrorLine { get; init; }

        /// <summary>
        /// Gets the list of update properties that were not applied.
        /// </summary>
        public IReadOnlyList<string>? NotAppliedProperties { get; init; }
    }

    /// <summary>
    /// Represents detailed compare-and-apply results for a collection.
    /// </summary>
    /// <typeparam name="K">The entity type.</typeparam>
    public sealed class CollectionApplyResult<K>
        where K : NamedEntity, new()
    {
        private readonly List<ApplyFailure> m_failures = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionApplyResult{K}"/> class.
        /// </summary>
        /// <param name="compareResult">The raw compare result bucket.</param>
        public CollectionApplyResult(EntityCompare.CollectionResultBucket<K> compareResult)
        {
            CompareResult = compareResult;
        }

        /// <summary>
        /// Gets the underlying compare result bucket.
        /// </summary>
        public EntityCompare.CollectionResultBucket<K> CompareResult { get; }

        /// <summary>
        /// Gets the number of successfully inserted items.
        /// </summary>
        public int Inserts { get; private set; }

        /// <summary>
        /// Gets the number of successfully updated items.
        /// </summary>
        public int Updates { get; private set; }

        /// <summary>
        /// Gets the number of successfully deleted items.
        /// </summary>
        public int Deletes { get; private set; }

        /// <summary>
        /// Gets the number of failed operations.
        /// </summary>
        public int Failures => m_failures.Count;

        /// <summary>
        /// Gets the failed operation details.
        /// </summary>
        public ReadOnlyCollection<ApplyFailure> FailureList => m_failures.AsReadOnly();

        internal void AddInsertSuccess() => Inserts += 1;

        internal void AddUpdateSuccess() => Updates += 1;

        internal void AddDeleteSuccess() => Deletes += 1;

        internal void AddFailure(ApplyFailure failure) => m_failures.Add(failure);
    }

    /// <summary>
    /// Represents detailed compare-and-apply results for a full project operation.
    /// </summary>
    public sealed class ProjectCompareAndApplyResult
    {
        private readonly List<ApplyFailure> m_failures = [];

        /// <summary>
        /// Gets the number of successfully inserted items.
        /// </summary>
        public int Inserts { get; private set; }

        /// <summary>
        /// Gets the number of successfully updated items.
        /// </summary>
        public int Updates { get; private set; }

        /// <summary>
        /// Gets the number of successfully deleted items.
        /// </summary>
        public int Deletes { get; private set; }

        /// <summary>
        /// Gets the number of failed operations.
        /// </summary>
        public int Failures => m_failures.Count;

        /// <summary>
        /// Gets the failed operation details.
        /// </summary>
        public ReadOnlyCollection<ApplyFailure> FailureList => m_failures.AsReadOnly();

        internal void Add(CollectionApplyResult<Channel> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(CollectionApplyResult<Device> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(CollectionApplyResult<Tag> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(CollectionApplyResult<DeviceTagGroup> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(CollectionApplyResult<MqttClientAgent> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(CollectionApplyResult<RestClientAgent> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(CollectionApplyResult<RestServerAgent> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(CollectionApplyResult<IotItem> result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void Add(ProjectCompareAndApplyResult result)
            => Add(result.Inserts, result.Updates, result.Deletes, result.FailureList);

        internal void AddInsertSuccess() => Inserts += 1;

        internal void AddUpdateSuccess() => Updates += 1;

        internal void AddDeleteSuccess() => Deletes += 1;

        internal void AddFailure(ApplyFailure failure) => m_failures.Add(failure);

        private void Add(int inserts, int updates, int deletes, IReadOnlyList<ApplyFailure> failures)
        {
            Inserts += inserts;
            Updates += updates;
            Deletes += deletes;
            m_failures.AddRange(failures);
        }
    }
}
