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
using System.Diagnostics;

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
            TurboContract.Requires(executerType != null, conditionString: "executerType != null");

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
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<object>() != null);


            var cparam = constructor.GetParameters();
            object[] args = new object[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                TurboContract.Assert(cparam[i] != null, conditionString: "cparam[i] != null");
                args[i] = injection.Resolve(cparam[i].ParameterType, cparam[i].Name, objType, extData);
            }

            var res = constructor.Invoke(args);
            TurboContract.Assert(res != null, conditionString: "res != null");
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
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<object>() != null);


            var cparam = constructor.GetParameters();
            if (cparam != null && cparam.Length > 0)
                throw new CommonIoCException("Only constructor without parameters can be used here");

            var res = constructor.Invoke(new object[] { });
            TurboContract.Assert(res != null, conditionString: "res != null");
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
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");

            TurboContract.Ensures(TurboContract.Result<object>() != null);

            var ci = ObjectInstantiationHelper.FindConstructor(objType, false);

            if (ci == null)
                throw new CommonIoCException(
                    string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            object res = ObjectInstantiationHelper.CreateObject(objType, ci, injection, extData);

            TurboContract.Assert(res != null, conditionString: "res != null");
            return res;
        }


        /// <summary>
        /// Creates instance of an object using Reflection by the parameterless constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <returns></returns>
        public static object CreateObject(Type objType)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Ensures(TurboContract.Result<object>() != null);

            var ci = objType.GetConstructor(System.Type.EmptyTypes);

            if (ci == null)
                throw new CommonIoCException(
                    string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            object res = ObjectInstantiationHelper.CreateObject(objType, ci);

            TurboContract.Assert(res != null, conditionString: "res != null");
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
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");

            TurboContract.Ensures(TurboContract.Result<Func<IInjectionResolver, object>>() != null);

            TurboContract.Assert(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

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
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            TurboContract.Ensures(TurboContract.Result<Func<IInjectionResolver, object>>() != null);


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
            TurboContract.Requires(methodCallExpr != null, conditionString: "methodCallExpr != null");
            TurboContract.Ensures(TurboContract.Result<MethodInfo>() != null);

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
            TurboContract.Requires(methodCallExpr != null, conditionString: "methodCallExpr != null");
            TurboContract.Ensures(TurboContract.Result<MethodInfo>() != null);

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
            TurboContract.Requires(constructorCallExpr != null, conditionString: "constructorCallExpr != null");
            TurboContract.Ensures(TurboContract.Result<ConstructorInfo>() != null);

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
        /// Creates an ExpressionTree to resolve the required injection object from IInjectionResolver
        /// </summary>
        /// <param name="key">The type of the injection object to be resolved</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="paramName">The name of the parameter to that the injection will be performed (can be null, required for injection.Resolve)</param>
        /// <param name="forType">The type of the object to be created (can be null, required for injection.Resolve)</param>
        /// <param name="extData">Extended information supplied by the user (can be null, required for injection.Resolve)</param>
        /// <returns>Expression Tree to resolve the injection from IInjectionResolver</returns>
        private static Expression CreateInjectionExtractionExpr(Type key, IInjectionResolver injection, string paramName, Type forType, object extData)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");

            TurboContract.Ensures(TurboContract.Result<Expression>() != null);

            var methodInfo = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            TurboContract.Assert(methodInfo != null, conditionString: "methodInfo != null");

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
        /// Creates an ExpressionTree to resolve the required injection object from IInjectionResolver, passed as Expression object ('param')
        /// </summary>
        /// <param name="key">The type of the injection object to be resolved</param>
        /// <param name="param">ExpressionTree that returns an IInjectionResolver</param>
        /// <param name="paramName">The name of the parameter to that the injection will be performed (can be null, required for injection.Resolve)</param>
        /// <param name="forType">The type of the object to be created (can be null, required for injection.Resolve)</param>
        /// <param name="extData">Extended information supplied by the user (can be null, required for injection.Resolve)</param>
        /// <returns>Expression Tree to resolve the injection from IInjectionResolver</returns>
        private static Expression CreateInjectionExtractionExpr(Type key, Expression param, string paramName, Type forType, object extData)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(param != null, conditionString: "param != null");

            TurboContract.Ensures(TurboContract.Result<Expression>() != null);

            TurboContract.Assert(param.Type == typeof(IInjectionResolver), conditionString: "param.Type == typeof(IInjectionResolver)");

            var methodInfo = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            TurboContract.Assert(methodInfo != null, conditionString: "methodInfo != null");

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
        /// Creates an ExpressionTree to resolve the required injection object from IInjectionResolver, passed as Expression object ('param')
        /// </summary>
        /// <param name="key">The type of the injection object to be resolved</param>
        /// <param name="param">ExpressionTree that returns an IInjectionResolver</param>
        /// <param name="paramName">ExpressionTree that returns the name of the parameter to that the injection will be performed (required for injection.Resolve)</param>
        /// <param name="forType">ExpressionTree that returns the type of the object to be created (required for injection.Resolve)</param>
        /// <param name="extData">ExpressionTree that returns the extended information supplied by the user (required for injection.Resolve)</param>
        /// <returns>Expression Tree to resolve the injection from IInjectionResolver</returns>
        private static Expression CreateInjectionExtractionExpr(Type key, Expression param, Expression paramName, Expression forType, Expression extData)
        {
            TurboContract.Requires(key != null, conditionString: "key != null");
            TurboContract.Requires(param != null, conditionString: "param != null");

            TurboContract.Ensures(TurboContract.Result<Expression>() != null);

            TurboContract.Assert(param.Type == typeof(IInjectionResolver), conditionString: "param.Type == typeof(IInjectionResolver)");

            var methodInfo = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            TurboContract.Assert(methodInfo != null, conditionString: "methodInfo != null");

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
        /// Builds the LambdaExpression that creates an object of the specified type
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Final LambdaExpression to create an object of specified type</returns>
        private static Expression<Func<IInjectionResolver, object>> GetObjectCreationExpression(Type objType, ConstructorInfo constructor, Expression extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(extData != null, conditionString: "extData != null");
            TurboContract.Requires(extData.Type == typeof(object), conditionString: "extData.Type == typeof(object)");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Expression<Func<IInjectionResolver, object>>>() != null);

            var injectionParam = Expression.Parameter(typeof(IInjectionResolver), "injection");
            TurboContract.Assert(injectionParam != null, conditionString: "injectionParam != null");

            var cparam = constructor.GetParameters();
            TurboContract.Assert(cparam != null, conditionString: "cparam != null");

            Expression[] args = new Expression[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                TurboContract.Assert(cparam[i] != null, conditionString: "cparam[i] != null");
                args[i] = CreateInjectionExtractionExpr(cparam[i].ParameterType, injectionParam, Expression.Constant(cparam[i].Name), Expression.Constant(objType, typeof(Type)), extData);
            }

            var finalExpr = Expression.New(constructor, args);
            var linqExpr = Expression.Lambda<Func<IInjectionResolver, object>>(finalExpr, injectionParam);

            TurboContract.Assert(linqExpr != null, conditionString: "linqExpr != null");
            return linqExpr;
        }




        /// <summary>
        /// Builds and compiles function to create object of type 'objType' with the specified constructor.
        /// That function are built and compiled with ExpressionTree.
        /// Arguments for injection are extracted from 'injection' object on every call.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Function to create the instance of 'objType'</returns>
        public static Func<object> GetCompiledCreationFunction(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Func<object>>() != null);

            var cparam = constructor.GetParameters();
            TurboContract.Assert(cparam != null, conditionString: "cparam != null");
            Expression[] args = new Expression[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                TurboContract.Assert(cparam[i] != null, conditionString: "cparam[i] != null");
                args[i] = CreateInjectionExtractionExpr(cparam[i].ParameterType, injection, cparam[i].Name, objType, extData);
            }

            var finalExpr = Expression.New(constructor, args);
            var linqExpr = Expression.Lambda<Func<object>>(finalExpr);

            var res = linqExpr.Compile();
            TurboContract.Assert(res != null, conditionString: "res != null");
            return res;
        }


        /// <summary>
        /// Builds and compiles function to create object of type 'objType' with the specified constructor.
        /// That function are built and compiled with ExpressionTree.
        /// Arguments for injection are extracted on every call.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">ExpressionTree that returns the extended information supplied by the user (required for injection.Resolve)</param>
        /// <returns>Function to create the instance of 'objType'</returns>
        private static Func<IInjectionResolver, object> GetCompiledCreationFunction(Type objType, ConstructorInfo constructor, Expression extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(extData != null, conditionString: "extData != null");
            TurboContract.Requires(extData.Type == typeof(object), conditionString: "extData.Type == typeof(object)");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Func<IInjectionResolver, object>>() != null);

            var linqExpr = GetObjectCreationExpression(objType, constructor, extData);
            TurboContract.Assert(linqExpr != null, conditionString: "linqExpr != null");

            var res = linqExpr.Compile();
            TurboContract.Assert(res != null, conditionString: "res != null");
            return res;
        }


        /// <summary>
        /// Builds and compiles function to create object of type 'objType' with the default constructor.
        /// That function are built and compiled with ExpressionTree.
        /// Arguments for injection are extracted on every call.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">ExpressionTree that returns the extended information supplied by the user (required for injection.Resolve)</param>
        /// <returns>Function to create the instance of 'objType'</returns>
        private static Func<IInjectionResolver, object> GetCompiledCreationFunction(Type objType, Expression extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(extData != null, conditionString: "extData != null");
            TurboContract.Requires(extData.Type == typeof(object), conditionString: "extData.Type == typeof(object)");

            TurboContract.Ensures(TurboContract.Result<Func<IInjectionResolver, object>>() != null);

            var constructor = FindConstructor(objType);
            TurboContract.Assert(constructor != null, conditionString: "constructor != null");
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));
            TurboContract.Assert(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            return GetCompiledCreationFunction(objType, constructor, extData);
        }

        /// <summary>
        /// Builds and compiles function to create object of type 'objType' with the default constructor.
        /// That function are built and compiled with ExpressionTree.
        /// Arguments for injection are extracted on every call.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Function to create the instance of 'objType'</returns>
        public static Func<IInjectionResolver, object> GetCompiledCreationFunction(Type objType, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            TurboContract.Ensures(TurboContract.Result<Func<IInjectionResolver, object>>() != null);

            var constructor = FindConstructor(objType);
            TurboContract.Assert(constructor != null, conditionString: "constructor != null");
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));
            TurboContract.Assert(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            return GetCompiledCreationFunction(objType, constructor, Expression.Constant(extData, typeof(object)));
        }


        #endregion



        #region GetCompiledArgsInlinedCreationFunction


        /// <summary>
        /// Builds and compiles function to create object of type 'objType' with the specified constructor.
        /// That function are built and compiled with ExpressionTree.
        /// Arguments for injection are extracted only once during this call.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Function to create the instance of 'objType'</returns>
        public static Func<object> GetCompiledArgsInlinedCreationFunction(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Func<object>>() != null);

            var cparam = constructor.GetParameters();
            TurboContract.Assert(cparam != null, conditionString: "cparam != null");
            Expression[] args = new Expression[cparam.Length];

            for (int i = 0; i < cparam.Length; i++)
            {
                TurboContract.Assert(cparam[i] != null, conditionString: "cparam[i] != null");
                object locArgVal = injection.Resolve(cparam[i].ParameterType, cparam[i].Name, objType, extData);
                args[i] = Expression.Constant(locArgVal);
            }

            var finalExpr = Expression.New(constructor, args);
            var linqExpr = Expression.Lambda<Func<object>>(finalExpr);

            var res = linqExpr.Compile();
            TurboContract.Assert(res != null, conditionString: "res != null");
            return res;
        }

        /// <summary>
        /// Builds and compiles function to create object of type 'objType' with the default constructor.
        /// That function are built and compiled with ExpressionTree.
        /// Arguments for injection are extracted only once during this call.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Function to create the instance of 'objType'</returns>
        public static Func<object> GetCompiledArgsInlinedCreationFunction(Type objType, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");

            var constructor = FindConstructor(objType);
            TurboContract.Assert(constructor != null, conditionString: "constructor != null");
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));
            TurboContract.Assert(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            return GetCompiledArgsInlinedCreationFunction(objType, constructor, injection, extData);
        }


        #endregion



        #region Dynamic Assembly Code Emit support

        /// <summary>
        /// Dynamic assembly to emit required types and methods
        /// </summary>
        private static AssemblyBuilder _dynamicAssembly;
        /// <summary>
        /// Dynamic module inside _dynamicAssembly
        /// </summary>
        private static ModuleBuilder _dynamicModule;
        /// <summary>
        /// Sync object to reliably create _dynamicAssembly and _dynamicModule
        /// </summary>
        private static readonly object _lockObjectForAssembly = new object();
        /// <summary>
        /// Sync object for emitting the code to _dynamicModule
        /// </summary>
        private static readonly object _singleThreadAccessToDynModule = new object();

        /// <summary>
        /// Returns the dynamic module, that can be used to emit the code
        /// </summary>
        /// <returns>ModuleBuilder</returns>
        private static ModuleBuilder GetDynamicModule()
        {
            TurboContract.Ensures(TurboContract.Result<ModuleBuilder>() != null);

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
        /// Emits class constructor that copies all arguments to the 'storageFields' fields
        /// </summary>
        /// <param name="constr">Constructor builder</param>
        /// <param name="storageFields">Fields that should be inited by the constructor</param>
        private static void EmitConstructor(ConstructorBuilder constr, params FieldInfo[] storageFields)
        {
            TurboContract.Requires(constr != null, conditionString: "constr != null");

            if (storageFields != null)
            {
                for (int i = 0; i < storageFields.Length; i++)
                {
                    TurboContract.Assert(storageFields[i] != null, conditionString: "storageFields[i] != null");
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
        /// Emits object creation method that parametrized with 'IInjectionResolver' object
        /// </summary>
        /// <param name="method">MethodBuilder</param>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Field that stores the custom data passed by the user</param>
        private static void EmitMethodWithResolver(MethodBuilder method, Type objType, ConstructorInfo constructor, FieldInfo extData)
        {
            TurboContract.Requires(method != null, conditionString: "method != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(extData != null, conditionString: "extData != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");
            TurboContract.Requires(extData.FieldType == typeof(object), conditionString: "extData.FieldType == typeof(object)");

            method.DefineParameter(0, ParameterAttributes.None, "resolver");

            var GetTypeFromHandle = ExtractMethodInfo(() => Type.GetTypeFromHandle(new RuntimeTypeHandle()));
            TurboContract.Assert(GetTypeFromHandle != null, conditionString: "GetTypeFromHandle != null");
            var Resolve = ExtractMethodInfo<IInjectionResolver>(a => a.Resolve(typeof(int), "", typeof(int), null));
            TurboContract.Assert(Resolve != null, conditionString: "Resolve != null");

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
        /// Emits object creation method. All constructor parameters should be stored in fields 'allFields'
        /// </summary>
        /// <param name="method">MethodBuilder</param>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="allFields">Fields that strores the objects required for the constructor</param>
        private static void EmitMethodWithInlinedParams(MethodBuilder method, Type objType, ConstructorInfo constructor, FieldInfo[] allFields)
        {
            TurboContract.Requires(method != null, conditionString: "method != null");
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(allFields != null, conditionString: "allFields != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            method.DefineParameter(0, ParameterAttributes.None, "resolver");

            var GetTypeFromHandle = ExtractMethodInfo(() => Type.GetTypeFromHandle(new RuntimeTypeHandle()));
            TurboContract.Assert(GetTypeFromHandle != null, conditionString: "GetTypeFromHandle != null");

            var methodILGen = method.GetILGenerator();

            var cparams = constructor.GetParameters();

            TurboContract.Assert(cparams.Length == allFields.Length, conditionString: "cparams.Length == allFields.Length");

            for (int i = 0; i < cparams.Length; i++)
            {
                TurboContract.Assert(cparams[i].ParameterType == allFields[i].FieldType, conditionString: "cparams[i].ParameterType == allFields[i].FieldType");
                methodILGen.Emit(OpCodes.Ldarg_0);
                methodILGen.Emit(OpCodes.Ldfld, allFields[i]);
            }

            methodILGen.Emit(OpCodes.Newobj, constructor);
            methodILGen.Emit(OpCodes.Ret);
        }


        /// <summary>
        /// Generates the dynamic type that implements 'IInstanceCreator' interface to create an instance of 'objType'
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <returns>Built type</returns>
        private static Type BuildTypeOfInstanceCreator(Type objType, ConstructorInfo constructor)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Type>() != null);

            var methodToImplement = ExtractMethodInfo<IInstanceCreator>(a => a.CreateInstance(null));
            TurboContract.Assert(methodToImplement != null, conditionString: "methodToImplement != null");

            lock (_singleThreadAccessToDynModule)
            {
                var moduleBuilder = GetDynamicModule();

                var typeBuilder = moduleBuilder.DefineType("InstanceCreator_" + objType.Name + "_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public,
                    typeof(object), new Type[] { typeof(IInstanceCreator) });

                var extInfoField = typeBuilder.DefineField("extInfo", typeof(object), FieldAttributes.Private | FieldAttributes.InitOnly);

                var constr = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, new Type[] { typeof(object) });
                TurboContract.Assert(constr != null, conditionString: "constr != null");
                EmitConstructor(constr, extInfoField);

                var method = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis,
                    typeof(object), new Type[] { typeof(IInjectionResolver) });
                TurboContract.Assert(method != null, conditionString: "method != null");
                EmitMethodWithResolver(method, objType, constructor, extInfoField);

                var interfMethod = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis, typeof(object), new Type[] { typeof(IInjectionResolver) });
                typeBuilder.DefineMethodOverride(interfMethod, methodToImplement);
                TurboContract.Assert(interfMethod != null, conditionString: "interfMethod != null");
                EmitMethodWithResolver(interfMethod, objType, constructor, extInfoField);


                return typeBuilder.CreateType();
            }
        }

        /// <summary>
        /// Generates the dynamic type that implements 'IInstanceCreatorNoParam' interface to create an instance of 'objType'.
        /// All constructor parameters resolves only once and stores internally in the fields.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <returns>Built type</returns>
        private static Type BuildTypeOfInstanceCreatorNoParam(Type objType, ConstructorInfo constructor)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Type>() != null);

            var methodToImplement = ExtractMethodInfo<IInstanceCreatorNoParam>(a => a.CreateInstance());
            TurboContract.Assert(methodToImplement != null, conditionString: "methodToImplement != null");

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
                TurboContract.Assert(constr != null, conditionString: "constr != null");
                EmitConstructor(constr, allFields);

                var method = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis,
                    typeof(object), Type.EmptyTypes);
                TurboContract.Assert(method != null, conditionString: "method != null");
                EmitMethodWithInlinedParams(method, objType, constructor, allFields);

                var intefMethod = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis, typeof(object), Type.EmptyTypes);
                typeBuilder.DefineMethodOverride(intefMethod, methodToImplement);
                TurboContract.Assert(intefMethod != null, conditionString: "intefMethod != null");
                EmitMethodWithInlinedParams(intefMethod, objType, constructor, allFields);


                return typeBuilder.CreateType();
            }
        }



        #endregion



        #region Build InstanceCreator in dynamic assembly

        /// <summary>
        /// Emits and creates an instance of an object that implements IInstanceCreator.
        /// That object can be used to create an object of 'objType' with specified constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Instance of an object that implements IInstanceCreator</returns>
        private static object GetInstanceCreatorObject(Type objType, ConstructorInfo constructor, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<object>() != null);
            TurboContract.Ensures(TurboContract.Result<object>() is IInstanceCreator);

            var type = BuildTypeOfInstanceCreator(objType, constructor);
            TurboContract.Assert(type != null, conditionString: "type != null");

            return Activator.CreateInstance(type, extData);
        }

        /// <summary>
        /// Emits and creates an instance of an object that implements IInstanceCreatorNoParam.
        /// That object can be used to create an object of 'objType' with specified constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Instance of an object that implements IInstanceCreatorNoParam</returns>
        private static object GetInstanceCreatorNoParamObject(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<object>() != null);
            TurboContract.Ensures(TurboContract.Result<object>() is IInstanceCreatorNoParam);

            var type = BuildTypeOfInstanceCreatorNoParam(objType, constructor);
            TurboContract.Assert(type != null, conditionString: "type != null");

            return CreateObject(type, injection, extData);
        }


        /// <summary>
        /// Emits and creates an instance of an object that implements IInstanceCreator.
        /// That object can be used to create an object of 'objType' with specified constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>IInstanceCreator to create 'objType' object</returns>
        public static IInstanceCreator BuildInstanceCreatorInDynAssembly(Type objType, ConstructorInfo constructor, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<IInstanceCreator>() != null);

            return GetInstanceCreatorObject(objType, constructor, extData) as IInstanceCreator;
        }

        /// <summary>
        /// Emits and creates an instance of an object that implements IInstanceCreatorNoParam.
        /// That object can be used to create an object of 'objType' with specified constructor.
        /// All constructor parameters resolves only once and stores internally in the fields.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>IInstanceCreatorNoParam to create 'objType' object</returns>
        public static IInstanceCreatorNoParam BuildInstanceCreatorNoParamInDynAssembly(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<IInstanceCreatorNoParam>() != null);

            return GetInstanceCreatorNoParamObject(objType, constructor, injection, extData) as IInstanceCreatorNoParam;
        }


        /// <summary>
        /// Creates a delegate to the method, that can create an object of 'objType' with specified constructor.
        /// That method emits in runtime to the dynamic assembly.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Delegate for the emitted method</returns>
        public static Func<IInjectionResolver, object> BuildCreatorFuncInDynAssembly(Type objType, ConstructorInfo constructor, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Func<IInjectionResolver, object>>() != null);

            var inst = GetInstanceCreatorObject(objType, constructor, extData);
            TurboContract.Assert(inst != null, conditionString: "inst != null");

            var instType = inst.GetType();
            var methodName = ExtractMethodInfo<IInstanceCreator>(a => a.CreateInstance(null)).Name;
            var method = instType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            TurboContract.Assert(method != null, conditionString: "method != null");

            var deleg = Delegate.CreateDelegate(typeof(Func<IInjectionResolver, object>), inst, method);

            return (Func<IInjectionResolver, object>)deleg;
        }

        /// <summary>
        /// Creates a delegate to the method, that can create an object of 'objType' with specified constructor.
        /// That method emits in runtime to the dynamic assembly.
        /// All constructor parameters resolves only once and stores internally.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="constructor">Constructor</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Delegate for the emitted method</returns>
        public static Func<object> BuildCreatorFuncNoParamInDynAssembly(Type objType, ConstructorInfo constructor, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(constructor != null, conditionString: "constructor != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");
            TurboContract.Requires(constructor.DeclaringType == objType, conditionString: "constructor.DeclaringType == objType");

            TurboContract.Ensures(TurboContract.Result<Func<object>>() != null);

            var inst = GetInstanceCreatorNoParamObject(objType, constructor, injection, extData);
            TurboContract.Assert(inst != null, conditionString: "inst != null");

            var instType = inst.GetType();
            var methodName = ExtractMethodInfo<IInstanceCreatorNoParam>(a => a.CreateInstance()).Name;
            var method = instType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            TurboContract.Assert(method != null, conditionString: "method != null");

            var deleg = Delegate.CreateDelegate(typeof(Func<object>), inst, method);

            return (Func<object>)deleg;
        }


        /// <summary>
        /// Emits and creates an instance of an object that implements IInstanceCreator.
        /// That object can be used to create an object of 'objType' with the default constructor
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>IInstanceCreator to create 'objType' object</returns>
        public static IInstanceCreator BuildInstanceCreatorInDynAssembly(Type objType, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            TurboContract.Ensures(TurboContract.Result<IInstanceCreator>() != null);

            var constructor = FindConstructor(objType);
            TurboContract.Assert(constructor != null, conditionString: "constructor != null");
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildInstanceCreatorInDynAssembly(objType, constructor, extData);
        }


        /// <summary>
        /// Emits and creates an instance of an object that implements IInstanceCreatorNoParam.
        /// That object can be used to create an object of 'objType' with the default constructor.
        /// All constructor parameters resolves only once and stores internally in the fields.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>IInstanceCreatorNoParam to create 'objType' object</returns>
        public static IInstanceCreatorNoParam BuildInstanceCreatorNoParamInDynAssembly(Type objType, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");

            TurboContract.Ensures(TurboContract.Result<IInstanceCreatorNoParam>() != null);

            var constructor = FindConstructor(objType);
            TurboContract.Assert(constructor != null, conditionString: "constructor != null");
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildInstanceCreatorNoParamInDynAssembly(objType, constructor, injection, extData);
        }


        /// <summary>
        /// Creates a delegate to the method, that can create an object of 'objType' with the default constructor.
        /// That method emits in runtime to the dynamic assembly.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Delegate for the emitted method</returns>
        public static Func<IInjectionResolver, object> BuildCreatorFuncInDynAssembly(Type objType, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");

            TurboContract.Ensures(TurboContract.Result<Func<IInjectionResolver, object>>() != null);

            var constructor = FindConstructor(objType);
            TurboContract.Assert(constructor != null, conditionString: "constructor != null");
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildCreatorFuncInDynAssembly(objType, constructor, extData);
        }


        /// <summary>
        /// Creates a delegate to the method, that can create an object of 'objType' with the default constructor.
        /// That method emits in runtime to the dynamic assembly.
        /// All constructor parameters resolves only once and stores internally.
        /// </summary>
        /// <param name="objType">The type of the object</param>
        /// <param name="injection">Injection resolver to get the objects required by the constructor</param>
        /// <param name="extData">Extended information supplied by the user for Injection Resolver</param>
        /// <returns>Delegate for the emitted method</returns>
        public static Func<object> BuildCreatorFuncNoParamInDynAssembly(Type objType, IInjectionResolver injection, object extData)
        {
            TurboContract.Requires(objType != null, conditionString: "objType != null");
            TurboContract.Requires(injection != null, conditionString: "injection != null");

            TurboContract.Ensures(TurboContract.Result<Func<object>>() != null);

            var constructor = FindConstructor(objType);
            TurboContract.Assert(constructor != null, conditionString: "constructor != null");
            if (constructor == null)
                throw new CommonIoCException(string.Format("Can't find appropriate constructor for type {0}", objType.FullName));

            return BuildCreatorFuncNoParamInDynAssembly(objType, constructor, injection, extData);
        } 

        #endregion
    }
}
