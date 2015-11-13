using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Generates and stores an unique identifier for the type
    /// </summary>
    public class TypeUIDResolver
    {
        /// <summary>
        /// Current Id
        /// </summary>
        private static int _currentId;
        /// <summary>
        /// Map from type to its Id
        /// </summary>
        private static readonly ConcurrentDictionary<Type, int> _typeIdMapper = new ConcurrentDictionary<Type, int>();

        /// <summary>
        /// Determines whether the ID was already generated for the type
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>True if the id was already generated</returns>
        public static bool HasTypeId(Type type)
        {
            return _typeIdMapper.ContainsKey(type);
        }
        /// <summary>
        /// Gets the unique ID for the specified type if it was generated. Overwise throws an exception.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Unique ID for the 'type'</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public static int GetTypeId(Type type)
        {
            return _typeIdMapper[type];
        }
        /// <summary>
        /// Gets the unique ID for the specified type if it was generated. Overwise returns zero.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Unique ID for the 'type' or zero</returns>
        public static int GetTypeIdOrDefault(Type type)
        {
            int result = 0;
            _typeIdMapper.TryGetValue(type, out result);
            return result;
        }
        /// <summary>
        /// Gets or generates the unique ID for the specified type (slow)
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Unique ID for the 'type'</returns>
        public static int GetOrGenerateTypeId(Type type)
        {
            int result = 0;
            if (_typeIdMapper.TryGetValue(type, out result))
                return result;

            var genericType = typeof(TypeUIDResolver<>).MakeGenericType(type);
            var method = genericType.GetMethod("GetMyId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return (int)method.Invoke(null, null);
        }

        /// <summary>
        /// Generates UID
        /// </summary>
        /// <returns>New UID</returns>
        private static int GetNewId()
        {
            return System.Threading.Interlocked.Increment(ref _currentId);
        }

        /// <summary>
        /// Generates new ID for the specified type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="idStorage">Generated UID</param>
        protected static void GenerateNewId<T>(ref int idStorage)
        {
            if (idStorage == 0)
            {
                if (System.Threading.Interlocked.CompareExchange(ref idStorage, GetNewId(), 0) == 0)
                    _typeIdMapper.TryAdd(typeof(T), idStorage);
            }
        }
    }

    /// <summary>
    /// Stores unique identifier for the type 'T' 
    /// </summary>
    /// <typeparam name="T">The type whose identifier is stored</typeparam>
    public sealed class TypeUIDResolver<T> : TypeUIDResolver
    {
        /// <summary>
        /// UID for the type 'T'
        /// </summary>
        private static int _myId;

        /// <summary>
        /// Static constructor
        /// </summary>
        static TypeUIDResolver()
        {
            _myId = 0;
            GenerateNewId<int>(ref _myId);
        }

        /// <summary>
        /// Gets or generates the identifier for the type 'T' (extremely fast) 
        /// </summary>
        /// <returns>UID for the type</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int GetMyId()
        {
            return _myId;
        }
    }
}
