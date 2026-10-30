using System;

namespace Raidline.Core.Common
{
    /// <summary>
    /// Outcome of an operation that can fail for an expected reason
    /// (no room in the grid, no ammo, not enough money). Programming errors throw instead.
    /// </summary>
    /// <typeparam name="TError">Enum of failure reasons.</typeparam>
    public readonly struct Result<TError> where TError : struct, Enum
    {
        /// <summary>True when the operation succeeded.</summary>
        public readonly bool IsSuccess;

        /// <summary>Failure reason; default value when <see cref="IsSuccess"/>.</summary>
        public readonly TError Error;

        private Result(bool isSuccess, TError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>True when the operation failed.</summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>A successful result.</summary>
        public static Result<TError> Ok() => new Result<TError>(true, default);

        /// <summary>A failed result with a reason.</summary>
        public static Result<TError> Fail(TError error) => new Result<TError>(false, error);

        public override string ToString() => IsSuccess ? "Ok" : "Fail(" + Error + ")";
    }
}
