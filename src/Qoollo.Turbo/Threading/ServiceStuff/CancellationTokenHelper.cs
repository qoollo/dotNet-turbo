using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Helper to execute private methods of CancellationToken
    /// </summary>
    internal static class CancellationTokenHelper
    {                   
        private delegate CancellationTokenRegistration RegisterWithoutECDelegate(ref CancellationToken token, Action<object> callback, object state);

        private static RegisterWithoutECDelegate _registerWithoutECDelegate;

        private static readonly object _syncObj = new object();

        /// <summary>
        /// Fallback method for RegisterWithoutEC
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="callback">Callback delegate</param>
        /// <param name="state">State for callback delegate</param>
        /// <returns>CancellationTokenRegistration that can be used to deregister the callback</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CancellationTokenRegistration FallbackRegisterWithoutEC(ref CancellationToken token, Action<object> callback, object state)
        {
            return token.Register(callback, state, false);
        }

        /// <summary>
        /// Attempts to generates dynamic method for RegisterWithoutEC
        /// </summary>
        /// <returns>Delegate for generated method if successful, otherwise null</returns>
        private static RegisterWithoutECDelegate TryGenerateRegisterWithoutEC()
        {
            var registerMethodCandidates = typeof(CancellationToken).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Where(o => o.Name == "Register" && o.GetParameters().Length == 4).ToList();
            if (registerMethodCandidates.Count != 1)
            {
                TurboContract.Assert(false, "CancellationTokenHelper.TryCreateRegisterWithoutECMethod should be successful for known runtimes");
                return null;
            }

            var method = new DynamicMethod("CancellationToken_RegisterWithoutEC_" + Guid.NewGuid().ToString("N"), typeof(CancellationTokenRegistration),
                new Type[] { typeof(CancellationToken).MakeByRefType(), typeof(Action<object>), typeof(object) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldarg_2);
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Call, registerMethodCandidates[0]);
            ilGen.Emit(OpCodes.Ret);

            return (RegisterWithoutECDelegate)method.CreateDelegate(typeof(RegisterWithoutECDelegate));
        }



        /// <summary>
        /// Initialize RegisterWithoutEC delegate
        /// </summary>
        /// <returns>Initialized delegate</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RegisterWithoutECDelegate InitRegisterWithoutEC()
        {
            lock (_syncObj)
            {
                var result = Volatile.Read(ref _registerWithoutECDelegate);
                if (result == null)
                {
                    result = TryGenerateRegisterWithoutEC() ?? new RegisterWithoutECDelegate(FallbackRegisterWithoutEC);
                    Volatile.Write(ref _registerWithoutECDelegate, result);
                }
                return result;
            }
        }


        /// <summary>
        /// Register delegate that will be called on token cancellation. 
        /// It attempts to register callback without capturing ExecutionContext. If it is not possible on current runtime it fallback to default Register method
        /// </summary>
        /// <param name="token">Source cancellation token</param>
        /// <param name="callback">Callback delegate</param>
        /// <param name="state">State for callback delegate</param>
        /// <returns>CancellationTokenRegistration that can be used to deregister the callback</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CancellationTokenRegistration RegisterWithoutECIfPossible(CancellationToken token, Action<object> callback, object state)
        {
            var action = _registerWithoutECDelegate ?? InitRegisterWithoutEC();
            return action(ref token, callback, state);
        }
    }
}
