using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Represents the method that specifies a set of criteria to complete waiting
    /// </summary>
    /// <returns>True if the current state meets the criteria</returns>
    public delegate bool WaitPredicate();
    /// <summary>
    /// Represents the method that specifies a set of criteria to complete waiting
    /// </summary>
    /// <param name="state">State object</param>
    /// <typeparam name="TState">The type of the state object</typeparam>
    /// <returns>True if the current state meets the criteria</returns>
    public delegate bool WaitPredicate<TState>(TState state);
    /// <summary>
    /// Represents the method that specifies a set of criteria to complete waiting
    /// </summary>
    /// <param name="state">State object</param>
    /// <typeparam name="TState">The type of the state object</typeparam>
    /// <returns>True if the current state meets the criteria</returns>
    public delegate bool WaitPredicateRef<TState>(ref TState state);
}
