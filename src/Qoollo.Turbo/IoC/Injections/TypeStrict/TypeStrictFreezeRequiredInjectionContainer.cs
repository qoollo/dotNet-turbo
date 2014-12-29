using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Контейнер инъекций для хранения соответствия Типа и объекта этого типа.
    /// Подходит для однопоточных сценариев, а также для многопоточных в случае заморозки.
    /// </summary>
    public sealed class TypeStrictFreezeRequiredInjectionContainer : FreezeRequiredGenericInjectionContainer<Type>
    {
        private readonly TypeStrictDirectInjectionResolver _fastDirectResolver;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_fastDirectResolver != null);
        }

        /// <summary>
        /// Простой резолвер инъекций, опирающийся лишь на тип запрашиваемого объекта
        /// </summary>
        private class TypeStrictDirectInjectionResolver : IInjectionResolver
        {
            private readonly TypeStrictFreezeRequiredInjectionContainer _container;

            [ContractInvariantMethod]
            private void Invariant()
            {
                Contract.Invariant(_container != null);
            }

            /// <summary>
            /// Конструктор TypeStrictDirectInjectionResolver
            /// </summary>
            /// <param name="container">Контейнер, которому он принадлежит</param>
            internal TypeStrictDirectInjectionResolver(TypeStrictFreezeRequiredInjectionContainer container)
            {
                Contract.Requires(container != null);

                _container = container;
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
                return _container.GetInjection(reqObjectType);
            }

            /// <summary>
            /// Упрощённое разрешение зависимости
            /// </summary>
            /// <typeparam name="T">Тип объекта, который требуется вернуть</typeparam>
            /// <param name="forType">Тип, для которого разрешается зависимость</param>
            /// <returns>Найденный объект запрашиваемого типа</returns>
            public T Resolve<T>(Type forType)
            {
                return (T)_container.GetInjection(typeof(T));
            }
        }


        /// <summary>
        /// Конструктор TypeStrictFreezeRequiredInjectionContainer
        /// </summary>
        public TypeStrictFreezeRequiredInjectionContainer()
        {
            _fastDirectResolver = new TypeStrictDirectInjectionResolver(this);
        }

        /// <summary>
        /// Конструктор TypeStrictFreezeRequiredInjectionContainer
        /// </summary>
        /// <param name="disposeInjectionsWithBuilder">Вызывать ли Dispose у хранимых объектов при уничтожении контейнера</param>
        public TypeStrictFreezeRequiredInjectionContainer(bool disposeInjectionsWithBuilder)
            : base(disposeInjectionsWithBuilder)
        {
            _fastDirectResolver = new TypeStrictDirectInjectionResolver(this);
        }


        /// <summary>
        /// Подходит ли данная инъекция для ключа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="injection">Инъекция</param>
        /// <returns>Подходит ли</returns>
        protected override bool IsGoodInjectionForKey(Type key, object injection)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (injection != null)
            {
                var tpInj = injection.GetType();
                return (tpInj == key) || key.IsAssignableFrom(tpInj);
            }

            return key.IsAssignableFromNull();
        }

        /// <summary>
        /// Получение инъекции по её типу
        /// </summary>
        /// <typeparam name="T">Тип запрашиваемой инъекции</typeparam>
        /// <returns>Инъекция</returns>
        public T GetInjection<T>()
        {
            return (T)this.GetInjection(typeof(T));
        }


        /// <summary>
        /// Попытка получить инъекцию
        /// </summary>
        /// <typeparam name="T">Тип запрашиваемой инъекции</typeparam>
        /// <param name="val">Значение инъекции в случае успеха</param>
        /// <returns>Успешность</returns>
        public bool TryGetInjection<T>(out T val)
        {
            object tmp = null;
            if (this.TryGetInjection(typeof(T), out tmp))
            {
                val = (T)tmp;
                return true;
            }

            val = default(T);
            return false;
        }


        /// <summary>
        /// Содержит ли контейнер инъекцию с типом T
        /// </summary>
        /// <typeparam name="T">Тип инъекции</typeparam>
        /// <returns>Содержит ли</returns>
        public bool Contains<T>()
        {
            return this.Contains(typeof(T));
        }

        /// <summary>
        /// Добавить инъекцию в контейнер
        /// </summary>
        /// <typeparam name="T">Тип инъекции</typeparam>
        /// <param name="val">Значение инъекции</param>
        public void AddInjection<T>(T val)
        {
            this.AddInjection(typeof(T), val);
        }

        /// <summary>
        /// Попытаться добавить инъекцию в контейнер
        /// </summary>
        /// <typeparam name="T">Тип инъекции</typeparam>
        /// <param name="val">Значение</param>
        /// <returns>Успешность</returns>
        public bool TryAddInjection<T>(T val)
        {
            return this.TryAddInjection(typeof(T), val);
        }

        /// <summary>
        /// Удалить инъекцию из контейнера
        /// </summary>
        /// <typeparam name="T">Тип удаляемой инъекции</typeparam>
        /// <returns>Была ли она там</returns>
        public bool RemoveInjection<T>()
        {
            return this.RemoveInjection(typeof(T));
        }

        /// <summary>
        /// Возвращает простой резолвер инъекций
        /// </summary>
        /// <returns>Резолвер</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IInjectionResolver GetDirectInjectionResolver()
        {
            Contract.Ensures(Contract.Result<IInjectionResolver>() != null);

            return _fastDirectResolver;
        }
    }
}
