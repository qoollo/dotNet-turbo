using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Extension methods for ConcurrentDictionary
    /// </summary>
    public static class QoolloConcurrentDictionaryExtensions
    {
        /// <summary>
        /// Reflection based estimate count calculator
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        private class GenericContainer<TKey, TValue>
        {
            private static FieldInfo _mTablesField;
            private static FieldInfo _mCountField;

            private static readonly object _syncObj = new object();

            private static void InitField()
            {
                if (_mCountField != null)
                    return;

                lock (_syncObj)
                {
                    if (_mCountField != null)
                        return;

                    _mTablesField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("m_tables", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (_mTablesField == null)
                        throw new InvalidOperationException("ConcurrentDictionary<,> does not contain 'm_tables' field");

                    _mCountField = _mTablesField.FieldType.GetField("m_countPerLock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | BindingFlags.Public);
                    if (_mCountField == null)
                        throw new InvalidOperationException("ConcurrentDictionary<,>.Tables does not contain 'm_countPerLock' field");
                }
            }

            public static int GetEstimateCount(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                Contract.Requires(dictionary != null);

                if (_mCountField == null)
                    InitField();

                var mTables = _mTablesField.GetValue(dictionary);
                int[] counts = (int[])_mCountField.GetValue(mTables);

                int result = 0;
                for (int i = 0; i < counts.Length; i++)
                    result += Volatile.Read(ref counts[i]);

                return result;
            }
        }

        /// <summary>
        /// Compiled estimate count calculator
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        private class GenericContainerCompiled<TKey, TValue>
        {
            private delegate int GetEstimateCountDelegate(ConcurrentDictionary<TKey, TValue> dictionary);

            private static GetEstimateCountDelegate _getEstimateCount;

            private static readonly object _syncObj = new object();

            private static GetEstimateCountDelegate CreateGetEstimateCountDelegate()
            {
                var mTablesField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("m_tables", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (mTablesField == null)
                    throw new InvalidOperationException("ConcurrentDictionary<,> does not contain 'm_tables' field");

                var mCountField = mTablesField.FieldType.GetField("m_countPerLock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | BindingFlags.Public);
                if (mCountField == null)
                    throw new InvalidOperationException("ConcurrentDictionary<,>.Tables does not contain 'm_countPerLock' field");

                var volatileReadMethod = typeof(Volatile).GetMethod("Read", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(int).MakeByRefType() }, null);
                if (volatileReadMethod == null)
                    throw new InvalidOperationException("Volatile.Read is not available");

                var method = new DynamicMethod("ConcurrentDictionary_GetEstimateCount_" + Guid.NewGuid().ToString("N"), typeof(int), new Type[] { typeof(ConcurrentDictionary<TKey, TValue>) }, true);

                var ilGen = method.GetILGenerator();
                var localArray = ilGen.DeclareLocal(typeof(int[]));
                var localResult = ilGen.DeclareLocal(typeof(int));
                var localIndex = ilGen.DeclareLocal(typeof(int));

                ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Stloc_0);
                ilGen.Emit(OpCodes.Ldc_I4_0);
                ilGen.Emit(OpCodes.Stloc_1);
                ilGen.Emit(OpCodes.Ldc_I4_0);
                ilGen.Emit(OpCodes.Stloc_2);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Volatile);
                ilGen.Emit(OpCodes.Ldfld, mTablesField);
                ilGen.Emit(OpCodes.Volatile);
                ilGen.Emit(OpCodes.Ldfld, mCountField);
                ilGen.Emit(OpCodes.Stloc_0);

               
                var loopLabelStart = ilGen.DefineLabel();
                var loopLabelCondition = ilGen.DefineLabel();

                ilGen.Emit(OpCodes.Br_S, loopLabelCondition);
                ilGen.MarkLabel(loopLabelStart);

                ilGen.Emit(OpCodes.Ldloc_1);
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Ldloc_2);
                ilGen.Emit(OpCodes.Ldelema, typeof(int));
                ilGen.EmitCall(OpCodes.Call, volatileReadMethod, null);
                ilGen.Emit(OpCodes.Add);
                ilGen.Emit(OpCodes.Stloc_1);

                ilGen.Emit(OpCodes.Ldloc_2);
                ilGen.Emit(OpCodes.Ldc_I4_1);
                ilGen.Emit(OpCodes.Add);
                ilGen.Emit(OpCodes.Stloc_2);

                ilGen.MarkLabel(loopLabelCondition);
       
                ilGen.Emit(OpCodes.Ldloc_2);
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Ldlen);
                ilGen.Emit(OpCodes.Conv_I4);
                ilGen.Emit(OpCodes.Blt_S, loopLabelStart);
            
                ilGen.Emit(OpCodes.Ldloc_1);
                ilGen.Emit(OpCodes.Ret);

                return (GetEstimateCountDelegate)method.CreateDelegate(typeof(GetEstimateCountDelegate));
            }

            private static void InitGetEstimateCount()
            {
                if (_getEstimateCount == null)
                {
                    lock (_syncObj)
                    {
                        if (_getEstimateCount == null)
                            _getEstimateCount = CreateGetEstimateCountDelegate();
                    }
                }
            }

            public static int GetEstimateCount(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                Contract.Requires(dictionary != null);

                var func = _getEstimateCount;
                if (func == null)
                {
                    InitGetEstimateCount();
                    func = _getEstimateCount;
                }

                return func(dictionary);
            }
        }


        /// <summary>
        /// Returns estimate number of elements contained in the  ConcurrentDictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
        /// <param name="dictionary">ConcurrentDictionary</param>
        /// <returns>Estimate number of elements contained in the  ConcurrentDictionary</returns>
        public static int GetEstimateCount<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            return GenericContainerCompiled<TKey, TValue>.GetEstimateCount(dictionary);
        }
    }
}
