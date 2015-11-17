using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Associations;
using Qoollo.Turbo.IoC.Injections;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Локатор объектов для общего случая (когда тип аллоцируемого объекта является дочерним к типу ключа)
    /// </summary>
    [Obsolete("This container is obsolete. Please, use TurboContainer instead.")]
    public class CommonObjectLocator: IObjectLocator<Type>, IDisposable
    {
        private bool _disposeInjectionWithBuilder;
        private bool _useAssocAsDISource;

        private readonly IInjectionResolver _resolver;
        private readonly DirectTypeAssociationContainer _association;
        private readonly TypeStrictInjectionContainer _injection;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_resolver != null);
            Contract.Invariant(_association != null);
            Contract.Invariant(_injection != null);
        }

        /// <summary>
        /// Association container
        /// </summary>
        private class AssociationContainer: DirectTypeAssociationContainer
        {
            private readonly IInjectionResolver _resolver;

            public AssociationContainer(IInjectionResolver resolver)
            {
                Contract.Requires(resolver != null);
                _resolver = resolver;
            }

            protected override Lifetime.LifetimeBase ProduceResolveInfo(Type key, Type objType, Lifetime.Factories.LifetimeFactory val)
            {
                return val.Create(objType, _resolver, null);
            }
        }

        /// <summary>
        /// Специальный резолвер инъекций, который сначала проверяет в контейнере инъекций, а потом в контейнере ассоциаций
        /// </summary>
        private class InjectionThenAssociationResolver : IInjectionResolver
        {
            private readonly TypeStrictInjectionContainer _sourceInj;
            private readonly CommonObjectLocator _curLocator;

            [ContractInvariantMethod]
            private void Invariant()
            {
                Contract.Invariant(_sourceInj != null);
                Contract.Invariant(_curLocator != null);
            }

            /// <summary>
            /// Конструктор InjectionThenAssociationResolver
            /// </summary>
            /// <param name="srcInj">Контейнер инъекций</param>
            /// <param name="locator">Локатор</param>
            public InjectionThenAssociationResolver(TypeStrictInjectionContainer srcInj, CommonObjectLocator locator)
            {
                Contract.Requires(srcInj != null);
                Contract.Requires(locator != null);

                _sourceInj = srcInj;
                _curLocator = locator;
            }

            /// <summary>
            /// Разрешить зависимость на основе подробной информации
            /// </summary>
            /// <param name="reqObjectType">Тип объекта, который требуется вернуть</param>
            /// <param name="paramName">Имя параметра, для которого разрешается зависимость (если применимо)</param>
            /// <param name="forType">Тип, для которого разрешается зависимость (если применимо)</param>
            /// <param name="extData">Расширенные данные для разрешения зависимости (если есть)</param>
            /// <returns>Найденный объект запрашиваемого типа</returns>
            public object Resolve(Type reqObjectType, string paramName, Type forType, object extData)
            {
                object res = null;            
                if (_sourceInj.TryGetInjection(reqObjectType, out res))
                    return res;

                return _curLocator.Resolve(reqObjectType);
            }

            /// <summary>
            /// Упрощённое разрешение зависимости
            /// </summary>
            /// <typeparam name="T">Тип объекта, который требуется вернуть</typeparam>
            /// <param name="forType">Тип, для которого разрешается зависимость</param>
            /// <returns>Найденный объект запрашиваемого типа</returns>
            public T Resolve<T>(Type forType)
            {
                object res = null;
                if (_sourceInj.TryGetInjection(typeof(T), out res))
                    return (T)res;

                return _curLocator.Resolve<T>();
            }
        }


        /// <summary>
        /// Конструктор CommonObjectLocator
        /// </summary>
        /// <param name="useAssocAsDISource">Использовать ли ассоциации как источник инъекций (возможно переполнение стека в случае ошибок)</param>
        /// <param name="disposeInjectionWithBuilder">Освобождать ли все инъекции с контейнером</param>
        public CommonObjectLocator(bool useAssocAsDISource, bool disposeInjectionWithBuilder)
        {
            _disposeInjectionWithBuilder = disposeInjectionWithBuilder;
            _useAssocAsDISource = useAssocAsDISource;

            _injection = new TypeStrictInjectionContainer(_disposeInjectionWithBuilder);

            if (_useAssocAsDISource)
                _resolver = new InjectionThenAssociationResolver(_injection, this);
            else
                _resolver = _injection.GetDirectInjectionResolver();

            _association = new AssociationContainer(_resolver);
        }

        /// <summary>
        /// Конструктор CommonObjectLocator
        /// </summary>
        public CommonObjectLocator()
            : this(false, true)
        {
        }



        /// <summary>
        /// Получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Полученное значение</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>()
        {
            var life = _association.GetAssociation(typeof(T));
            Contract.Assume(life != null);
            return (T)life.GetInstance(_resolver);
        }

        /// <summary>
        /// Попытаться получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="val">Полученное значение в случае успеха</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve<T>(out T val)
        {
            Lifetime.LifetimeBase life = null;

            if (_association.TryGetAssociation(typeof(T), out life))
            {
                Contract.Assume(life != null);
                object tmp = null;
                if (life.TryGetInstance(_resolver, out tmp))
                {
                    if (tmp is T)
                    {
                        val = (T)tmp;
                        return true;
                    }
                }
            }

            val = default(T);
            return false;
        }

        /// <summary>
        /// Можно ли получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Можно ли</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanResolve<T>()
        {
            return _association.Contains(typeof(T));
        }


        /// <summary>
        /// Создаёт объект типа T с использованием инъекций
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Созданный объект</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CreateObject<T>()
        {
            return InstantiationService.CreateObject<T>(_resolver);
        }


        /// <summary>
        /// Контейнер инъекций
        /// </summary>
        public TypeStrictInjectionContainer Injection
        {
            get { return _injection; }
        }

        /// <summary>
        /// Контейнер ассоциаций
        /// </summary>
        public DirectTypeAssociationContainer Association
        {
            get { return _association; }
        }


        /// <summary>
        /// Получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Полученный объект</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type key)
        {
            Contract.Requires(key != null);

            var life = _association.GetAssociation(key);
            Contract.Assume(life != null);
            return life.GetInstance(_resolver);
        }

        /// <summary>
        /// Попытаться получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект, если удалось получить</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve(Type key, out object val)
        {
            Contract.Requires((object)key != null);

            Lifetime.LifetimeBase life = null;

            if (_association.TryGetAssociation(key, out life))
            {
                Contract.Assume(life != null);
                if (life.TryGetInstance(_resolver, out val))
                    return true;
            }

            val = null;
            return false;
        }

        /// <summary>
        /// Можно ли получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Можно ли</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanResolve(Type key)
        {
            Contract.Requires(key != null);

            return Association.Contains(key);
        }

        /// <summary>
        /// Получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Полученный объект</returns>
        object IObjectLocator<Type>.Resolve(Type key)
        {
            return this.Resolve(key);
        }

        /// <summary>
        /// Попытаться получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект, если удалось получить</param>
        /// <returns>Успешность</returns>
        bool IObjectLocator<Type>.TryResolve(Type key, out object val)
        {
            return this.TryResolve(key, out val);
        }

        /// <summary>
        /// Можно ли получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Можно ли</returns>
        bool IObjectLocator<Type>.CanResolve(Type key)
        {
            return this.CanResolve(key);
        }


        /// <summary>
        /// Внутреннее освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">True - вызвано пользователем, False - вызвано деструктором</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (_association != null && _association is IDisposable)
                (_association as IDisposable).Dispose();

            if (_injection != null && _injection is IDisposable)
                (_injection as IDisposable).Dispose();
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
