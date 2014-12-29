using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.Lifetime;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Базовый контейнер ассоциаций для IoC с произвольным типом ключа
    /// </summary>
    /// <typeparam name="TKey">Тип ключа</typeparam>
    [ContractClass(typeof(GenericAssociationContainerBaseCodeContractCheck<>))]
    public abstract class GenericAssociationContainerBase<TKey>: IAssociationSource<TKey>, IFreezable, IDisposable
    {
        private volatile bool _isFrozen = false;
        private volatile bool _isDisposed = false;

        /// <summary>
        /// Подходит ли переданный тип для заданного ключа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Подходит ли</returns>
        [Pure]
        protected abstract bool IsGoodTypeForKey(TKey key, Type objType);

        /// <summary>
        /// Сформировать Lifetime контейнер по типу и фабрике 
        /// </summary>
        /// <param name="key">Ключ, по которому будет сохранён контейнер</param>
        /// <param name="objType">Тип объекта, который будет обрабатывать Lifetime контейнер</param>
        /// <param name="val">Фабрика для создания Lifetime контейнера</param>
        /// <returns>Lifetime контейнер</returns>
        protected abstract LifetimeBase ProduceResolveInfo(TKey key, Type objType, Lifetime.Factories.LifetimeFactory val);


        /// <summary>
        /// Внутренний метод добавления ассоциации
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта</param>
        protected abstract void AddAssociationInner(TKey key, LifetimeBase val);
        /// <summary>
        /// Внутренний метод попытки добавить ассоциацию в контейнер
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта</param>
        /// <returns>Успешность</returns>
        protected abstract bool TryAddAssociationInner(TKey key, LifetimeBase val);
        /// <summary>
        /// Внутренний метод добавления ассоциации в контейнер с использованием фабрики формирования Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, разрешаемый по ключу</param>
        /// <param name="val">Фабрика созадания Lifetime контейнера</param>
        protected abstract void AddAssociationInner(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val);
        /// <summary>
        /// Внутренний метод попытки добавления ассоциации в контейнер с использованием фабрики формирования Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, разрешаемый по ключу</param>
        /// <param name="val">Фабрика созадания Lifetime контейнера</param>
        /// <returns>Успешность</returns>
        protected abstract bool TryAddAssociationInner(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val);
        /// <summary>
        /// Внетренний метод попытки получить ассоциацию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта, если удалось получить</param>
        /// <returns>Успешность</returns>
        protected abstract bool TryGetAssociationInner(TKey key, out LifetimeBase val);
        /// <summary>
        /// Внутренний метод удаления ассоциации из контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Удалили ли</returns>
        protected abstract bool RemoveAssociationInner(TKey key);
        /// <summary>
        /// Внутренний метод проверки наличия ассоциации в контейнере
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть ли</returns>
        [Pure]
        protected abstract bool ContainsInner(TKey key);


        /// <summary>
        /// Проверяет состояние контейнера при выполнении какого-либо действия.
        /// Если состояние не соостветствует действию, то выбрасывается исключение
        /// </summary>
        /// <param name="onEdit">Действие делает изменения в контейнере</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckContainerState(bool onEdit)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("GenericAssociationContainerBase");

            if (onEdit && _isFrozen)
                throw new ObjectFrozenException("GenericAssociationContainerBase is frozen");
        }

        /// <summary>
        /// Проверяет состояние контейнера при выполнении какого-либо действия.
        /// Сообщает, возможно ли выполнить данное действие.
        /// </summary>
        /// <param name="onEdit">Действие делает изменения в контейнере</param>
        /// <returns>Можно ли выполнить действие</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CheckContainerStateBool(bool onEdit)
        {
            return !(_isDisposed || (onEdit && _isFrozen));
        }

        /// <summary>
        /// Преобразовывает режим инстанцирования на основе данных переопределения
        /// </summary>
        /// <param name="src">Исходный режим инстанцирования</param>
        /// <param name="overrideMod">В какой режим переопределить</param>
        /// <returns>Переопределённый режим инстанцирования</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ObjectInstantiationMode TransformInstMode(ObjectInstantiationMode src, OverrideObjectInstantiationMode overrideMod)
        {
            switch (overrideMod)
            {
                case OverrideObjectInstantiationMode.ToSingleton:
                    return ObjectInstantiationMode.Singleton;
                case OverrideObjectInstantiationMode.ToDeferedSingleton:
                    return ObjectInstantiationMode.DeferedSingleton;
                case OverrideObjectInstantiationMode.ToPerThread:
                    return ObjectInstantiationMode.PerThread;
                case OverrideObjectInstantiationMode.ToPerCall:
                    return ObjectInstantiationMode.PerCall;
                case OverrideObjectInstantiationMode.ToPerCallInlinedParams:
                    return ObjectInstantiationMode.PerCallInlinedParams;
                case OverrideObjectInstantiationMode.None:
                    return src;
            }
            Contract.Assert(false, "unknown OverrideObjectInstantiationMode");
            throw new AssociationIoCException("unknown OverrideObjectInstantiationMode");
        }

        /// <summary>
        /// Добавить ассоциацию в контейнер (с перезаписью существующего)
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта</param>
        protected void AddAssociation(TKey key, LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(val != null);
            Contract.Ensures(this.ContainsInner(key));

            CheckContainerState(true);

            if (!IsGoodTypeForKey(key, val.OutputType))
                throw new AssociationBadKeyForTypeException(string.Format("GenericAssociationContainerBase: Bad key ({0}) for type ({1})", key, val.OutputType));

            AddAssociationInner(key, val);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию в контейнер
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта</param>
        /// <returns>Удалось ли добавить</returns>
        protected bool TryAddAssociation(TKey key, LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(val != null);
            Contract.Ensures(Contract.Result<bool>() == false || this.ContainsInner(key));

            if (!CheckContainerStateBool(true))
                return false;

            if (!IsGoodTypeForKey(key, val.OutputType))
                return false;

            return TryAddAssociationInner(key, val);
        }

        /// <summary>
        /// Добавить ассоциацию в контейнер с использованием фабрики формирования Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, разрешаемый по ключу</param>
        /// <param name="val">Фабрика созадания Lifetime контейнера</param>
        protected void AddAssociation(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);
            Contract.Ensures(this.ContainsInner(key));

            CheckContainerState(true);

            if (!IsGoodTypeForKey(key, objType))
                throw new AssociationBadKeyForTypeException(string.Format("GenericAssociationContainerBase: Bad key ({0}) for type ({1})", key, objType));

            AddAssociationInner(key, objType, val);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию в контейнер с использованием фабрики формирования Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, разрешаемый по ключу</param>
        /// <param name="val">Фабрика созадания Lifetime контейнера</param>
        /// <returns>Удалось ли добавить</returns>
        protected bool TryAddAssociation(TKey key, Type objType, Qoollo.Turbo.IoC.Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);
            Contract.Ensures(Contract.Result<bool>() == false || this.ContainsInner(key));

            if (!CheckContainerStateBool(true))
                return false;

            if (!IsGoodTypeForKey(key, objType))
                return false;

            return TryAddAssociationInner(key, objType, val);
        }

        /// <summary>
        /// Удалить ассоциацию из контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Удалилась ли</returns>
        public bool RemoveAssociation(TKey key)
        {
            Contract.Requires((object)key != null);
            Contract.Ensures(!this.ContainsInner(key));

            CheckContainerState(true);

            return RemoveAssociationInner(key);
        }
        /// <summary>
        /// Содержит ли контейнер ассоциацию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержит ли</returns>
        public bool Contains(TKey key)
        {
            Contract.Requires((object)key != null);

            if (_isDisposed)
                return false;

            return this.ContainsInner(key);
        }


        /// <summary>
        /// Выполняет поиск подходящих типов для добавления в контейнер
        /// </summary>
        /// <typeparam name="TAttr">Тип аттрибута, который ищется на типе</typeparam>
        /// <param name="typeSource">Перечень типов, которые будут проверяться</param>
        /// <param name="attrCmpPredicate">Предикат для отсеивания типов с не подходящими атрибутами</param>
        /// <param name="procAction">Действие, которое выполняется для каждого найденного типа</param>
        /// <param name="multiAttr">Разрешить обработку множества атрибутов на типе (false - после обработки 1-ого сразу уходим на следующий тип)</param>
        protected void AddTypeRangeWithStrictAttr<TAttr>(IEnumerable<Type> typeSource, Func<TAttr, bool> attrCmpPredicate, 
            Action<Type, TAttr> procAction, bool multiAttr = true) 
            where TAttr: LocatorTargetObjectAttribute
        {
            Contract.Requires<ArgumentNullException>(typeSource != null);
            Contract.Requires<ArgumentNullException>(procAction != null);

            CheckContainerState(true);

            try
            {
                object[] attr = null;
                foreach (var curTp in typeSource)
                {
                    Contract.Assume(curTp != null);

                    attr = curTp.GetCustomAttributes(false);
                    if (attr == null || attr.Length == 0)
                        continue;

                    foreach (var curAttr in attr.Where(o => o is TAttr).Cast<TAttr>())
                    {
                        Contract.Assume(curAttr != null);

                        if (attrCmpPredicate == null || attrCmpPredicate(curAttr))
                        {
                            procAction(curTp, curAttr);

                            if (!multiAttr)
                                break;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (ObjectFrozenException)
            {
                throw;
            }
            catch (CommonIoCException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AssociationIoCException("Some exception during AddTypeRangeWithStrictAttr", ex);
            }
        }


        /// <summary>
        /// Добавить в контейнер типы на основе сканирования перечня типов
        /// </summary>
        /// <typeparam name="TAttr">Тип проверяемого атрибута, который должен быть определён на типе</typeparam>
        /// <param name="typeSource">Перечень проверяемых типов</param>
        /// <param name="attrCmpPredicate">Предикат для отсеивания типов с не подходящими атрибутами</param>
        /// <param name="keyGenerator">Функция извлечения ключа для добавления в контейнер</param>
        /// <param name="modeOver">Переопределение режима инстанцирования</param>
        /// <param name="multiAttr">Разрешить обработку множества атрибутов на типе</param>
        /// <param name="combineIfPossible">Использовать единый Lifetime контейнер для объектов одного типа, если возможно</param>
        protected void AddTypeRangeWithStrictAttrPlain<TAttr>(IEnumerable<Type> typeSource, Func<TAttr, bool> attrCmpPredicate, Func<Type, TAttr, TKey> keyGenerator, 
            OverrideObjectInstantiationMode modeOver = OverrideObjectInstantiationMode.None, bool multiAttr = true, bool combineIfPossible = true)
            where TAttr : LocatorTargetObjectAttribute
        {
            Contract.Requires<ArgumentNullException>(typeSource != null);
            Contract.Requires<ArgumentNullException>(keyGenerator != null);


            Type curAnalizeType = null;
            LifetimeBase singletonLf = null;
            LifetimeBase deferedSingletonLf = null;
            LifetimeBase perThreadLf = null;

            AddTypeRangeWithStrictAttr<TAttr>(typeSource, attrCmpPredicate, (tp, attr) =>
                {
                    if (tp != curAnalizeType)
                    {
                        curAnalizeType = tp;
                        singletonLf = null;
                        deferedSingletonLf = null;
                        perThreadLf = null;
                    }

                    var key = keyGenerator(tp, attr);

                    ObjectInstantiationMode instMode = TransformInstMode(attr.Mode, modeOver);

                    if (combineIfPossible)
                    {
                        switch (instMode)
                        {
                            case ObjectInstantiationMode.Singleton:
                                if (singletonLf == null)
                                    singletonLf = ProduceResolveInfo(key, tp, LifetimeFactories.Singleton);
                                AddAssociation(key, singletonLf);
                                break;
                            case ObjectInstantiationMode.DeferedSingleton:
                                if (deferedSingletonLf == null)
                                    deferedSingletonLf = ProduceResolveInfo(key, tp, LifetimeFactories.DeferedSingleton);
                                AddAssociation(key, deferedSingletonLf);
                                break;
                            case ObjectInstantiationMode.PerThread:
                                if (perThreadLf == null)
                                    perThreadLf = ProduceResolveInfo(key, tp, LifetimeFactories.PerThread);
                                AddAssociation(key, perThreadLf);
                                break;
                            default:
                                AddAssociation(key, tp, LifetimeFactories.GetLifetimeFactory(instMode));
                                break;
                        }
                    }
                    else
                    {
                        AddAssociation(key, tp, LifetimeFactories.GetLifetimeFactory(instMode));
                    }
                }, multiAttr);
        }



        /// <summary>
        /// Получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Контейнер управления жизнью объекта</returns>
        LifetimeBase IAssociationSource<TKey>.GetAssociation(TKey key)
        {
            Contract.Ensures(Contract.Result<LifetimeBase>() != null);

            if (_isDisposed)
                throw new ObjectDisposedException("GenericAssociationContainerBase");

            LifetimeBase res = null;
            if (!TryGetAssociationInner(key, out res))
                throw new KeyNotFoundException(string.Format("Key {0} not found in GenericAssociationContainerBase", key));

            return res;
        }

        /// <summary>
        /// Попытаться получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта, если удалось получить</param>
        /// <returns>Успешность</returns>
        bool IAssociationSource<TKey>.TryGetAssociation(TKey key, out LifetimeBase val)
        {
            if (_isDisposed)
            {
                val = null;
                return false;
            }

            return TryGetAssociationInner(key, out val);
        }

        /// <summary>
        /// Содержит ли контейнер ассоциацию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержит ли</returns>
        bool IAssociationSource<TKey>.Contains(TKey key)
        {
            if (_isDisposed)
                return false;

            return ContainsInner(key);
        }


        /// <summary>
        /// Заморозить контейнер инъекций
        /// </summary>
        public void Freeze()
        {
            _isFrozen = true;
        }

        /// <summary>
        /// Заморожен ли контейнер инъекций
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Освобождены ли ресурсы контейнера инъекций
        /// </summary>
        protected bool IsDisposed
        {
            get { return _isDisposed; }
        }

        /// <summary>
        /// Внутреннее освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">True - вызвано пользователем, False - вызвано деструктором</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }





    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(GenericAssociationContainerBase<>))]
    abstract class GenericAssociationContainerBaseCodeContractCheck<T> : GenericAssociationContainerBase<T>
    {
        /// <summary>Контракты</summary>
        private GenericAssociationContainerBaseCodeContractCheck() { }


        protected override LifetimeBase ProduceResolveInfo(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);
            Contract.Ensures(Contract.Result<LifetimeBase>() != null);

            throw new NotImplementedException();
        }


        protected override bool IsGoodTypeForKey(T key, Type objType)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);

            throw new NotImplementedException();
        }

        protected override void AddAssociationInner(T key, LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override bool TryAddAssociationInner(T key, LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override void AddAssociationInner(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override bool TryAddAssociationInner(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            Contract.Requires((object)key != null);
            Contract.Requires(objType != null);
            Contract.Requires(val != null);

            throw new NotImplementedException();
        }

        protected override bool TryGetAssociationInner(T key, out LifetimeBase val)
        {
            Contract.Requires((object)key != null);
            Contract.Ensures((Contract.Result<bool>() == true && Contract.ValueAtReturn<LifetimeBase>(out val) != null) ||
                (Contract.Result<bool>() == false && Contract.ValueAtReturn<LifetimeBase>(out val) == null));

            throw new NotImplementedException();
        }

        protected override bool RemoveAssociationInner(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }

        protected override bool ContainsInner(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }
    }
}
