using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues
{
    /// <summary>
    /// Converter methods for TransformationQueue
    /// </summary>
    /// <typeparam name="TExternal">Source type</typeparam>
    /// <typeparam name="TInner">Target type</typeparam>
    public interface ITransformationQueueConverter<TExternal, TInner>
    {
        /// <summary>
        /// Converts item from TExternal to TInner before store inside wrapped queue
        /// </summary>
        /// <param name="item">Item to be converted</param>
        /// <returns>Converted item</returns>
        TInner Convert(TExternal item);
        /// <summary>
        /// Converts item from TInner to TExternal after dequeuing item from inner queue
        /// </summary>
        /// <param name="item">Item to be converted</param>
        /// <returns>Converted item</returns>
        TExternal ConvertBack(TInner item);
    }



    /// <summary>
    /// Queue wrapper that performs item transformation from TExternal to TInner
    /// </summary>
    /// <typeparam name="TExternal">Items type that is visible by external code</typeparam>
    /// <typeparam name="TInner">Items type of the wrapped queue</typeparam>
    public class TransformationQueue<TExternal, TInner> : TransformationQueueBase<TExternal, TInner>
    {
        private readonly ITransformationQueueConverter<TExternal, TInner> _converter;

        /// <summary>
        /// TransformationQueueBase constructor
        /// </summary>
        /// <param name="queue">Inner queue to be wrapped</param>
        /// <param name="converter">Converter between TExternal and TInner</param>
        public TransformationQueue(IQueue<TInner> queue, ITransformationQueueConverter<TExternal, TInner> converter)
            : base(queue)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            _converter = converter;
        }

        /// <summary>
        /// Converts item from TExternal to TInner before store inside wrapped queue
        /// </summary>
        /// <param name="item">Item to be converted</param>
        /// <returns>Converted item</returns>
        protected sealed override TInner Convert(TExternal item)
        {
            return _converter.Convert(item);
        }

        /// <summary>
        /// Converts item from TInner to TExternal after dequeuing item from inner queue
        /// </summary>
        /// <param name="item">Item to be converted</param>
        /// <returns>Converted item</returns>
        protected sealed override TExternal ConvertBack(TInner item)
        {
            return _converter.ConvertBack(item);
        }
    }
}
