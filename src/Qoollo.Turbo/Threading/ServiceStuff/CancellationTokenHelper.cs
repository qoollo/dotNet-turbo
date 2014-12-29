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
    /// Помошник в работе с токеном отмены
    /// </summary>
    internal static class CancellationTokenHelper
    {                   
        private delegate CancellationTokenRegistration RegisterWithoutECDelegate(ref CancellationToken token, Action<object> callback, object state);

        private static RegisterWithoutECDelegate _registerWithoutECDelegate;

        private static readonly object _syncObj = new object();

        /// <summary>
        /// Сгенерировать динамический метод для вызова RegisterWithoutEC
        /// </summary>
        /// <returns>Делегат для сгенерированного метода</returns>
        private static RegisterWithoutECDelegate CreateRegisterWithoutECMethod()
        {
            var RegisterMethod = typeof(CancellationToken).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single(o => o.Name == "Register" && o.GetParameters().Length == 4);

            var method = new DynamicMethod("CancellationToken_RegisterWithoutEC_" + Guid.NewGuid().ToString("N"), typeof(CancellationTokenRegistration),
                new Type[] { typeof(CancellationToken).MakeByRefType(), typeof(Action<object>), typeof(object) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldarg_2);
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Call, RegisterMethod);
            ilGen.Emit(OpCodes.Ret);

            return (RegisterWithoutECDelegate)method.CreateDelegate(typeof(RegisterWithoutECDelegate));
        }


        /// <summary>
        /// Выполнить инициализацию динамчиеских методов
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitDynamicMethods()
        {
            lock (_syncObj)
            {
                if (_registerWithoutECDelegate == null)
                {
                    _registerWithoutECDelegate = CreateRegisterWithoutECMethod();
                }
            }
        }


        /// <summary>
        /// Зарегистрировать коллбэк в токене без захвата ExecutionContext
        /// </summary>
        /// <param name="token">Токен</param>
        /// <param name="callback">Коллбэк</param>
        /// <param name="state">Объект состояния</param>
        /// <returns>Информация о регистрации</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CancellationTokenRegistration RegisterWithoutEC(CancellationToken token, Action<object> callback, object state)
        {
            var action = _registerWithoutECDelegate;
            if (action == null)
            {
                InitDynamicMethods();
                action = _registerWithoutECDelegate;
            }
            return action(ref token, callback, state);
        }
    }
}
