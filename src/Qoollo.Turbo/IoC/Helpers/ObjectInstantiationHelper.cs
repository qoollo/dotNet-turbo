using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics.Contracts;
using Qoollo.Turbo.IoC.Injections;
using Qoollo.Turbo.IoC.ServiceStuff;
using System.Reflection.Emit;

namespace Qoollo.Turbo.IoC.Helpers
{
    /// <summary>
    /// Helper methods for IoC to instantiate objects
    /// </summary>
    internal static class ObjectInstantiationHelper
    {
        #region FindConstructor

        /// <summary>
        /// Looks for the default constructor for the object of specified type
        /// </summary>
        /// <param name="executerType">Type of the object</param>
        /// <param name="onlyPublic">Search only public constructors</param>
        /// <returns>Found constructor</returns>
        public static ConstructorInfo FindConstructor(Type executerType, bool onlyPublic = false)
        {
            Contract.Requires(executerType != null);

            ConstructorInfo res = null;
            ConstructorInfo[] allConstr = null;

            if (onlyPublic)
                allConstr = executerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            else
                allConstr = executerType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (allConstr == null || allConstr.Length == 0)
                return null;

            foreach (var ci in allConstr)
            {
                var attrib = ci.GetCustomAttributes(typeof(DefaultConstructorAttribute), false);
                if (attrib != null && attrib.Length > 0)
                    return ci;
            }

            res = executerType.GetConstructor(System.Type.EmptyTypes);
            if (res != null && res.Attributes.HasFlag(MethodAttributes.Public))
                return res;

            return allConstr.FirstOrDefault(o => o.Attributes.HasFlag(MethodAttributes.Public));
        }

        #endregion



        #region CreateObject with constructor

        /// <summary>
        /// Creates instance of an object using Reflection
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Created object</returns>
        public static object CreateObject(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            Contract.Requires(constructor != null);
            Contract.Requires(objType != null);
            Contract.Requires(injection != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<object>() != null);


            var cparam = constructor.GetParameters();
            object[] args = new object[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                Contract.Assume(cparam[i] != null);
                args[i] = injection.Resolve(cparam[i].ParameterType, cparam[i].Name, objType, extData);
            }

            var res = constructor.Invoke(args);
            Contract.Assume(res != null);
            return res;
        }


        /// <summary>
        /// Creates instance of an object using Reflection by the parameterless constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <returns>Created object</returns>
        public static object CreateObject(Type objType, ConstructorInfo constructor)
        {
            Contract.Requires(constructor != null);
            Contract.Requires(objType != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<object>() != null);


            var cparam = constructor.GetParameters();
            if (cparam != null && cparam.Length > 0)
                throw new CommonIoCException("Only constructor without parameters can be used here");

            var res = constructor.Invoke(new object[] { });
            Contract.Assume(res != null);
            return res;
        }

        #endregion



        #region CreateObject


        /// <summary>
        /// Creates instance of an object using Reflection
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Created object</returns>
        public static object CreateObject(Type objType, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(injection != null);

            Contract.Ensures(Contract.Result<object>() != null);

            var ci = ObjectInstantiationHelper.FindConstructor(objType, false);

            if (ci == null)
                throw new CommonIoCException(
                    string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            object res = ObjectInstantiationHelper.CreateObject(objType, ci, injection, extData);

            Contract.Assume(res != null);
            return res;
        }


        /// <summary>
        /// Creates instance of an object using Reflection by the parameterless constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <returns></returns>
        public static object CreateObject(Type objType)
        {
            Contract.Requires(objType != null);
            Contract.Ensures(Contract.Result<object>() != null);

            var ci = objType.GetConstructor(System.Type.EmptyTypes);

            if (ci == null)
                throw new CommonIoCException(
                    string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            object res = ObjectInstantiationHelper.CreateObject(objType, ci);

            Contract.Assume(res != null);
            return res;
        }

        #endregion



        #region GetReflectionBasedCreationFunction


        /// <summary>
        /// Returns the delegate to create the object of specified type using Reflection
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Function to create an instance of an object</returns>
        public static Func<IInjectionResolver, object> GetReflectionBasedCreationFunction(Type objType, ConstructorInfo constructor, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);

            Contract.Ensures(Contract.Result<Func<IInjectionResolver, object>>() != null);

            Contract.Assert(constructor.DeclaringType == objType);

            return (injection) => CreateObject(objType, constructor, injection, extData);
        }

        /// <summary>
        /// Returns the delegate to create the object of specified type using Reflection
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Function to create an instance of an object</returns>
        public static Func<IInjectionResolver, object> GetReflectionBasedCreationFunction(Type objType, object extData)
        {
            Contract.Requires(objType != null);

            Contract.Ensures(Contract.Result<Func<IInjectionResolver, object>>() != null);


            return (injection) => CreateObject(objType, injection, extData);
        }

        #endregion



        #region ExtractMethodInfo

        /// <summary>
        /// Extracts MethodInfo for instance methods from Expression tree
        /// </summary>
        /// <typeparam name="InstT">The type of an object</typeparam>
        /// <param name="methodCallExpr">Calling expression</param>
        /// <returns>Extracted MethodInfo</returns>
        private static MethodInfo ExtractMethodInfo<InstT>(Expression<Action<InstT>> methodCallExpr)
        {
            Contract.Requires(methodCallExpr != null);
            Contract.Ensures(Contract.Result<MethodInfo>() != null);

            var body = methodCallExpr.Body;

            if (body is MethodCallExpression)
            {
                return (body as MethodCallExpression).Method;
            }

            throw new CommonIoCException("Method call not found in expression");
        }

        /// <summary>
        /// Extracts MethodInfo for static methods from Expression tree
        /// </summary>
        /// <param name="methodCallExpr">Calling expression</param>
        /// <returns>Extracted MethodInfo</returns>
        private static MethodInfo ExtractMethodInfo(Expression<Action> methodCallExpr)
        {
            Contract.Requires(methodCallExpr != null);
            Contract.Ensures(Contract.Result<MethodInfo>() != null);

            var body = methodCallExpr.Body;

            if (body is MethodCallExpression)
            {
                return (body as MethodCallExpression).Method;
            }

            throw new CommonIoCException("Method call not found in expression");
        }


        /// <summary>
        /// Extracts ConstructorInfo from Expression tree
        /// </summary>
        /// <param name="constructorCallExpr">Calling expression</param>
        /// <returns>Extracted ConstructorInfo</returns>
        private static ConstructorInfo ExtractConstructorInfo(Expression<Action> constructorCallExpr)
        {
            Contract.Requires(constructorCallExpr != null);
            Contract.Ensures(Contract.Result<ConstructorInfo>() != null);

            var body = constructorCallExpr.Body;

            if (body is NewExpression)
            {
                return (body as NewExpression).Constructor;
            }

            throw new CommonIoCException("Constructor call not found in expression");
        } 

        #endregion




        #region CreateInjectionExtractionExpr


        /// <summary>
        /// Создаёт Expression для извлечения записи из конкретного объекта injection
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="paramName">Имя параметра, для которого разрешаем зависимость</param>
        /// <param name="forType">Тип, для которого разрешаем зависимость (передаётся в injection.Resolve)</param>
        /// <param name="extData">Расширенная информация для разрешения инъекций (передаётся в injection.Resolve)</param>
        /// <returns>Expression для извлечения записи</returns>
        private static Expression CreateInjectionExtractionExpr(Type key, IInjectionResolver injection, string paramName, Type forType, object extData)
        {
            Contract.Requires(key != null);
            Contract.Requires(injection != null);

            Contract.Ensures(Contract.Result<Expression>() != null);

            var methodInfo = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            Contract.Assume(methodInfo != null);

            return Expression.Convert(
                            Expression.Call(
                                    Expression.Constant(injection, typeof(IInjectionResolver)),
                                    methodInfo,
                                    Expression.Constant(key, typeof(Type)),
                                    Expression.Constant(paramName, typeof(string)),
                                    Expression.Constant(forType, typeof(Type)),
                                    Expression.Constant(extData, typeof(object))),
                            key);
        }


        /// <summary>
        /// Создаёт Expression для извлечения записи из IInjectionResolver, получаемого через выражение param
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="param">Expression для получения IInjectionResolver</param>
        /// <param name="paramName">Имя параметра, для которого разрешаем зависимость</param>
        /// <param name="forType">Тип, для которого разрешаем зависимость (передаётся в injection.Resolve)</param>
        /// <param name="extData">Расширенная информация для разрешения инъекций (передаётся в injection.Resolve)</param>
        /// <returns>Expression для извлечения записи</returns>
        private static Expression CreateInjectionExtractionExpr(Type key, Expression param, string paramName, Type forType, object extData)
        {
            Contract.Requires(key != null);
            Contract.Requires(param != null);

            Contract.Ensures(Contract.Result<Expression>() != null);

            Contract.Assume(param.Type == typeof(IInjectionResolver));

            var methodInfo = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            Contract.Assume(methodInfo != null);

            return Expression.Convert(
                            Expression.Call(
                                    param,
                                    methodInfo,
                                    Expression.Constant(key, typeof(Type)),
                                    Expression.Constant(paramName, typeof(string)),
                                    Expression.Constant(forType, typeof(Type)),
                                    Expression.Constant(extData, typeof(object))),
                            key);
        }

        /// <summary>
        /// Создаёт Expression для извлечения записи из IInjectionResolver, получаемого через выражение param
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="param">Expression для получения IInjectionResolver</param>
        /// <param name="paramName">Expression для получения имени параметра, для которого разрешаем зависимость</param>
        /// <param name="forType">Expression для получения типа, для которого разрешаем зависимость (передаётся в injection.Resolve)</param>
        /// <param name="extData">Expression для получения расширенной информации для разрешения инъекций (передаётся в injection.Resolve)</param>
        /// <returns>Expression для извлечения записи</returns>
        private static Expression CreateInjectionExtractionExpr(Type key, Expression param, Expression paramName, Expression forType, Expression extData)
        {
            Contract.Requires(key != null);
            Contract.Requires(param != null);

            Contract.Ensures(Contract.Result<Expression>() != null);

            Contract.Assume(param.Type == typeof(IInjectionResolver));

            var methodInfo = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            Contract.Assume(methodInfo != null);

            return Expression.Convert(
                            Expression.Call(
                                    param,
                                    methodInfo,
                                    Expression.Constant(key, typeof(Type)),
                                    paramName,
                                    forType,
                                    extData),
                            key);
        }

        #endregion



        #region GetCompiledCreationFunction


        /// <summary>
        /// Возвращает LambdaExpression для создания объекта с передаваемым в качестве параметра объектом разрешения инъекций 
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Сформированный LambdaExpression</returns>
        private static Expression<Func<IInjectionResolver, object>> GetObjectCreationExpression(Type objType, ConstructorInfo constructor, Expression extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(extData != null);
            Contract.Requires(extData.Type == typeof(object));
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Expression<Func<IInjectionResolver, object>>>() != null);

            var injectionParam = Expression.Parameter(typeof(IInjectionResolver), "injection");
            Contract.Assume(injectionParam != null);

            var cparam = constructor.GetParameters();
            Contract.Assume(cparam != null);

            Expression[] args = new Expression[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                Contract.Assume(cparam[i] != null);
                args[i] = CreateInjectionExtractionExpr(cparam[i].ParameterType, injectionParam, Expression.Constant(cparam[i].Name), Expression.Constant(objType, typeof(Type)), extData);
            }

            var finalExpr = Expression.New(constructor, args);
            var linqExpr = Expression.Lambda<Func<IInjectionResolver, object>>(finalExpr, injectionParam);

            Contract.Assume(linqExpr != null);
            return linqExpr;
        }




        /// <summary>
        /// Возвращает функцию, которая создаёт объект типа objType.
        /// Функция представляет собой скомпилированное ExcpressionTree. 
        /// Аргументы извлекаются при каждом обращении из словаря инъекций injection, зашитом в данную функцию.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Функция создания объекта</returns>
        public static Func<object> GetCompiledCreationFunction(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(injection != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Func<object>>() != null);

            var cparam = constructor.GetParameters();
            Contract.Assume(cparam != null);
            Expression[] args = new Expression[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                Contract.Assume(cparam[i] != null);
                args[i] = CreateInjectionExtractionExpr(cparam[i].ParameterType, injection, cparam[i].Name, objType, extData);
            }

            var finalExpr = Expression.New(constructor, args);
            var linqExpr = Expression.Lambda<Func<object>>(finalExpr);

            var res = linqExpr.Compile();
            Contract.Assume(res != null);
            return res;
        }


        /// <summary>
        /// Возвращает функцию, которая создаёт объект типа objType.
        /// Функция представляет собой скомпилированное ExcpressionTree. 
        /// Аргументы извлекаются при каждом обращении из словаря инъекций, передаваемом в качестве параметра
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Expression для получения расширенной информации для разрешения инъекций</param>
        /// <returns>Функция создания объекта</returns>
        private static Func<IInjectionResolver, object> GetCompiledCreationFunction(Type objType, ConstructorInfo constructor, Expression extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(extData != null);
            Contract.Requires(extData.Type == typeof(object));
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Func<IInjectionResolver, object>>() != null);

            var linqExpr = GetObjectCreationExpression(objType, constructor, extData);
            Contract.Assume(linqExpr != null);

            var res = linqExpr.Compile();
            Contract.Assume(res != null);
            return res;
        }


        /// <summary>
        /// Возвращает функцию, которая создаёт объект типа objType.
        /// Функция представляет собой скомпилированное ExcpressionTree. 
        /// Аргументы извлекаются при каждом обращении из словаря инъекций, передаваемом в качестве параметра
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Expression для получения расширенной информации для разрешения инъекций</param>
        /// <returns>Функция создания объекта</returns>
        private static Func<IInjectionResolver, object> GetCompiledCreationFunction(Type objType, Expression extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(extData != null);
            Contract.Requires(extData.Type == typeof(object));

            Contract.Ensures(Contract.Result<Func<IInjectionResolver, object>>() != null);

            var constructor = FindConstructor(objType);
            Contract.Assert(constructor != null);
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));
            Contract.Assume(constructor.DeclaringType == objType);

            return GetCompiledCreationFunction(objType, constructor, extData);
        }

        /// <summary>
        /// Возвращает функцию, которая создаёт объект типа objType.
        /// Функция представляет собой скомпилированное ExcpressionTree. 
        /// Аргументы извлекаются при каждом обращении из словаря инъекций, передаваемом в качестве параметра
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Функция создания объекта</returns>
        public static Func<IInjectionResolver, object> GetCompiledCreationFunction(Type objType, object extData)
        {
            Contract.Requires(objType != null);

            Contract.Ensures(Contract.Result<Func<IInjectionResolver, object>>() != null);

            var constructor = FindConstructor(objType);
            Contract.Assert(constructor != null);
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));
            Contract.Assume(constructor.DeclaringType == objType);

            return GetCompiledCreationFunction(objType, constructor, Expression.Constant(extData, typeof(object)));
        }


        #endregion



        #region GetCompiledArgsInlinedCreationFunction


        /// <summary>
        /// Возвращает функцию, которая создаёт объект типа objType.
        /// Функция представляет собой скомпилированное ExcpressionTree. 
        /// Аргументы зашиты в функцию для ускорения процесса создания.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Функция создания объекта</returns>
        public static Func<object> GetCompiledArgsInlinedCreationFunction(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(injection != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Func<object>>() != null);

            var cparam = constructor.GetParameters();
            Contract.Assume(cparam != null);
            Expression[] args = new Expression[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                Contract.Assume(cparam[i] != null);
                object locArgVal = injection.Resolve(cparam[i].ParameterType, cparam[i].Name, objType, extData);
                args[i] = Expression.Constant(locArgVal);
            }

            var finalExpr = Expression.New(constructor, args);
            var linqExpr = Expression.Lambda<Func<object>>(finalExpr);

            var res = linqExpr.Compile();
            Contract.Assume(res != null);
            return res;
        }

        /// <summary>
        /// Возвращает функцию, которая создаёт объект типа objType.
        /// Функция представляет собой скомпилированное ExcpressionTree. 
        /// Аргументы зашиты в функцию для ускорения процесса создания.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Функция создания объекта</returns>
        public static Func<object> GetCompiledArgsInlinedCreationFunction(Type objType, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(injection != null);

            var constructor = FindConstructor(objType);
            Contract.Assert(constructor != null);
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));
            Contract.Assume(constructor.DeclaringType == objType);

            return GetCompiledArgsInlinedCreationFunction(objType, constructor, injection, extData);
        }


        #endregion



        #region Dynamic Assembly Code Emit supprot

        /// <summary>
        /// Динамическая сборка, внутри которой генерируются типы
        /// </summary>
        private static AssemblyBuilder _dynamicAssembly;
        /// <summary>
        /// Динамический модуль внутри _dynamicAssembly
        /// </summary>
        private static ModuleBuilder _dynamicModule;
        /// <summary>
        /// Объект блокировки для создания _dynamicAssembly и _dynamicModule
        /// </summary>
        private static readonly object _lockObjectForAssembly = new object();
        /// <summary>
        /// Объект блокировки при работе с _dynamicModule
        /// </summary>
        private static readonly object _singleThreadAccessToDynModule = new object();

        /// <summary>
        /// Возвращает динамический модуль для формирования типов на лету
        /// </summary>
        /// <returns>Построитель модуля</returns>
        private static ModuleBuilder GetDynamicModule()
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);

            if (_dynamicModule == null)
            {
                lock (_lockObjectForAssembly)
                {
                    if (_dynamicModule == null)
                    {
                        _dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                              new AssemblyName("IoCObjectInstAsm_" + Guid.NewGuid().ToString("N")),
                              System.Reflection.Emit.AssemblyBuilderAccess.Run);

                        _dynamicModule = _dynamicAssembly.DefineDynamicModule("Module");
                    }
                }
            }

            return _dynamicModule;
        }

        /// <summary>
        /// Генерирует код конструктора с параметрами для заполнения полей объекта из storageFields
        /// </summary>
        /// <param name="constr">Построитель конструктора</param>
        /// <param name="storageFields">Поля класса, которые должны заполнятся в конструкторе из его параметров</param>
        private static void EmitConstructor(ConstructorBuilder constr, params FieldInfo[] storageFields)
        {
            Contract.Requires(constr != null);

            if (storageFields != null)
            {
                for (int i = 0; i < storageFields.Length; i++)
                {
                    Contract.Assume(storageFields[i] != null);
                    constr.DefineParameter(i, ParameterAttributes.None, "c_" + storageFields[i].Name);
                }
            }

            var constrILGen = constr.GetILGenerator();
            constrILGen.Emit(OpCodes.Ldarg_0);
            constrILGen.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            if (storageFields != null)
            {
                for (int i = 0; i < storageFields.Length; i++)
                {
                    constrILGen.Emit(OpCodes.Ldarg_0);
                    switch (i + 1)
                    {
                        case 1:
                            constrILGen.Emit(OpCodes.Ldarg_1);
                            break;
                        case 2:
                            constrILGen.Emit(OpCodes.Ldarg_2);
                            break;
                        case 3:
                            constrILGen.Emit(OpCodes.Ldarg_3);
                            break;
                        default:
                            constrILGen.Emit(OpCodes.Ldarg_S, i + 1);
                            break;
                    }
                    constrILGen.Emit(OpCodes.Stfld, storageFields[i]);
                }
            }

            constrILGen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Генерирует код метода для создания объекта типа objType конструктором constructor с расширенной информацией в поле extData
        /// </summary>
        /// <param name="method">MethodBuilder</param>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Поле, в котором хранятся расширенные параметры</param>
        private static void EmitMethodWithResolver(MethodBuilder method, Type objType, ConstructorInfo constructor, FieldInfo extData)
        {
            Contract.Requires(method != null);
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(extData != null);
            Contract.Requires(constructor.DeclaringType == objType);
            Contract.Requires(extData.FieldType == typeof(object));

            method.DefineParameter(0, ParameterAttributes.None, "resolver");

            var GetTypeFromHandle = ExtractMethodInfo(() => Type.GetTypeFromHandle(new RuntimeTypeHandle()));
            Contract.Assume(GetTypeFromHandle != null);
            var Resolve = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            Contract.Assume(Resolve != null);

            var methodILGen = method.GetILGenerator();

            var cparams = constructor.GetParameters();

            for (int i = 0; i < cparams.Length; i++)
            {
                methodILGen.Emit(OpCodes.Ldarg_1);
                methodILGen.Emit(OpCodes.Ldtoken, cparams[i].ParameterType);
                methodILGen.Emit(OpCodes.Call, GetTypeFromHandle);
                methodILGen.Emit(OpCodes.Ldstr, cparams[i].Name);
                methodILGen.Emit(OpCodes.Ldtoken, objType);
                methodILGen.Emit(OpCodes.Call, GetTypeFromHandle);
                methodILGen.Emit(OpCodes.Ldarg_0);
                methodILGen.Emit(OpCodes.Ldfld, extData);
                methodILGen.Emit(OpCodes.Callvirt, Resolve);

                if (cparams[i].ParameterType.IsValueType)
                    methodILGen.Emit(OpCodes.Unbox_Any, cparams[i].ParameterType);
                else if (cparams[i].ParameterType != typeof(object))
                    methodILGen.Emit(OpCodes.Castclass, cparams[i].ParameterType);
            }

            methodILGen.Emit(OpCodes.Newobj, constructor);
            methodILGen.Emit(OpCodes.Ret);
        }


        /// <summary>
        /// Генерирует код метода для создания объекта типа objType конструктором constructor.
        /// Все параметры для конструктора objType находятся в полях стоящегося объекта, переданных в массиве allFields.
        /// Порядок должен точно соответствовать
        /// </summary>
        /// <param name="method">MethodBuilder</param>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="allFields">Массив полей, в которых хранятся параметры для конструктора объекта</param>
        private static void EmitMethodWithInlinedParams(MethodBuilder method, Type objType, ConstructorInfo constructor, FieldInfo[] allFields)
        {
            Contract.Requires(method != null);
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(allFields != null);
            Contract.Requires(constructor.DeclaringType == objType);

            method.DefineParameter(0, ParameterAttributes.None, "resolver");

            var GetTypeFromHandle = ExtractMethodInfo(() => Type.GetTypeFromHandle(new RuntimeTypeHandle()));
            Contract.Assume(GetTypeFromHandle != null);

            var methodILGen = method.GetILGenerator();

            var cparams = constructor.GetParameters();

            Contract.Assert(cparams.Length == allFields.Length);

            for (int i = 0; i < cparams.Length; i++)
            {
                Contract.Assert(cparams[i].ParameterType == allFields[i].FieldType);
                methodILGen.Emit(OpCodes.Ldarg_0);
                methodILGen.Emit(OpCodes.Ldfld, allFields[i]);
            }

            methodILGen.Emit(OpCodes.Newobj, constructor);
            methodILGen.Emit(OpCodes.Ret);
        }


        /// <summary>
        /// Строит тип, реализующий интерфейс IInstanceCreator для создания объекта типа objType
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <returns>Построенный тип</returns>
        private static Type BuildTypeOfInstanceCreator(Type objType, ConstructorInfo constructor)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Type>() != null);

            var methodToImplement = ExtractMethodInfo<IInstanceCreator>(a => a.CreateInstance(null));
            Contract.Assume(methodToImplement != null);

            lock (_singleThreadAccessToDynModule)
            {
                var moduleBuilder = GetDynamicModule();

                var typeBuilder = moduleBuilder.DefineType("InstanceCreator_" + objType.Name + "_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public,
                    typeof(object), new Type[] { typeof(IInstanceCreator) });

                var extInfoField = typeBuilder.DefineField("extInfo", typeof(object), FieldAttributes.Private | FieldAttributes.InitOnly);

                var constr = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, new Type[] { typeof(object) });
                Contract.Assume(constr != null);
                EmitConstructor(constr, extInfoField);

                var method = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis,
                    typeof(object), new Type[] { typeof(IInjectionResolver) });
                Contract.Assume(method != null);
                EmitMethodWithResolver(method, objType, constructor, extInfoField);

                var interfMethod = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis, typeof(object), new Type[] { typeof(IInjectionResolver) });
                typeBuilder.DefineMethodOverride(interfMethod, methodToImplement);
                Contract.Assume(interfMethod != null);
                EmitMethodWithResolver(interfMethod, objType, constructor, extInfoField);


                return typeBuilder.CreateType();
            }
        }

        /// <summary>
        /// Строит тип, реализующий интерфейс IInstanceCreatorNoParam для создания объекта типа objType.
        /// Параметры передаются в конструктор создаваемого объекта
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <returns>Построенный тип</returns>
        private static Type BuildTypeOfInstanceCreatorNoParam(Type objType, ConstructorInfo constructor)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Type>() != null);

            var methodToImplement = ExtractMethodInfo<IInstanceCreatorNoParam>(a => a.CreateInstance());
            Contract.Assume(methodToImplement != null);

            lock (_singleThreadAccessToDynModule)
            {
                var moduleBuilder = GetDynamicModule();

                var typeBuilder = moduleBuilder.DefineType("InstanceCreatorNoParam_" + objType.Name + "_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public,
                    typeof(object), new Type[] { typeof(IInstanceCreatorNoParam) });

                var constrParams = constructor.GetParameters();

                FieldInfo[] allFields = new FieldInfo[constrParams.Length];
                for (int i = 0; i < constrParams.Length; i++)
                    allFields[i] = typeBuilder.DefineField(constrParams[i].Name, constrParams[i].ParameterType, FieldAttributes.Private | FieldAttributes.InitOnly);


                var constr = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, allFields.Select(o => o.FieldType).ToArray());
                Contract.Assume(constr != null);
                EmitConstructor(constr, allFields);

                var method = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis,
                    typeof(object), Type.EmptyTypes);
                Contract.Assume(method != null);
                EmitMethodWithInlinedParams(method, objType, constructor, allFields);

                var intefMethod = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis, typeof(object), Type.EmptyTypes);
                typeBuilder.DefineMethodOverride(intefMethod, methodToImplement);
                Contract.Assume(intefMethod != null);
                EmitMethodWithInlinedParams(intefMethod, objType, constructor, allFields);


                return typeBuilder.CreateType();
            }
        }



        #endregion



        #region Build InstanceCreator in dynamic assembly

        /// <summary>
        /// Создаёт объект, реализующий IInstanceCreator, для создания объекта типа objType конструктором constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданный объект</returns>
        private static object GetInstanceCreatorObject(Type objType, ConstructorInfo constructor, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<object>() != null);
            Contract.Ensures(Contract.Result<object>() is IInstanceCreator);

            var type = BuildTypeOfInstanceCreator(objType, constructor);
            Contract.Assume(type != null);

            return Activator.CreateInstance(type, extData);
        }

        /// <summary>
        /// Создаёт объект, реализующий IInstanceCreatorNoParam, для создания объекта типа objType конструктором constructor.
        /// Параметры конструктора извлекаются 1 раз и зашиваются в объект.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданный объект</returns>
        private static object GetInstanceCreatorNoParamObject(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(injection != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<object>() != null);
            Contract.Ensures(Contract.Result<object>() is IInstanceCreatorNoParam);

            var type = BuildTypeOfInstanceCreatorNoParam(objType, constructor);
            Contract.Assume(type != null);

            return CreateObject(type, injection, extData);
        }


        /// <summary>
        /// Создаёт объект IInstanceCreator для создания объекта типа objType конструктором constructor.
        /// Тип объекта генерируется на лету внутри динамической сборки
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданный объект IInstanceCreator</returns>
        public static IInstanceCreator BuildInstanceCreatorInDynAssembly(Type objType, ConstructorInfo constructor, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<IInstanceCreator>() != null);

            return GetInstanceCreatorObject(objType, constructor, extData) as IInstanceCreator;
        }

        /// <summary>
        /// Создаёт объект IInstanceCreatorNoParam для создания объекта типа objType конструктором constructor.
        /// Тип объекта генерируется на лету внутри динамической сборки.
        /// Параметры для конструктора выбираются 1 раз и зашиваются в объект IInstanceCreatorNoParam
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданный объект IInstanceCreatorNoParam</returns>
        public static IInstanceCreatorNoParam BuildInstanceCreatorNoParamInDynAssembly(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(injection != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<IInstanceCreatorNoParam>() != null);

            return GetInstanceCreatorNoParamObject(objType, constructor, injection, extData) as IInstanceCreatorNoParam;
        }


        /// <summary>
        /// Создаёт функцию для создания объекта типа objType конструктором constructor.
        /// Тип объекта генерируется на лету внутри динамической сборки
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданная функция</returns>
        public static Func<IInjectionResolver, object> BuildCreatorFuncInDynAssembly(Type objType, ConstructorInfo constructor, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Func<IInjectionResolver, object>>() != null);

            var inst = GetInstanceCreatorObject(objType, constructor, extData);
            Contract.Assume(inst != null);

            var instType = inst.GetType();
            var methodName = ExtractMethodInfo<IInstanceCreator>(a => a.CreateInstance(null)).Name;
            var method = instType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Contract.Assume(method != null);

            var deleg = Delegate.CreateDelegate(typeof(Func<IInjectionResolver, object>), inst, method);

            return (Func<IInjectionResolver, object>)deleg;
        }

        /// <summary>
        /// Создаёт функцию для создания объекта типа objType конструктором constructor.
        /// Тип объекта генерируется на лету внутри динамической сборки.
        /// Параметры для конструктора выбираются 1 раз и зашиваются в объект создания
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданная функция</returns>
        public static Func<object> BuildCreatorFuncNoParamInDynAssembly(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(constructor != null);
            Contract.Requires(injection != null);
            Contract.Requires(constructor.DeclaringType == objType);

            Contract.Ensures(Contract.Result<Func<object>>() != null);

            var inst = GetInstanceCreatorNoParamObject(objType, constructor, injection, extData);
            Contract.Assume(inst != null);

            var instType = inst.GetType();
            var methodName = ExtractMethodInfo<IInstanceCreatorNoParam>(a => a.CreateInstance()).Name;
            var method = instType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Contract.Assume(method != null);

            var deleg = Delegate.CreateDelegate(typeof(Func<object>), inst, method);

            return (Func<object>)deleg;
        }


        /// <summary>
        /// Создаёт объект IInstanceCreator для создания объекта типа objType.
        /// Тип объекта генерируется на лету внутри динамической сборки
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданный объект IInstanceCreator</returns>
        public static IInstanceCreator BuildInstanceCreatorInDynAssembly(Type objType, object extData)
        {
            Contract.Requires(objType != null);

            Contract.Ensures(Contract.Result<IInstanceCreator>() != null);

            var constructor = FindConstructor(objType);
            Contract.Assert(constructor != null);
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildInstanceCreatorInDynAssembly(objType, constructor, extData);
        }


        /// <summary>
        /// Создаёт объект IInstanceCreatorNoParam для создания объекта типа objType.
        /// Тип объекта генерируется на лету внутри динамической сборки.
        /// Параметры для конструктора выбираются 1 раз и зашиваются в объект IInstanceCreatorNoParam
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданный объект IInstanceCreatorNoParam</returns>
        public static IInstanceCreatorNoParam BuildInstanceCreatorNoParamInDynAssembly(Type objType, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(injection != null);

            Contract.Ensures(Contract.Result<IInstanceCreatorNoParam>() != null);

            var constructor = FindConstructor(objType);
            Contract.Assert(constructor != null);
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildInstanceCreatorNoParamInDynAssembly(objType, constructor, injection, extData);
        }


        /// <summary>
        /// Создаёт функцию для создания объекта типа objType.
        /// Тип объекта генерируется на лету внутри динамической сборки
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданная функция</returns>
        public static Func<IInjectionResolver, object> BuildCreatorFuncInDynAssembly(Type objType, object extData)
        {
            Contract.Requires(objType != null);

            Contract.Ensures(Contract.Result<Func<IInjectionResolver, object>>() != null);

            var constructor = FindConstructor(objType);
            Contract.Assert(constructor != null);
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildCreatorFuncInDynAssembly(objType, constructor, extData);
        }


        /// <summary>
        /// Создаёт функцию для создания объекта типа objType.
        /// Тип объекта генерируется на лету внутри динамической сборки.
        /// Параметры для конструктора выбираются 1 раз и зашиваются в объект создания
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Созданная функция</returns>
        public static Func<object> BuildCreatorFuncNoParamInDynAssembly(Type objType, IInjectionResolver injection, object extData)
        {
            Contract.Requires(objType != null);
            Contract.Requires(injection != null);

            Contract.Ensures(Contract.Result<Func<object>>() != null);

            var constructor = FindConstructor(objType);
            Contract.Assert(constructor != null);
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildCreatorFuncNoParamInDynAssembly(objType, constructor, injection, extData);
        } 

        #endregion
    }
}
